// MIMESIS Patches Installer - entry point
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// No arguments: opens the installer window.
// Command line: MimesisPatchesInstaller install|install:<A,B>|uninstall|uninstall:<Patch> [game folder] [--no-update] [--relaunch]
//   install installs/updates every released patch; uninstall removes everything; uninstall:<Patch> removes one.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MimesisInstaller
{
    static class Program
    {
        [DllImport("kernel32.dll")] static extern bool AttachConsole(int processId);
        [DllImport("kernel32.dll")] static extern bool AllocConsole();

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Log.Unattended = true;
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
                return 0;
            }
            // Command line: use the calling console (or open one) so output is visible.
            if (!AttachConsole(-1)) AllocConsole();
            // Never wait for input (it may run unattended from the in-game Update button), and keep a log.
            Log.Unattended = true;
            Console.SetIn(System.IO.TextReader.Null);
            var log = new System.IO.StreamWriter(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MimesisPatchesInstaller.log"), false) { AutoFlush = true };
            Console.SetOut(new Tee(Console.Out, log));
            log.WriteLine($"[{DateTime.Now}] {string.Join(" ", args)}");
            try { return RunCommandLine(args); }
            finally { log.Dispose(); }
        }

        static int RunCommandLine(string[] args)
        {
            Console.WriteLine($"MIMESIS Patches Installer v{Engine.Version}");
            Console.WriteLine("by Mohamed Darwesh (@medovanx) - github.com/medovanx/mimesis-patches\n");
            try
            {
                Console.WriteLine("Checking GitHub for the latest patches...");
                var releases = Engine.LatestReleases();
                if (!args.Contains("--no-update") && Engine.NewerInstaller(releases) is { } newer)
                {
                    Engine.LaunchNewer(newer, args);
                    return 0;
                }
                bool relaunch = args.Contains("--relaunch");
                args = args.Where(a => a != "--no-update" && a != "--relaunch").ToArray();
                // Game folder (or, from older in-game updaters, the path to Assembly-CSharp.dll).
                var dll = args.Length > 1 ? (args[1].EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? args[1] : Engine.FindDll(args[1])) : Engine.DetectDll();
                if (dll == null)
                {
                    Log.Fail("MIMESIS not found. Put this exe next to MIMESIS.exe, or pass the game folder.");
                    return 1;
                }

                var mode = args[0].ToLowerInvariant();
                if (mode == "install")
                {
                    Engine.Install(dll, Engine.Patches, releases);
                    Log.Success("Done.");
                }
                else if (mode.StartsWith("install:"))
                {
                    // install:A,B - used by the in-game Update button
                    var names = mode.Substring("install:".Length).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var picked = Engine.Patches.Where(x => names.Any(n => string.Equals(n, x.Name, StringComparison.OrdinalIgnoreCase))).ToList();
                    if (picked.Count == 0) { Log.Fail($"Unknown patch: {mode.Substring(8)}"); return 1; }
                    Engine.Install(dll, picked, releases);
                    Log.Success("Updated: " + string.Join(", ", picked.Select(x => x.Name)));
                }
                else if (mode == "uninstall")
                {
                    Engine.Uninstall(dll, Engine.Patches, releases);
                    Log.Success("All patches removed. BepInEx stays installed for any other mods.");
                }
                else if (mode.StartsWith("uninstall:"))
                {
                    var wanted = mode.Substring("uninstall:".Length);
                    var p = Engine.Patches.FirstOrDefault(x => string.Equals(x.Name, wanted, StringComparison.OrdinalIgnoreCase));
                    if (p == null) { Log.Fail($"Unknown patch: {wanted}"); return 1; }
                    Engine.Uninstall(dll, new[] { p }, releases);
                    Log.Success($"{p.Name} removed.");
                }
                else
                {
                    Log.Fail($"Unknown command: {args[0]} (use install, install:<Patch>, uninstall or uninstall:<Patch>)");
                    return 1;
                }
                if (relaunch)
                {
                    var game = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dll), "..", "..", "MIMESIS.exe"));
                    Console.WriteLine("Starting MIMESIS...");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(game) { UseShellExecute = true, WorkingDirectory = System.IO.Path.GetDirectoryName(game) });
                    System.Threading.Thread.Sleep(1500);
                }
                return 0;
            }
            catch (Exception e)
            {
                Log.Fail(Describe(e));
                return 1;
            }
        }

        /// <summary>Writes console output to the screen and to the log file.</summary>
        sealed class Tee : System.IO.TextWriter
        {
            readonly System.IO.TextWriter _a, _b;
            public Tee(System.IO.TextWriter a, System.IO.TextWriter b) { _a = a; _b = b; }
            public override System.Text.Encoding Encoding => _a.Encoding;
            public override void Write(char value) { _a.Write(value); try { _b.Write(value); } catch { } }
            public override void Write(string value) { _a.Write(value); try { _b.Write(value); } catch { } }
            public override void WriteLine(string value) { _a.WriteLine(value); try { _b.WriteLine(value); } catch { } }
        }

        public static string Describe(Exception e)
        {
            var ex = e.GetBaseException();
            if (ex is System.Net.Http.HttpRequestException || ex is System.Threading.Tasks.TaskCanceledException)
                return $"Couldn't reach GitHub ({ex.Message}). Check your internet connection and try again.";
            if (ex is System.IO.IOException)
                return $"Couldn't write the game files ({ex.Message}). Make sure MIMESIS is closed and try again.";
            return ex.Message;
        }
    }
}
