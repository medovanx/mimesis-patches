// MIMESIS HudPercent Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: HudPercentPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS HudPercent Patcher", "health and radiation percentages on the HUD",
    plugin: "HudPercent.Plugin", runtimeDll: "HudPercentRuntime.dll");
