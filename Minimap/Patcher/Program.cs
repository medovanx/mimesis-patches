// MIMESIS Minimap Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: MinimapPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS Minimap Patcher", "floor-plan minimap",
    plugin: "Minimap.Plugin", runtimeDll: "MinimapRuntime.dll");
