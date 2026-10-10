// MIMESIS Fov Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: FovPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS Fov Patcher", "field of view setting",
    plugin: "Fov.Plugin", runtimeDll: "FovRuntime.dll");
