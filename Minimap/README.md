# Minimap (MinimapPatcher)

**Version 1.0.4**

A floor-plan minimap in the bottom-left corner with your position and facing.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `MinimapPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

Each player installs it for themselves. In game, **M** shows or hides the minimap.

## Modes
Click the **Minimap** chip on the main menu to switch (the chip shows the current mode; it's remembered):
- **Explored only** (default): the floor appears as you walk near it.
- **Whole map**: the full floor plan of the level from the start.

## What it shows (and doesn't)
- The map is drawn from the level's walkable floor (the NavMesh the game builds at level load), so it contains **only the layout and your own arrow**. No teammates, mimics, monsters or items, so a "teammate" can still be a mimic.
- Only the floor you're on is drawn; other floors appear faint.
- Hidden while you're dead, in menus, or outside a level.

## Files installed
- `MIMESIS_Data/Managed/MinimapRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `Minimap.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)

Settings (view size, reveal radius, floor tolerance) are constants at the top of `Runtime/Minimap.cs`.
