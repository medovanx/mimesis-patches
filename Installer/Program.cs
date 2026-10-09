// MIMESIS Patches Installer - installs, updates or removes every patch in one go
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Usage: MimesisPatchesInstaller [install|uninstall] [path to Assembly-CSharp.dll]
// Without arguments it shows a checklist (all patches checked): unchecked patches are skipped (not installed,
// and left as they are if already installed). Install runs each patch's own install logic (compiled in), so
// it's identical to the single patchers; re-running it updates everything. Uninstall restores the original
// Assembly-CSharp.dll from the backup and deletes every patch file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MimesisPatches;

var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
Console.Title = $"MIMESIS Patches Installer v{version}";

// name, description, runtime DLL, install action
var patches = new List<(string Name, string About, string Runtime, Action<string> Install)>
{
    ("BiggerLobby", "10-player lobbies + difficulty scaling", "BiggerLobbyRuntime.dll", dll => BiggerLobbySetup.Setup.Run(new[] { dll })),
    ("PSController", "PlayStation button icons", "PSControllerRuntime.dll", dll => PSControllerSetup.Setup.Run(new[] { "install", dll })),
    ("SpectatorCam", "Free spectator camera", "SpectatorCamRuntime.dll", dll => PatcherCore.Run(new[] { "install", dll }, "MIMESIS SpectatorCam Patcher", "SpectatorCam.Plugin", "SpectatorCamRuntime.dll")),
    ("Minimap", "Minimap (layout + you)", "MinimapRuntime.dll", dll => PatcherCore.Run(new[] { "install", dll }, "MIMESIS Minimap Patcher", "Minimap.Plugin", "MinimapRuntime.dll")),
    ("HostOptions", "Host: infinite stamina, starting money", "HostOptionsRuntime.dll", dll => PatcherCore.Run(new[] { "install", dll }, "MIMESIS HostOptions Patcher", "HostOptions.Plugin", "HostOptionsRuntime.dll")),
    ("HudPercent", "Health / radiation % on the HUD", "HudPercentRuntime.dll", dll => PatcherCore.Run(new[] { "install", dll }, "MIMESIS HudPercent Patcher", "HudPercent.Plugin", "HudPercentRuntime.dll")),
    ("Inventory", "Host sets 4-8 slots (all players need it)", "InventoryRuntime.dll", dll => PatcherCore.Run(new[] { "install", dll }, "MIMESIS Inventory Patcher", "Inventory.Plugin", "InventoryRuntime.dll")),
};

try
{
    var here = AppContext.BaseDirectory;
    var dll = args.Length > 1 ? args[1] : new[] {
        Path.Combine(here, "Assembly-CSharp.dll"),
        Path.Combine(here, "MIMESIS_Data", "Managed", "Assembly-CSharp.dll"),
    }.FirstOrDefault(File.Exists);
    if (dll == null)
    {
        Header(version);
        PatcherCore.Fail("Assembly-CSharp.dll not found.\nPut this exe in the MIMESIS game folder (next to MIMESIS.exe) and run it again.");
    }
    else
    {
        var managed = Path.GetDirectoryName(dll);
        bool[] selected = Enumerable.Repeat(true, patches.Count).ToArray();
        string mode = args.Length > 0 ? args[0].ToLowerInvariant() : Checklist(version, patches.Select(p => (p.Name, p.About, File.Exists(Path.Combine(managed, p.Runtime)))).ToList(), selected);

        Console.Clear();
        Header(version);
        if (mode == "uninstall")
        {
            var bak = dll + ".bak";
            if (File.Exists(bak)) File.Copy(bak, dll, true);
            foreach (var f in patches.Select(p => p.Runtime).Append("0Harmony.dll")) { var p = Path.Combine(managed, f); if (File.Exists(p)) File.Delete(p); }
            var icons = Path.Combine(Path.GetDirectoryName(managed), "PSIcons");
            if (Directory.Exists(icons)) Directory.Delete(icons, true);
            PatcherCore.Success("SUCCESS! All patches removed; the game is back to its original files.");
        }
        else if (mode == "install")
        {
            for (int i = 0; i < patches.Count; i++)
            {
                if (!selected[i]) { Console.WriteLine($"== {patches[i].Name}: skipped\n"); continue; }
                Console.WriteLine($"== {patches[i].Name}");
                patches[i].Install(dll);
                Console.WriteLine();
            }
            PatcherCore.Success("Done. Open the game and click \"Patches\" on the main menu to configure them.");
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
    PatcherCore.Fail($"Unexpected error: {e}");
}
Console.WriteLine();
Console.WriteLine("Press any key to close.");
try { Console.ReadKey(true); } catch (InvalidOperationException) { }

static void Header(string version)
{
    Console.WriteLine($"MIMESIS Patches Installer v{version}");
    Console.WriteLine("by Mohamed Darwesh (@medovanx) - github.com/medovanx/mimesis-patches");
    Console.WriteLine();
}

// Arrow keys + Space checklist. Returns "install", "uninstall" or "quit".
static string Checklist(string version, List<(string Name, string About, bool Installed)> items, bool[] selected)
{
    if (Console.IsInputRedirected) return "install";
    int cursor = 0;
    Console.CursorVisible = false;
    try
    {
        while (true)
        {
            Console.Clear();
            Header(version);
            Console.WriteLine("Choose the patches to install or update:\n");
            for (int i = 0; i < items.Count; i++)
            {
                Console.ForegroundColor = i == cursor ? ConsoleColor.Yellow : ConsoleColor.Gray;
                Console.Write($"{(i == cursor ? ">" : " ")} [{(selected[i] ? "x" : " ")}] {items[i].Name,-14}{items[i].About,-44}");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(items[i].Installed ? "(installed)" : "");
            }
            Console.ResetColor();
            Console.WriteLine("\nUp/Down move   Space toggle   A all/none   Enter install selected   U uninstall all   Esc quit");
            Console.WriteLine("Unchecked patches are skipped (left as they are if already installed).");

            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.UpArrow: cursor = (cursor - 1 + items.Count) % items.Count; break;
                case ConsoleKey.DownArrow: cursor = (cursor + 1) % items.Count; break;
                case ConsoleKey.Spacebar: selected[cursor] = !selected[cursor]; break;
                case ConsoleKey.A:
                    bool all = selected.All(s => s);
                    for (int i = 0; i < selected.Length; i++) selected[i] = !all;
                    break;
                case ConsoleKey.Enter: return selected.Any(s => s) ? "install" : "quit";
                case ConsoleKey.U: return "uninstall";
                case ConsoleKey.Escape: return "quit";
            }
        }
    }
    finally { Console.CursorVisible = true; }
}
