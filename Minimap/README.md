# Minimap

**Version 1.4.2**

A minimap in the bottom-left corner showing your position and facing.

![Graphic minimap in the corner with player/monster/item dots](screenshots/graphic.png)

![Plain minimap style](screenshots/plain.png)

**Only you need it.**

## Install
Use the [installer](../Installer/), or: close the game, put `MinimapPatcher.exe` next to `MIMESIS.exe`, run it and choose install (`i`).

## Controls
**M** shows or hides the minimap. It's hidden while you're dead or in menus.

## Options

![Patches > Minimap settings](screenshots/settings.png)

**Patches > Minimap** on the main menu. Settings are remembered.

| Setting | Options |
|---|---|
| Reveal | **Explored** (default): the map appears as you walk near it. **Full map**: everything from the start. |
| Style | **Plain** (default): a clean floor plan of your current floor. **Graphic**: a real top-down view of the level, with dark rooms lit up on the map only. |
| Show | **Players** / **Monsters** / **Items**, each on or off (all off by default). They appear as dots: players blue, monsters red, items yellow. A legend under the map lists them. |

Settings combine: Graphic + Explored shows the real view with unvisited areas darkened.

By default you only see the level and your own arrow, so a "teammate" can still be a mimic. Mimics count as **monsters**: with Players on and Monsters off, a "player" without a dot is a mimic. That changes how the game plays, so it's off unless you turn it on.

## Uninstall
Run `MinimapPatcher.exe` and choose `u`, or use the installer.
