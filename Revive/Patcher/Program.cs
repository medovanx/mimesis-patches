// MIMESIS Revive Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: RevivePatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS Revive Patcher", "revive dead teammates",
    plugin: "Revive.Plugin", runtimeDll: "ReviveRuntime.dll");
