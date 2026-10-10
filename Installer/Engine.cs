// MIMESIS Patches Installer - install logic shared by the window and the command line
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The patches are BepInEx 5 plugins. The installer sets up BepInEx in the game folder if needed, then downloads each
// patch's plugin DLL (and PS icons) from its latest GitHub release into BepInEx\plugins\MimesisPatches.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace MimesisInstaller
{
    record Release(Version Version, Dictionary<string, string> Assets);

    /// <summary>A patch: its plugin DLL (and extra files) come from its latest GitHub release.</summary>
    record PatchInfo(string Name, string About, string Who, string[] Files)
    {
        public string Dll => Name + "Runtime.dll";
    }

    static class Engine
    {
        public const string Repo = "medovanx/mimesis-patches";
        public static readonly string Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        // BepInEx 5, the mod loader the patches run in. Installed once into the game folder if it isn't there.
        const string BepInExVersion = "5.4.23.5";
        const string BepInExUrl = "https://github.com/BepInEx/BepInEx/releases/download/v" + BepInExVersion + "/BepInEx_win_x64_" + BepInExVersion + ".zip";

        public static readonly List<PatchInfo> Patches = new List<PatchInfo>
        {
            new("BiggerLobby", "10-player lobbies + difficulty scaling", "Host", new[] { "BiggerLobbyRuntime.dll" }),
            new("PSController", "PlayStation button icons", "Anyone", new[] { "PSControllerRuntime.dll", "PSIcons.zip" }),
            new("SpectatorCam", "Free spectator camera", "Anyone", new[] { "SpectatorCamRuntime.dll" }),
            new("Minimap", "Minimap of the level", "Anyone", new[] { "MinimapRuntime.dll" }),
            new("HostOptions", "Stamina, money, difficulty", "Host", new[] { "HostOptionsRuntime.dll" }),
            new("HudPercent", "Health / radiation % on the HUD", "Anyone", new[] { "HudPercentRuntime.dll" }),
            new("Inventory", "Host sets 1-8 inventory slots", "Everyone", new[] { "InventoryRuntime.dll" }),
            new("LateJoin", "Join a game in progress", "Everyone", new[] { "LateJoinRuntime.dll" }),
            new("Fov", "Field of view slider", "Anyone", new[] { "FovRuntime.dll" }),
            new("Revive", "Revive dead teammates", "Host", new[] { "ReviveRuntime.dll" }),
        };

        // Long timeout: patch files and the installer itself can be slow to download on some connections.
        public static readonly HttpClient Http = CreateHttp();

        static HttpClient CreateHttp()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("mimesis-patches-installer");
            return http;
        }

        /// <summary>Assembly-CSharp.dll next to the exe or under MIMESIS_Data\Managed in the given folder.</summary>
        public static string FindDll(string folder) => new[] {
            Path.Combine(folder, "Assembly-CSharp.dll"),
            Path.Combine(folder, "MIMESIS_Data", "Managed", "Assembly-CSharp.dll"),
        }.FirstOrDefault(File.Exists);

        /// <summary>Finds the game without asking: next to this exe, then the path the game writes at the top of its
        /// log on every launch (any copy that has been started once), then Steam's library folders.</summary>
        public static string DetectDll()
        {
            var here = FindDll(AppContext.BaseDirectory);
            if (here != null) return here;
            foreach (var managed in FromGameLog().Concat(FromSteam()))
            {
                try
                {
                    var dll = Path.Combine(managed, "Assembly-CSharp.dll");
                    if (File.Exists(dll)) return Path.GetFullPath(dll);
                }
                catch { }
            }
            return null;
        }

        // %USERPROFILE%\AppData\LocalLow\ReLUGames\MIMESIS\Player.log starts with "Mono path[0] = '<game>/MIMESIS_Data/Managed'".
        static IEnumerable<string> FromGameLog()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "ReLUGames", "MIMESIS");
            foreach (var name in new[] { "Player.log", "Player-prev.log" })
            {
                string line = null;
                try
                {
                    using var fs = new FileStream(Path.Combine(dir, name), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(fs);
                    for (int i = 0; i < 20 && (line = reader.ReadLine()) != null; i++)
                        if (line.StartsWith("Mono path[0]")) break;
                }
                catch { continue; }
                if (line == null || !line.StartsWith("Mono path[0]")) continue;
                int a = line.IndexOf('\''), b = line.LastIndexOf('\'');
                if (a >= 0 && b > a) yield return line.Substring(a + 1, b - a - 1).Replace('/', Path.DirectorySeparatorChar);
            }
        }

        // Steam: registry SteamPath -> steamapps\libraryfolders.vdf "path" entries -> steamapps\common\MIMESIS.
        static IEnumerable<string> FromSteam()
        {
            string steam = null;
            try { steam = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string; } catch { }
            if (string.IsNullOrEmpty(steam)) yield break;
            var libraries = new List<string> { steam };
            try
            {
                var vdf = File.ReadAllText(Path.Combine(steam, "steamapps", "libraryfolders.vdf"));
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(vdf, "\"path\"\\s+\"([^\"]+)\""))
                    libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
            }
            catch { }
            foreach (var lib in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
                yield return Path.Combine(lib, "steamapps", "common", "MIMESIS", "MIMESIS_Data", "Managed");
        }

        // Layout: <game>\MIMESIS_Data\Managed\Assembly-CSharp.dll (how the game is found) and
        //         <game>\BepInEx\plugins\MimesisPatches\<Patch>Runtime.dll (+ PSIcons\) for the patches.
        static string GameDir(string dll) => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dll), "..", ".."));
        static string PluginDir(string dll) => Path.Combine(GameDir(dll), "BepInEx", "plugins", "MimesisPatches");

        // Installed as a plugin, or the old way (DLL in Managed), which the next install moves to BepInEx.
        public static bool IsInstalled(string dll, PatchInfo p) =>
            File.Exists(Path.Combine(PluginDir(dll), p.Dll)) || File.Exists(Path.Combine(Path.GetDirectoryName(dll), p.Dll));

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
            Log.CloseGame();
            // Patches installed the old way (hook in Assembly-CSharp.dll) move to BepInEx too, not just the selected ones.
            var list = selected.Union(Migrate(dll)).ToList();
            EnsureBepInEx(dll);
            var plugins = PluginDir(dll);
            Directory.CreateDirectory(plugins);
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                progress?.Invoke(i, list.Count);
                if (!releases.TryGetValue(p.Name, out var r)) { Console.WriteLine($"== {p.Name}: not released yet, skipped\n"); continue; }
                Console.WriteLine($"== {p.Name} v{r.Version.ToString(3)}");
                foreach (var file in Download(r, p.Files, p.Name))
                {
                    if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        var to = Path.Combine(plugins, Path.GetFileNameWithoutExtension(file));   // PSIcons.zip -> PSIcons\
                        Directory.CreateDirectory(to);
                        ZipFile.ExtractToDirectory(file, to, true);
                    }
                    else File.Copy(file, Path.Combine(plugins, Path.GetFileName(file)), true);
                }
                Log.Success("   installed");
                Console.WriteLine();
            }
            progress?.Invoke(list.Count, list.Count);
        }

        /// <summary>Removes the given patches' plugin files. BepInEx stays (other mods may use it).</summary>
        public static void Uninstall(string dll, IEnumerable<PatchInfo> remove, Dictionary<string, Release> releases, Action<int, int> progress = null)
        {
            Log.CloseGame();
            var removing = remove.ToList();
            var kept = Migrate(dll).Except(removing).ToList();   // old-style installs that stay: reinstall them as plugins
            var plugins = PluginDir(dll);
            foreach (var p in removing)
            {
                var path = Path.Combine(plugins, p.Dll);
                if (File.Exists(path)) File.Delete(path);
                if (p.Name == "PSController" && Directory.Exists(Path.Combine(plugins, "PSIcons"))) Directory.Delete(Path.Combine(plugins, "PSIcons"), true);
                Console.WriteLine($"== {p.Name}: removed");
            }
            if (Directory.Exists(plugins) && !Directory.EnumerateFileSystemEntries(plugins).Any()) Directory.Delete(plugins);
            Console.WriteLine();
            if (kept.Count > 0) Install(dll, kept, releases, progress);
            else progress?.Invoke(1, 1);
        }

        // Downloads BepInEx 5 and unpacks it into the game folder, unless it's already there.
        static void EnsureBepInEx(string dll)
        {
            var game = GameDir(dll);
            if (File.Exists(Path.Combine(game, "BepInEx", "core", "BepInEx.dll"))) return;
            Console.WriteLine($"== BepInEx {BepInExVersion} (mod loader, installed once)");
            var zip = Path.Combine(Path.GetTempPath(), "mimesis-patches", $"BepInEx_{BepInExVersion}.zip");
            Directory.CreateDirectory(Path.GetDirectoryName(zip));
            if (!File.Exists(zip))
            {
                Console.WriteLine("   downloading...");
                File.WriteAllBytes(zip, Http.GetByteArrayAsync(BepInExUrl).Result);
            }
            ZipFile.ExtractToDirectory(zip, game, true);
            Log.Success("   installed");
            Console.WriteLine();
        }

        /// <summary>Undoes an install from before BepInEx (startup hook in Assembly-CSharp.dll, patch DLLs in Managed):
        /// restores the original game DLL and removes those files. Returns the patches that were installed that way.</summary>
        static List<PatchInfo> Migrate(string dll)
        {
            var managed = Path.GetDirectoryName(dll);
            var old = Patches.Where(p => File.Exists(Path.Combine(managed, p.Dll))).ToList();
            var bak = dll + ".bak";
            if (!File.Exists(bak) && old.Count == 0) return old;
            Console.WriteLine("== Moving patches from the old install to BepInEx");
            if (File.Exists(bak))
            {
                File.Copy(bak, dll, true);
                File.Delete(bak);
                Console.WriteLine("   original Assembly-CSharp.dll restored");
            }
            foreach (var p in old) File.Delete(Path.Combine(managed, p.Dll));
            var harmony = Path.Combine(managed, "0Harmony.dll");   // the game doesn't ship Harmony; it came with the patches
            if (File.Exists(harmony)) File.Delete(harmony);
            var icons = Path.Combine(Path.GetDirectoryName(managed), "PSIcons");
            if (Directory.Exists(icons)) Directory.Delete(icons, true);
            Console.WriteLine();
            return old;
        }

        // Downloads a release's files into a temp folder (cached per version).
        static List<string> Download(Release r, string[] files, string name)
        {
            var dir = Path.Combine(Path.GetTempPath(), "mimesis-patches", $"{name}-v{r.Version.ToString(3)}");
            Directory.CreateDirectory(dir);
            var paths = new List<string>();
            foreach (var f in files)
            {
                if (!r.Assets.TryGetValue(f, out var url)) throw new InvalidOperationException($"{name} v{r.Version.ToString(3)} release has no {f}.");
                var path = Path.Combine(dir, f);
                if (!File.Exists(path))
                {
                    Console.WriteLine($"   downloading {f}...");
                    File.WriteAllBytes(path, Http.GetByteArrayAsync(url).Result);
                }
                paths.Add(path);
            }
            return paths;
        }
    }

    /// <summary>Console output helpers and closing the game.</summary>
    static class Log
    {
        /// <summary>Set when running from the window or the in-game updater: never wait for input.</summary>
        public static bool Unattended;

        public static void CloseGame()
        {
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("MIMESIS"))
            {
                try
                {
                    Console.WriteLine("MIMESIS is running; closing it so the patches can be installed...");
                    p.Kill();
                    p.WaitForExit(15000);
                }
                catch (Exception e) { Console.WriteLine($"Couldn't close MIMESIS ({e.Message}). Close it yourself and try again."); }
                finally { p.Dispose(); }
            }
        }

        public static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void Fail(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("FAILED: " + msg);
            Console.ResetColor();
        }
    }
}
