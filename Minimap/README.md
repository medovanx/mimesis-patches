# Minimap (MinimapPatcher)

**Version 1.1.0**

A minimap in the bottom-left corner with your position and facing.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `MinimapPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

Each player installs it for themselves. In game, **M** shows or hides the minimap.

## Settings
Click the **Minimap** chip on the main menu to open the settings window. The chip shows the current choice, and it's remembered between sessions.

| Setting | Options |
|---|---|
| Reveal | **Explored** (default): the map appears as you walk near it. **Full map**: everything from the start. |
| Style | **Plain** (default): a clean floor plan. **Graphic**: a real top-down view of the level. |

The two combine: Graphic + Explored shows the real view with unvisited areas darkened.

## What it shows (and doesn't)
Only **the level and your own arrow**. No teammates, mimics or monsters, so a "teammate" can still be a mimic.
- **Plain** is drawn from the level's walkable floor (the Unity NavMesh in levels, the A* pathfinding graph in the tram/lobby), which never contains actors. Only the floor you're on is drawn; other floors appear faint.
- **Graphic** renders the level with an extra camera just above your head looking down. Every player, mimic and monster is switched off for that camera's render only, so they never appear on it. It renders at 256 px about 7 times a second to keep the FPS cost low.
- Hidden while you're dead, in menus, or outside a game scene.

## Files installed
- `MIMESIS_Data/Managed/MinimapRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `Minimap.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)

Tuning values (view size, reveal radius, graphic resolution and rate) are constants at the top of `Runtime/Minimap.cs`.
