// MIMESIS Patches Installer - entry point
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// No arguments: opens the installer window.
// Command line: MimesisPatchesInstaller install|uninstall|uninstall:<Patch> [path to Assembly-CSharp.dll] [--no-update]
//   install installs/updates every released patch; uninstall removes everything; uninstall:<Patch> removes one.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MimesisPatches;

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
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
                return 0;
            }
            // Command line: use the calling console (or open one) so output is visible.
            if (!AttachConsole(-1)) AllocConsole();
            return RunCommandLine(args);
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
                args = args.Where(a => a != "--no-update").ToArray();
                var dll = args.Length > 1 ? args[1] : Engine.FindDll(AppContext.BaseDirectory);
                if (dll == null)
                {
                    PatcherCore.Fail("Assembly-CSharp.dll not found. Put this exe in the MIMESIS game folder (next to MIMESIS.exe), or pass its path.");
                    return 1;
                }

                var mode = args[0].ToLowerInvariant();
                if (mode == "install")
                {
                    Engine.Install(dll, Engine.Patches, releases);
                    PatcherCore.Success("Done.");
                }
                else if (mode == "uninstall")
                {
                    Engine.Uninstall(dll, Engine.Patches, releases);
                    PatcherCore.Success("All patches removed; the game is back to its original files.");
                }
                else if (mode.StartsWith("uninstall:"))
                {
                    var wanted = mode.Substring("uninstall:".Length);
                    var p = Engine.Patches.FirstOrDefault(x => string.Equals(x.Name, wanted, StringComparison.OrdinalIgnoreCase));
                    if (p == null) { PatcherCore.Fail($"Unknown patch: {wanted}"); return 1; }
                    Engine.Uninstall(dll, new[] { p }, releases);
                    PatcherCore.Success($"{p.Name} removed.");
                }
                else
                {
                    PatcherCore.Fail($"Unknown command: {args[0]} (use install, uninstall or uninstall:<Patch>)");
                    return 1;
                }
                return 0;
            }
            catch (Exception e)
            {
                PatcherCore.Fail(Describe(e));
                return 1;
            }
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
