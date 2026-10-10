# Minimap (MinimapPatcher)

**Version 1.4.0**

A minimap in the bottom-left corner with your position and facing.

## Use
1. Close the game.
2. Put `MinimapPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

Each player installs it for themselves. In game, **M** shows or hides the minimap.

## Settings
Open **Patches → Minimap** on the main menu to change these; they're remembered between sessions.

| Setting | Options |
|---|---|
| Reveal | **Explored** (default): the map appears as you walk near it. **Full map**: everything from the start. |
| Style | **Plain** (default): a clean floor plan. **Graphic**: a real top-down view of the level. |
| Show | **Players** / **Monsters** / **Items**, each on or off (all off by default). Enabled ones appear as dots: players blue, monsters red, items yellow; in Graphic their models are visible too. |

A legend under the map lists the colour of each enabled category (hidden when none are enabled).

The settings combine: Graphic + Explored shows the real view with unvisited areas darkened.

## What it shows (and doesn't)
By default only **the level and your own arrow**: no teammates, mimics, monsters or items, so a "teammate" can still be a mimic.

The **Show** options change that. Mimics count as **monsters**, so turning Players on while Monsters stays off lets you tell real players from mimics: a "player" with no dot is a mimic. That's a big change to how the game plays, so it's off unless you choose it.
- **Plain** is drawn from the level's walkable floor (the Unity NavMesh in levels, the A* pathfinding graph in the tram/lobby), which never contains actors. Only the floor you're on is drawn; other floors appear faint.
- **Graphic** renders the level with an extra camera just above your head looking down. Your own character and every category you haven't enabled are switched off for that camera's render only, so they never appear on it. It's lit evenly for that render only, so dark rooms are readable on the map while your own view stays dark. It renders about 7 times a second (a slightly larger area than shown, so the image scrolls smoothly in between) to keep the FPS cost low.
- Hidden while you're dead, in menus, or outside a game scene.

## Files installed
- `MIMESIS_Data/Managed/MinimapRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `Minimap.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)

Tuning values (view size, reveal radius, graphic resolution and rate) are constants at the top of `Runtime/Minimap.cs`.
