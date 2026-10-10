# Revive

**Version 1.1.0**

**Revive a dead teammate** during a level: stand next to their body for a few seconds and they come back with low health.

**Only the host needs it.**

## Install
Use the [installer](../Installer/), or install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into the game folder, run the game once, then copy `ReviveRuntime.dll` from this patch's [release](https://github.com/medovanx/mimesis-patches/releases) into `BepInEx\plugins`.

## Options

![Patches > Revive settings](screenshots/settings.png)

**Patches > Revive** on the main menu:
- On/off
- Hold time: 3 / 5 / 10 s (default 5 s)
- Per player: once per level (default) or unlimited

There's no progress bar yet; just stay next to the body.

## Uninstall
Use the installer's **Uninstall selected**, or delete `ReviveRuntime.dll` from `BepInEx\plugins`.
