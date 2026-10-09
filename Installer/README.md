# MIMESIS Patches Installer

**Version 2.1.0**

One exe that installs, updates or removes **every patch** at once.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Always up to date
The installer contains no patch files. It downloads each checked patch's files from that patch's **latest GitHub release**, so the exe you share always installs the newest patches. If the install logic itself was updated, it first downloads and opens the newest installer (`MimesisPatchesInstaller-vX.Y.Z.exe` next to it). It needs an internet connection.

## Use
1. Close the game.
2. Put `MimesisPatchesInstaller.exe` next to `MIMESIS.exe` and run it.
3. A checklist opens with every patch checked (already-installed ones are marked):
   - **Up/Down** move, **Space** check/uncheck, **A** all/none
   - **Enter** installs/updates the checked patches; unchecked ones are skipped (left as they are if already installed)
   - **U** uninstalls everything, **Esc** quits

- **Install/update** runs each patch's own install step (the same code as the single patchers), so running it again after a new release updates everything.
- **Uninstall** restores the original `Assembly-CSharp.dll` from the backup and deletes every patch file.
- Command line: `MimesisPatchesInstaller.exe install|uninstall [path to Assembly-CSharp.dll]`.

Afterwards, click **Patches** on the main menu to turn individual patches on/off and change their settings.

## Build
`dotnet publish -c Release` here. It compiles in `Common/PatcherCore.cs` and the BiggerLobby/PSController `Setup.cs` (the install logic) and downloads everything else. Each patch release must therefore include its runtime DLL plus `0Harmony.dll` (PSController: `PSIcons.zip` instead).
