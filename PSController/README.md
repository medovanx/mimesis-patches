<img src="icon.png" width="128" align="right">

# PSController

**Version 1.4.0**

Shows PlayStation button icons (✕ ○ □ △, L1/R1/L2/R2, L3/R3) instead of Xbox ones.

![In-game HUD showing PlayStation button prompts (L1/R1/Cross etc.)](screenshots/prompts.png)

The full icon set:

![PlayStation icon set](screenshots/icons.png)

**Only you need it.**

## Install
Use the [installer](../Installer/), or install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into the game folder, run the game once, then copy `PSControllerRuntime.dll` from this patch's [release](https://github.com/medovanx/mimesis-patches/releases) into `BepInEx\plugins`. Also unzip `PSIcons.zip` into a `PSIcons` folder next to the DLL.

## Controller setup
Use **DS4Windows** so your PS4/PS5 controller is read reliably:
- Set the output to **Xbox 360**. Don't use DualShock 4 output: L1 registers as LT and rebinding saves the wrong buttons. The PS icons come from this patch either way.
- Turn on **Hide DS4 Controller** (HidHide) so the game only sees the virtual pad.
- Tip: in DS4Windows **Auto Profiles**, load your Xbox profile automatically when `MIMESIS.exe` runs.
- After switching, use **Reset keys** in the game's controls menu, since old binds may be wrong.

| Your button | Icon shown |
|---|---|
| ✕ ○ □ △ | ✕ ○ □ △ |
| L1 / R1 | L1 / R1 |
| L2 / R2 | L2 / R2 |
| L3 / R3 (stick clicks) | L3 / R3 |

You can replace the icons in `MIMESIS_Data/PSIcons/` (74×74 PNGs) with your own.

## Options

![Patches > PSController settings](screenshots/settings.png)

**Patches > PSController** on the main menu: turn it on/off.

## Troubleshooting
`Player.log` has a `[PSController] Gamepads: ...` line showing which pad the game uses. To log every button press, create an empty `MIMESIS_Data/PSIcons/debug.txt`; delete it when done.

## Uninstall
Use the installer's **Uninstall selected**, or delete `PSControllerRuntime.dll` from `BepInEx\plugins`.

## Build
`dotnet build -c Release` in `Runtime`.
