// MIMESIS SpectatorCam Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: SpectatorCamPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS SpectatorCam Patcher", "free spectator camera",
    plugin: "SpectatorCam.Plugin", runtimeDll: "SpectatorCamRuntime.dll");
