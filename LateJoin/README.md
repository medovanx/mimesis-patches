# LateJoin

**Version 1.1.0**

Lets friends **join a game that's already running**. If the team is in a level, you wait on a message and join at the tram when they return, then play from the next stage.

**The host and the joining player both need it.**

## Install
Use the [installer](../Installer/), or install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into the game folder, run the game once, then copy `LateJoinRuntime.dll` from this patch's [release](https://github.com/medovanx/mimesis-patches/releases) into `BepInEx\plugins`.

## Options

![Patches > LateJoin settings](screenshots/settings.png)

**Patches > LateJoin** on the main menu: the host can turn it off (on by default).

You can't drop straight into a level in progress; you join at the tram.

## Uninstall
Use the installer's **Uninstall selected**, or delete `LateJoinRuntime.dll` from `BepInEx\plugins`.
