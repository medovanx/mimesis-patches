# MIMESIS Patches Installer

**Version 3.0.0**

One exe that installs, updates or removes **every patch** at once.

## Always up to date
The installer contains no patch files. It downloads each checked patch's files from that patch's **latest GitHub release**, so the exe you share always installs the newest patches. If the install logic itself was updated, it first downloads and opens the newest installer (`MimesisPatchesInstaller-vX.Y.Z.exe` next to it). It needs an internet connection.

## Use
1. Run `MimesisPatchesInstaller.exe` from anywhere. It finds the game automatically: next to the exe, from the path the game writes in its log (`%USERPROFILE%\AppData\LocalLow\ReLUGames\MIMESIS\Player.log`, so the game must have been started once), or from your Steam libraries. If it can't, use **Change game folder**.
2. Tick the patches you want and click **Install / update selected**. **Uninstall selected** removes only the ticked ones; **Uninstall all** restores the original game files.
3. The list shows each patch's latest version, who needs it, and whether it's installed. Progress and details appear in the log at the bottom.

MIMESIS is closed automatically while patching. Windows may show a SmartScreen warning because the exe isn't signed: **More info → Run anyway**.

Command line: `MimesisPatchesInstaller.exe install | uninstall | uninstall:<Patch> [path to Assembly-CSharp.dll]`.

## Build
`dotnet publish -c Release` here. It compiles in `Common/PatcherCore.cs` and the BiggerLobby/PSController `Setup.cs` (the install logic) and downloads everything else. Each patch release must therefore include its runtime DLL plus `0Harmony.dll` (PSController: `PSIcons.zip` instead).
