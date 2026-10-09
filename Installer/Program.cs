// MIMESIS Patches Installer - installs, updates or removes every patch in one go
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Usage: MimesisPatchesInstaller [install|uninstall] [path to Assembly-CSharp.dll]
// Install runs each patch's own install logic (compiled in), so it's identical to running the single patchers;
// re-running it updates everything. Uninstall restores the original Assembly-CSharp.dll from the backup and
// deletes every patch file.

using System;
using System.IO;
using System.Linq;
using MimesisPatches;

var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
Console.Title = $"MIMESIS Patches Installer v{version}";
Console.WriteLine($"MIMESIS Patches Installer v{version} - all patches in one go");
Console.WriteLine("by Mohamed Darwesh (@medovanx) - github.com/medovanx/mimesis-patches");
Console.WriteLine();

// (title, plugin class, runtime DLL) for the patches that use the shared patcher core.
var corePatches = new[]
{
    ("MIMESIS SpectatorCam Patcher", "SpectatorCam.Plugin", "SpectatorCamRuntime.dll"),
    ("MIMESIS Minimap Patcher", "Minimap.Plugin", "MinimapRuntime.dll"),
    ("MIMESIS HostOptions Patcher", "HostOptions.Plugin", "HostOptionsRuntime.dll"),
    ("MIMESIS HudPercent Patcher", "HudPercent.Plugin", "HudPercentRuntime.dll"),
    ("MIMESIS Inventory Patcher", "Inventory.Plugin", "InventoryRuntime.dll"),
};
var runtimeFiles = new[] { "BiggerLobbyRuntime.dll", "PSControllerRuntime.dll" }.Concat(corePatches.Select(p => p.Item3)).Append("0Harmony.dll").ToArray();

try
{
    var here = AppContext.BaseDirectory;
    var dll = args.Length > 1 ? args[1] : new[] {
        Path.Combine(here, "Assembly-CSharp.dll"),
        Path.Combine(here, "MIMESIS_Data", "Managed", "Assembly-CSharp.dll"),
    }.FirstOrDefault(File.Exists);
    if (dll == null)
    {
        PatcherCore.Fail("Assembly-CSharp.dll not found.\nPut this exe in the MIMESIS game folder (next to MIMESIS.exe) and run it again.");
    }
    else if ((args.Length > 0 ? args[0].ToLowerInvariant() : Ask()) == "uninstall")
    {
        var managed = Path.GetDirectoryName(dll);
        var bak = dll + ".bak";
        if (File.Exists(bak)) File.Copy(bak, dll, true);
        foreach (var f in runtimeFiles) { var p = Path.Combine(managed, f); if (File.Exists(p)) File.Delete(p); }
        var icons = Path.Combine(Path.GetDirectoryName(managed), "PSIcons");
        if (Directory.Exists(icons)) Directory.Delete(icons, true);
        PatcherCore.Success("\nSUCCESS! All patches removed; the game is back to its original files.");
    }
    else
    {
        Console.WriteLine("== BiggerLobby");
        BiggerLobbySetup.Setup.Run(new[] { dll });
        Console.WriteLine("\n== PSController");
        PSControllerSetup.Setup.Run(new[] { "install", dll });
        foreach (var (title, plugin, runtime) in corePatches)
        {
            Console.WriteLine($"\n== {title.Replace("MIMESIS ", "").Replace(" Patcher", "")}");
            PatcherCore.Run(new[] { "install", dll }, title, plugin, runtime);
        }
        PatcherCore.Success("\nDone. Open the game and click \"Patches\" on the main menu to configure them.");
    }
}
catch (IOException e)
{
    PatcherCore.Fail($"Could not write the game files. Make sure MIMESIS is closed, then try again.\n({e.Message})");
}
catch (Exception e)
{
    PatcherCore.Fail($"Unexpected error: {e}");
}
Console.WriteLine();
Console.WriteLine("Press any key to close.");
try { Console.ReadKey(true); } catch (InvalidOperationException) { }

static string Ask()
{
    Console.WriteLine("Patches: BiggerLobby, PSController, SpectatorCam, Minimap, HostOptions, HudPercent, Inventory");
    while (true)
    {
        Console.Write("Install/update all or uninstall all? (i/u, Enter = install): ");
        var s = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s) || s == "i" || s == "install") return "install";
        if (s == "u" || s == "uninstall") return "uninstall";
    }
}
