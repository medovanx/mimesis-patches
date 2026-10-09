# MIMESIS Patches Installer

**Version 1.2.0**

One exe that installs, updates or removes **every patch** at once.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Always up to date
On launch the installer checks GitHub. If a newer installer has been released, it downloads it next to itself (`MimesisPatchesInstaller-vX.Y.Z.exe`), opens it and closes; the new one carries the newest version of every patch. So you can share this one exe and friends always get the latest patches. Offline, it just installs what it has.

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
Build every patch's `Runtime` first (`dotnet build -c Release`), then `dotnet publish -c Release` here. It compiles in `Common/PatcherCore.cs` and the BiggerLobby/PSController `Setup.cs`, and embeds all runtime DLLs, Harmony and the PS icons.
