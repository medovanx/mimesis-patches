// MIMESIS Patches Installer - installs, updates or removes every patch in one go, downloading them from GitHub
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Usage: MimesisPatchesInstaller [install|uninstall] [path to Assembly-CSharp.dll]
// The installer carries only the install logic. Patch files (runtime DLLs, Harmony, PS icons) are downloaded
// from each patch's latest GitHub release, so a shared copy always installs the newest patches. If the
// install logic itself changed, it first downloads and opens the newest installer.
// Without arguments it shows a checklist: unchecked patches are skipped (left as they are if installed).
// Uninstall restores the original Assembly-CSharp.dll from the backup and deletes every patch file.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using MimesisPatches;

const string Repo = "medovanx/mimesis-patches";
var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
Console.Title = $"MIMESIS Patches Installer v{version}";

// name, description, files its release provides, install action
var patches = new List<(string Name, string About, string[] Files, Action<string> Install)>
{
    ("BiggerLobby", "10-player lobbies + difficulty scaling", new[] { "BiggerLobbyRuntime.dll", "0Harmony.dll" }, dll => BiggerLobbySetup.Setup.Run(new[] { dll })),
    ("PSController", "PlayStation button icons", new[] { "PSControllerRuntime.dll", "PSIcons.zip" }, dll => PSControllerSetup.Setup.Run(new[] { "install", dll })),
    ("SpectatorCam", "Free spectator camera", new[] { "SpectatorCamRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "SpectatorCam")),
    ("Minimap", "Minimap (layout + you)", new[] { "MinimapRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Minimap")),
    ("HostOptions", "Host: infinite stamina, starting money", new[] { "HostOptionsRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "HostOptions")),
    ("HudPercent", "Health / radiation % on the HUD", new[] { "HudPercentRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "HudPercent")),
    ("Inventory", "Host sets 1-8 slots (all players need it)", new[] { "InventoryRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Inventory")),
    ("LateJoin", "Join a game in progress (all players need it)", new[] { "LateJoinRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "LateJoin")),
    ("Fov", "Field of view setting", new[] { "FovRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Fov")),
    ("Revive", "Revive dead teammates (host)", new[] { "ReviveRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Revive")),
};

// Long timeout: patch files and the installer itself can be slow to download on some connections.
using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("mimesis-patches-installer");

try
{
    Header(version);
    Console.WriteLine("Checking GitHub for the latest patches...");
    var releases = LatestReleases(http);   // "<Name>" -> (version, asset name -> url)

    if (!args.Contains("--no-update") && releases.TryGetValue("Installer", out var inst) && inst.Version > new Version(version)
        && inst.Assets.TryGetValue("MimesisPatchesInstaller.exe", out var instUrl))
    {
        // Newer install logic: hand over to the newer installer.
        Console.WriteLine($"Downloading installer v{inst.Version.ToString(3)}...");
        var target = Path.Combine(AppContext.BaseDirectory, $"MimesisPatchesInstaller-v{inst.Version.ToString(3)}.exe");
        File.WriteAllBytes(target, http.GetByteArrayAsync(instUrl).Result);
        var start = new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = AppContext.BaseDirectory };
        foreach (var a in args.Append("--no-update")) start.ArgumentList.Add(a);
        System.Diagnostics.Process.Start(start);
        return;
    }
    args = args.Where(a => a != "--no-update").ToArray();

    var here = AppContext.BaseDirectory;
    var dll = args.Length > 1 ? args[1] : new[] {
        Path.Combine(here, "Assembly-CSharp.dll"),
        Path.Combine(here, "MIMESIS_Data", "Managed", "Assembly-CSharp.dll"),
    }.FirstOrDefault(File.Exists);
    if (dll == null)
    {
        PatcherCore.Fail("Assembly-CSharp.dll not found.\nPut this exe in the MIMESIS game folder (next to MIMESIS.exe) and run it again.");
    }
    else
    {
        var managed = Path.GetDirectoryName(dll);
        var items = patches.Select(p =>
        {
            releases.TryGetValue(p.Name, out var r);
            bool installed = File.Exists(Path.Combine(managed, p.Files[0]));
            string state = r == null ? "not released yet" : $"v{r.Version.ToString(3)}" + (installed ? "  (installed)" : "");
            return (p.Name, p.About, state, Available: r != null);
        }).ToList();
        bool[] selected = items.Select(i => i.Available).ToArray();
        string mode = args.Length > 0 ? args[0].ToLowerInvariant() : Checklist(version, items, selected);

        Clear();
        Header(version);
        if (mode == "uninstall")
        {
            var bak = dll + ".bak";
            if (File.Exists(bak)) File.Copy(bak, dll, true);
            foreach (var f in patches.Select(p => p.Files[0]).Append("0Harmony.dll")) { var p = Path.Combine(managed, f); if (File.Exists(p)) File.Delete(p); }
            var icons = Path.Combine(Path.GetDirectoryName(managed), "PSIcons");
            if (Directory.Exists(icons)) Directory.Delete(icons, true);
            PatcherCore.Success("SUCCESS! All patches removed; the game is back to its original files.");
        }
        else if (mode == "install")
        {
            for (int i = 0; i < patches.Count; i++)
            {
                var p = patches[i];
                if (!selected[i] || !releases.TryGetValue(p.Name, out var r)) { Console.WriteLine($"== {p.Name}: skipped\n"); continue; }
                Console.WriteLine($"== {p.Name} v{r.Version.ToString(3)}");
                UseFiles(Download(http, r, p.Files, p.Name));
                p.Install(dll);
                Console.WriteLine();
            }
            PatcherCore.Success("Done. Open the game and click \"Patches\" on the main menu to configure them.");
        }
        else if (mode.StartsWith("uninstall:"))
        {
            var name = mode.Substring("uninstall:".Length);
            var p = patches.First(x => x.Name == name);
            var path = Path.Combine(managed, p.Files[0]);
            if (File.Exists(path)) File.Delete(path);
            if (name == "PSController")
            {
                var icons = Path.Combine(Path.GetDirectoryName(managed), "PSIcons");
                if (Directory.Exists(icons)) Directory.Delete(icons, true);
            }
            PatcherCore.Success($"SUCCESS! {name} removed. (Assembly-CSharp.dll was left as is; its hook is now unused.)");
        }
        else Console.WriteLine("Nothing changed.");
    }
}
catch (IOException e)
{
    PatcherCore.Fail($"Could not write the game files. Make sure MIMESIS is closed, then try again.\n({e.Message})");
}
catch (Exception e)
{
    var ex = e.GetBaseException();
    PatcherCore.Fail(ex is HttpRequestException || ex is System.Threading.Tasks.TaskCanceledException
        ? $"Couldn't reach GitHub ({ex.Message}). Check your internet connection and try again."
        : $"Unexpected error: {e}");
}
Console.WriteLine();
Console.WriteLine("Press any key to close.");
try { Console.ReadKey(true); } catch (InvalidOperationException) { }

static void Core(string dll, string name) =>
    PatcherCore.Run(new[] { "install", dll }, $"MIMESIS {name} Patcher", $"{name}.Plugin", $"{name}Runtime.dll");

// Latest release per patch, from tags "<Name>-vX.Y.Z".
static Dictionary<string, Release> LatestReleases(HttpClient http)
{
    var result = new Dictionary<string, Release>();
    using var doc = JsonDocument.Parse(http.GetStringAsync($"https://api.github.com/repos/{Repo}/releases?per_page=100").Result);
    foreach (var rel in doc.RootElement.EnumerateArray())
    {
        if (rel.GetProperty("draft").GetBoolean()) continue;
        var tag = rel.GetProperty("tag_name").GetString() ?? "";
        int dash = tag.LastIndexOf("-v", StringComparison.Ordinal);
        if (dash <= 0 || !Version.TryParse(tag.Substring(dash + 2), out var v)) continue;
        var name = tag.Substring(0, dash);
        if (result.TryGetValue(name, out var have) && have.Version >= v) continue;
        var assets = rel.GetProperty("assets").EnumerateArray()
            .ToDictionary(a => a.GetProperty("name").GetString(), a => a.GetProperty("browser_download_url").GetString());
        result[name] = new Release(v, assets);
    }
    return result;
}

// Downloads a release's files into a temp folder (cached per version).
static Dictionary<string, string> Download(HttpClient http, Release r, string[] files, string name)
{
    var dir = Path.Combine(Path.GetTempPath(), "mimesis-patches", $"{name}-v{r.Version.ToString(3)}");
    Directory.CreateDirectory(dir);
    var paths = new Dictionary<string, string>();
    foreach (var f in files)
    {
        if (!r.Assets.TryGetValue(f, out var url)) throw new InvalidOperationException($"{name} v{r.Version.ToString(3)} release has no {f}.");
        var path = Path.Combine(dir, f);
        if (!File.Exists(path))
        {
            Console.WriteLine($"   downloading {f}...");
            File.WriteAllBytes(path, http.GetByteArrayAsync(url).Result);
        }
        paths[f] = path;
    }
    return paths;
}

// Points the install logic at the downloaded files (PSIcons.zip becomes the "icons/*.png" resources).
static void UseFiles(Dictionary<string, string> files)
{
    var icons = new Dictionary<string, byte[]>();
    if (files.TryGetValue("PSIcons.zip", out var zip))
        using (var archive = ZipFile.OpenRead(zip))
            foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".png")))
            {
                using var s = entry.Open();
                using var m = new MemoryStream();
                s.CopyTo(m);
                icons["icons/" + entry.Name] = m.ToArray();
            }
    Stream Open(string name) =>
        icons.TryGetValue(name, out var bytes) ? new MemoryStream(bytes)
        : files.TryGetValue(name, out var path) ? File.OpenRead(path)
        : null;
    PatcherCore.ResourceOverride = Open;
    BiggerLobbySetup.Setup.ResourceOverride = Open;
    PSControllerSetup.Setup.ResourceOverride = Open;
    PSControllerSetup.Setup.ResourceNamesOverride = () => icons.Keys.ToArray();
}

// Clear fails without a real console window (e.g. output redirected); that's fine to skip.
static void Clear() { try { Console.Clear(); } catch (IOException) { } }

static void Header(string version)
{
    Console.WriteLine($"MIMESIS Patches Installer v{version}");
    Console.WriteLine("by Mohamed Darwesh (@medovanx) - github.com/medovanx/mimesis-patches");
    Console.WriteLine();
}

// Arrow keys + Space checklist. Returns "install", "uninstall", "uninstall:<Name>" or "quit".
static string Checklist(string version, List<(string Name, string About, string State, bool Available)> items, bool[] selected)
{
    if (Console.IsInputRedirected) return "install";
    int cursor = 0;
    int nameWidth = items.Max(i => i.Name.Length) + 2;
    int aboutWidth = items.Max(i => i.About.Length) + 2;
    Console.CursorVisible = false;
    Console.Clear();   // once, so the buffer is sized correctly; every later redraw only repositions the cursor
    try
    {
        while (true)
        {
            // Redraw in place (no Console.Clear) so the window doesn't flash on every key press.
            Console.SetCursorPosition(0, 0);
            Header(version);
            Console.WriteLine("Choose the patches to install or update (downloaded from GitHub):" + Pad("", 20) + "\n");
            for (int i = 0; i < items.Count; i++)
            {
                Console.ForegroundColor = !items[i].Available ? ConsoleColor.DarkGray : i == cursor ? ConsoleColor.Yellow : ConsoleColor.Gray;
                Console.Write($"{(i == cursor ? ">" : " ")} [{(selected[i] ? "x" : " ")}] {Pad(items[i].Name, nameWidth)}{Pad(items[i].About, aboutWidth)}");
                Console.ForegroundColor = items[i].State.Contains("installed") ? ConsoleColor.DarkGreen : ConsoleColor.DarkGray;
                Console.WriteLine(Pad(items[i].State, 24));
            }
            Console.ResetColor();
            Console.WriteLine("\n[\u2191/\u2193] move   [Space] toggle   [A] all/none   [Enter] install selected                    ");
            Console.WriteLine("[U] uninstall all   [Delete] uninstall selected patch only   [Esc] quit                  ");
            Console.WriteLine("Unchecked patches are skipped (left as they are if already installed).                   ");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.UpArrow: cursor = (cursor - 1 + items.Count) % items.Count; break;
                case ConsoleKey.DownArrow: cursor = (cursor + 1) % items.Count; break;
                case ConsoleKey.Spacebar: if (items[cursor].Available) selected[cursor] = !selected[cursor]; break;
                case ConsoleKey.A:
                    bool all = Enumerable.Range(0, items.Count).Where(i => items[i].Available).All(i => selected[i]);
                    for (int i = 0; i < selected.Length; i++) selected[i] = items[i].Available && !all;
                    break;
                case ConsoleKey.Enter: return selected.Any(s => s) ? "install" : "quit";
                case ConsoleKey.U: return "uninstall";
                case ConsoleKey.Delete: case ConsoleKey.Backspace: return $"uninstall:{items[cursor].Name}";
                case ConsoleKey.Escape: return "quit";
            }
        }
    }
    finally { Console.CursorVisible = true; }
}

static string Pad(string s, int width) => s.Length >= width ? s : s + new string(' ', width - s.Length);

record Release(Version Version, Dictionary<string, string> Assets);
