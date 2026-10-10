# HostOptions

**Version 1.2.0**

Extra lobby options for the host: **infinite stamina**, **starting money**, and **difficulty and economy** settings.

![Patches > HostOptions page: infinite stamina and starting money](screenshots/options.png)

**Only the host needs it.** Other players don't see the controls.

## Install
Use the [installer](../Installer/), or: close the game, put `HostOptionsPatcher.exe` next to `MIMESIS.exe`, run it and choose install (`i`).

## Options

![Patches > HostOptions settings](screenshots/settings.png)

In the lobby menu (Esc), under "Use Entry Password", or in **Patches > HostOptions** on the main menu:
- **Infinite Stamina**: nobody's stamina drains. Monsters are unaffected.
- **STARTING MONEY**: the funds a run starts with (game default $15). Type a value and press Enter or click away. Empty or invalid resets to the default. Changing it before the first departure also sets your current funds.

In **Patches > HostOptions** only:
- **Difficulty**: Easy, Normal or Hard sets the four sliders below. Move any slider for a custom mix.
- **Quota / repair**: money needed to repair the tram (25%-300%).
- **Shop prices**: cost of shop items.
- **Sell value**: money you get for selling items.
- **Monsters**: how many monsters and mimics spawn in a level.
- **Apply to**: *All games* (default) also uses these on runs you continue from a save; *New games only* leaves loaded saves at normal values.

| Preset | Quota | Prices | Sell | Monsters |
|---|---|---|---|---|
| Easy | 75% | 75% | 125% | 60% |
| Normal | 100% | 100% | 100% | 100% |
| Hard | 125% | 125% | 90% | 150% |

These stack with BiggerLobby's scaling for big groups.

Settings are saved on your PC.

## Uninstall
Run `HostOptionsPatcher.exe` and choose `u`, or use the installer.
