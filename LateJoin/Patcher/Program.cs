// MIMESIS LateJoin Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: LateJoinPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS LateJoin Patcher", "join a game in progress",
    plugin: "LateJoin.Plugin", runtimeDll: "LateJoinRuntime.dll");
