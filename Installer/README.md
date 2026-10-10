# MIMESIS Patches Installer

**Version 4.0.0**

Install, update or remove **every patch** from one exe.

![Installer window with the patch cards (one Installed, one Download)](screenshots/installer.png)

## Use
1. Download `MimesisPatchesInstaller.exe` from the [Releases page](https://github.com/medovanx/mimesis-patches/releases) and run it from anywhere. It finds your game automatically; if not, click **Change game folder**.
2. Tick the patches you want and click **Install / update selected**. If BepInEx 5 isn't in your game folder, the installer adds it (5.4.23.5) first. Patches go into `BepInEx\plugins\MimesisPatches\` (PSController also gets a `PSIcons` folder there).
3. To remove patches, click **Uninstall selected** (ticked ones only) or **Uninstall all**. BepInEx stays installed.

The list shows each patch's latest version, who needs it, and whether you have it installed. You can also update from inside the game: **Patches** window > **Update** / **Update all**.

Good to know:
- You need an internet connection. The installer always downloads the latest patches, and updates itself when needed.
- MIMESIS is closed automatically while installing.
- Your game files are never modified. If you used an older version of the installer, it cleans that install up for you (restores the original `Assembly-CSharp.dll` and removes the old files).
- If Windows shows a SmartScreen warning, click **More info > Run anyway** (the exe isn't signed).
- If it can't find your game, start MIMESIS once and try again.

Command line: `MimesisPatchesInstaller.exe install | install:<Patch> | uninstall | uninstall:<Patch> [game folder]`. Without a folder it finds the game on its own.

## Build
`dotnet publish -c Release` in this folder.
