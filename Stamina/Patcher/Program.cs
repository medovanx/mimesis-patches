// MIMESIS Stamina Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: StaminaPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS Stamina Patcher", "host-controlled infinite stamina",
    plugin: "Stamina.Plugin", runtimeDll: "StaminaRuntime.dll");
