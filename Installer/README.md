# MIMESIS Patches Installer

**Version 3.0.0**

Install, update or remove **every patch** from one exe.

## Use
1. Download `MimesisPatchesInstaller.exe` from the [Releases page](https://github.com/medovanx/mimesis-patches/releases) and run it from anywhere. It finds your game automatically; if not, click **Change game folder**.
2. Tick the patches you want and click **Install / update selected**.
3. To remove patches, click **Uninstall selected** (ticked ones only) or **Uninstall all** (restores the original game files).

The list shows each patch's latest version, who needs it, and whether you have it installed.

Good to know:
- You need an internet connection. The installer always downloads the latest patches, and updates itself when needed.
- MIMESIS is closed automatically while patching.
- If Windows shows a SmartScreen warning, click **More info > Run anyway** (the exe isn't signed).
- If it can't find your game, start MIMESIS once and try again.

Command line: `MimesisPatchesInstaller.exe install | uninstall | uninstall:<Patch> [path to Assembly-CSharp.dll]`.

## Build
`dotnet publish -c Release` in this folder.
