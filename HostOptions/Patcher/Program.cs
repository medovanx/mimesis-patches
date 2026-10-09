// MIMESIS HostOptions Patcher
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

// Usage: HostOptionsPatcher [install|uninstall] [path to Assembly-CSharp.dll]
MimesisPatches.PatcherCore.Main(args, "MIMESIS HostOptions Patcher", "host lobby options: infinite stamina, starting money",
    plugin: "HostOptions.Plugin", runtimeDll: "HostOptionsRuntime.dll");
