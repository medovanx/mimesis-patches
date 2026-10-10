// MIMESIS Patches Installer - install logic shared by the window and the command line
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The installer carries only the install logic. Patch files (runtime DLLs, Harmony, PS icons) are downloaded
// from each patch's latest GitHub release, so a shared copy always installs the newest patches.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using MimesisPatches;

namespace MimesisInstaller
{
    record Release(Version Version, Dictionary<string, string> Assets);

    record PatchInfo(string Name, string About, string Who, string[] Files, Action<string> Install);

    static class Engine
    {
        public const string Repo = "medovanx/mimesis-patches";
        public static readonly string Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public static readonly List<PatchInfo> Patches = new List<PatchInfo>
        {
            new("BiggerLobby", "10-player lobbies + difficulty scaling", "Host", new[] { "BiggerLobbyRuntime.dll", "0Harmony.dll" }, dll => BiggerLobbySetup.Setup.Run(new[] { dll })),
            new("PSController", "PlayStation button icons", "Anyone", new[] { "PSControllerRuntime.dll", "PSIcons.zip" }, dll => PSControllerSetup.Setup.Run(new[] { "install", dll })),
            new("SpectatorCam", "Free spectator camera", "Anyone", new[] { "SpectatorCamRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "SpectatorCam")),
            new("Minimap", "Minimap (layout + you)", "Anyone", new[] { "MinimapRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Minimap")),
            new("HostOptions", "Infinite stamina, starting money", "Host", new[] { "HostOptionsRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "HostOptions")),
            new("HudPercent", "Health / radiation % on the HUD", "Anyone", new[] { "HudPercentRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "HudPercent")),
            new("Inventory", "Host sets 1-8 inventory slots", "Everyone", new[] { "InventoryRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Inventory")),
            new("LateJoin", "Join a game in progress", "Everyone", new[] { "LateJoinRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "LateJoin")),
            new("Fov", "Field of view slider", "Anyone", new[] { "FovRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Fov")),
            new("Revive", "Revive dead teammates", "Host", new[] { "ReviveRuntime.dll", "0Harmony.dll" }, dll => Core(dll, "Revive")),
        };

        // Long timeout: patch files and the installer itself can be slow to download on some connections.
        public static readonly HttpClient Http = CreateHttp();

        static HttpClient CreateHttp()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("mimesis-patches-installer");
            return http;
        }

        static void Core(string dll, string name) =>
            PatcherCore.Run(new[] { "install", dll }, $"MIMESIS {name} Patcher", $"{name}.Plugin", $"{name}Runtime.dll");

        /// <summary>Assembly-CSharp.dll next to the exe or under MIMESIS_Data\Managed in the given folder.</summary>
        public static string FindDll(string folder) => new[] {
            Path.Combine(folder, "Assembly-CSharp.dll"),
            Path.Combine(folder, "MIMESIS_Data", "Managed", "Assembly-CSharp.dll"),
        }.FirstOrDefault(File.Exists);

        public static bool IsInstalled(string dll, PatchInfo p) => File.Exists(Path.Combine(Path.GetDirectoryName(dll), p.Files[0]));

        /// <summary>Latest release per name, from tags "Name-vX.Y.Z" (includes "Installer").</summary>
        public static Dictionary<string, Release> LatestReleases()
        {
            var result = new Dictionary<string, Release>();
            using var doc = JsonDocument.Parse(Http.GetStringAsync($"https://api.github.com/repos/{Repo}/releases?per_page=100").Result);
            foreach (var rel in doc.RootElement.EnumerateArray())
            {
                if (rel.GetProperty("draft").GetBoolean()) continue;
                var tag = rel.GetProperty("tag_name").GetString() ?? "";
                int dash = tag.LastIndexOf("-v", StringComparison.Ordinal);
                if (dash <= 0 || !System.Version.TryParse(tag.Substring(dash + 2), out var v)) continue;
                var name = tag.Substring(0, dash);
                if (result.TryGetValue(name, out var have) && have.Version >= v) continue;
                var assets = rel.GetProperty("assets").EnumerateArray()
                    .ToDictionary(a => a.GetProperty("name").GetString(), a => a.GetProperty("browser_download_url").GetString());
                result[name] = new Release(v, assets);
            }
            return result;
        }

        /// <summary>Newer installer release, if any.</summary>
        public static Release NewerInstaller(Dictionary<string, Release> releases) =>
            releases.TryGetValue("Installer", out var r) && r.Version > new Version(Version) && r.Assets.ContainsKey("MimesisPatchesInstaller.exe") ? r : null;

        /// <summary>Downloads the newer installer next to this one and starts it with the given arguments.</summary>
        public static void LaunchNewer(Release r, IEnumerable<string> args)
        {
            Console.WriteLine($"Downloading installer v{r.Version.ToString(3)}...");
            var target = Path.Combine(AppContext.BaseDirectory, $"MimesisPatchesInstaller-v{r.Version.ToString(3)}.exe");
            File.WriteAllBytes(target, Http.GetByteArrayAsync(r.Assets["MimesisPatchesInstaller.exe"]).Result);
            var start = new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = AppContext.BaseDirectory };
            foreach (var a in args.Append("--no-update")) start.ArgumentList.Add(a);
            System.Diagnostics.Process.Start(start);
        }

        public static void Install(string dll, IEnumerable<PatchInfo> selected, Dictionary<string, Release> releases, Action<int, int> progress = null)
        {
            PatcherCore.CloseGame();
            var list = selected.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                progress?.Invoke(i, list.Count);
                if (!releases.TryGetValue(p.Name, out var r)) { Console.WriteLine($"== {p.Name}: not released yet, skipped\n"); continue; }
                Console.WriteLine($"== {p.Name} v{r.Version.ToString(3)}");
                UseFiles(Download(r, p.Files, p.Name));
                p.Install(dll);
                Console.WriteLine();
            }
            progress?.Invoke(list.Count, list.Count);
        }

        /// <summary>Removes the given patches. Every patch adds a startup hook to Assembly-CSharp.dll, so this
        /// restores the original and reinstalls the patches that stay.</summary>
        public static void Uninstall(string dll, IEnumerable<PatchInfo> remove, Dictionary<string, Release> releases, Action<int, int> progress = null)
        {
            PatcherCore.CloseGame();
            var removing = remove.Select(p => p.Name).ToHashSet();
            var managed = Path.GetDirectoryName(dll);
            var keep = Patches.Where(p => !removing.Contains(p.Name) && IsInstalled(dll, p)).ToList();
            var bak = dll + ".bak";
            if (!File.Exists(bak)) throw new InvalidOperationException("No Assembly-CSharp.dll.bak backup found, so patches can't be removed safely.");
            File.Copy(bak, dll, true);
            foreach (var f in Patches.Select(p => p.Files[0]).Append("0Harmony.dll"))
            {
                var path = Path.Combine(managed, f);
                if (File.Exists(path)) File.Delete(path);
            }
            var icons = Path.Combine(Path.GetDirectoryName(managed), "PSIcons");
            if (Directory.Exists(icons)) Directory.Delete(icons, true);
            foreach (var name in removing) Console.WriteLine($"== {name}: removed");
            Console.WriteLine();
            if (keep.Count > 0)
            {
                Console.WriteLine($"Reinstalling the {keep.Count} patch(es) you're keeping...\n");
                Install(dll, keep, releases, progress);
            }
        }

        // Downloads a release's files into a temp folder (cached per version).
        static Dictionary<string, string> Download(Release r, string[] files, string name)
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
                    File.WriteAllBytes(path, Http.GetByteArrayAsync(url).Result);
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
    }
}
