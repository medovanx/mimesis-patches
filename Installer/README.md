# MIMESIS Patches Installer

**Version 1.0.0**

One exe that installs, updates or removes **every patch** at once.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `MimesisPatchesInstaller.exe` next to `MIMESIS.exe` and run it.
3. Press Enter (or `i`) to install/update all, or `u` to uninstall all.

- **Install/update** runs each patch's own install step (the same code as the single patchers), so running it again after a new release updates everything.
- **Uninstall** restores the original `Assembly-CSharp.dll` from the backup and deletes every patch file.
- Command line: `MimesisPatchesInstaller.exe install|uninstall [path to Assembly-CSharp.dll]`.

Afterwards, click **Patches** on the main menu to turn individual patches on/off and change their settings.

## Build
Build every patch's `Runtime` first (`dotnet build -c Release`), then `dotnet publish -c Release` here. It compiles in `Common/PatcherCore.cs` and the BiggerLobby/PSController `Setup.cs`, and embeds all runtime DLLs, Harmony and the PS icons.
