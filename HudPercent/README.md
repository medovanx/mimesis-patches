# HudPercent (HudPercentPatcher)

**Version 1.1.2**

Shows your **health** and **radiation** as percentages next to the bars in the top-left HUD.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `HudPercentPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

Each player installs it for themselves; it only changes your own HUD.

- **Health**: how much you have left (100% = full health).
- **Radiation**: how contaminated you are (100% = full bar).

## How it works
The HUD (`UIPrefab_InGame`) updates its bars through `OnHpChanged` and `OnContaChanged`. Postfixes on those write the same values as a percentage into a label attached to the right edge of each bar, styled like the HUD's money text.

## Files installed
- `MIMESIS_Data/Managed/HudPercentRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `HudPercent.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)

## Settings
Open **Patches → HudPercent** on the main menu to turn it on/off.
