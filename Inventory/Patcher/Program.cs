// MIMESIS Inventory Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: InventoryPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS Inventory Patcher", "host-set inventory size (1-8 slots)",
    plugin: "Inventory.Plugin", runtimeDll: "InventoryRuntime.dll");
