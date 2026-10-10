# Changelog

Each patch is versioned separately and released with a tag `<Patch>-v<version>`.

## Inventory 1.2.8
- Fixed: you could only pick up 4 items even with more slots. The host no longer reports the inventory as full at 4.

## Installer 2.2.0
- Checklist: [Delete]/[Backspace] uninstalls just the selected patch, without touching the others.
- The checklist redraws in place instead of clearing the screen on every key press, so it no longer flickers.
- Close button in the Patches window is drawn as a true ✕ (two crossed bars) instead of the letter "X".

## Fov 1.3.0
- Field of view slider (50-120°) in Patches → Fov; [ and ] change it in game: tap for 5° steps, hold to change smoothly.

## Revive 1.0.0
- Revive dead teammates by standing next to their body (host only).

## Installer 3.0.0
- Finds the game automatically (next to the exe, the game's own log, or Steam libraries); runs from anywhere.
- Windows installer window: tick patches, install/update, uninstall selected or all, progress bar and log, game-folder picker.
- Removing a single patch is safe (restores the original and reinstalls the rest); MIMESIS is closed automatically.
- Lists Fov and Revive.
- About 52 MB (Windows Forms can't be trimmed).

## Installer 2.1.1
- 3x smaller (12 MB instead of 38 MB) and a 30-minute download timeout, so it works on slow connections.

## Installer 2.1.0
- Lists the LateJoin patch.

## LateJoin 1.0.0
- Join a game in progress: late joiners wait, then enter at the tram when the team returns. Host and joiner need it.

## Inventory 1.2.7
- Stack counts / durability % follow their slot when fewer than 4 slots are shown.

## HudPercent 1.1.2
- Same gap after both bars (radiation % no longer touches its bar).

## Inventory 1.2.3
- The slot count is set only in Patches → Inventory (removed from the lobby menu).

## Inventory 1.2.2
- The stamina bar moves above the second slot row.

## Inventory 1.2.0
- Fixed the 5-8 slot layout: slots 1-4 stay exactly as the game draws them, slots 5-8 are a copy of that row placed above.

## Inventory 1.1.1
- Fixed: with fewer than 4 slots, the hidden slots still showed their stack counts.

## Installer 2.0.0
- Contains no patches: downloads each checked patch's files from its latest GitHub release.
- Checklist shows each patch's latest released version.

## Installer 1.2.0
- Downloads and opens the newest installer from GitHub on launch, so a shared copy always installs the latest patches.
- Key hints in brackets.

## Inventory 1.1.0
- Slot count goes down to 1 (range 1-8); slot buttons in the Patches window are a 2 × 4 grid.

## Installer 1.1.0
- Checklist to pick which patches to install (arrow keys + Space).

## Installer 1.0.0
- One exe that installs/updates or removes all patches.

## Inventory 1.0.0
- Host-set inventory size (1-8 slots), 2 × 4 grid HUD, shared with players through the Steam lobby. Every player needs it.

## HudPercent 1.1.1
- Radiation % now sits next to its bar.

## All patches (BiggerLobby 1.3.0, PSController 1.3.0, SpectatorCam 1.1.0, Minimap 1.4.0, HostOptions 1.1.0, HudPercent 1.1.0)
- One **Patches** chip on the main menu replaces the per-patch chips. It opens a window: installed patches on the left, the selected patch's options on the right, with update links.
- New options: BiggerLobby difficulty scaling on/off; PSController icons on/off; SpectatorCam, HudPercent and Minimap on/off; HostOptions stamina and starting money; Minimap settings moved here from its own window.

## Minimap 1.3.2
- Legend lists only enabled categories (no "You"); hidden when none are enabled.

## Minimap 1.3.1
- Bigger dots on the map (12 px instead of 7).

## HudPercent 1.0.0
- Health and radiation percentages next to the top-left HUD bars.

## HostOptions 1.0.3
- Fixed: patches failed to load (Harmony class split), the stamina checkbox had no label, and changing starting money in the lobby didn't update the funds.

## HostOptions 1.0.0
- Host-only lobby options under "Use Entry Password": **Infinite Stamina** and **Starting money**. Both run on the host, so other players don't need the patch.

## Minimap 1.3.0
- Colour legend under the map: You, plus Players / Monsters / Items when enabled.

## Minimap 1.2.2
- Graphic style scrolls smoothly: each render covers a larger area and the image slides with you between renders.

## Minimap 1.2.1
- Graphic style: the level is lit evenly for the minimap render only (dark rooms no longer show black); the game's own view is unchanged.

## Minimap 1.2.0
- Show options in the settings window: Players / Monsters / Items (off by default). Enabled ones get coloured dots, and models in Graphic style. Mimics count as monsters.

## Minimap 1.1.0
- Settings window: click the Minimap chip on the main menu to choose Reveal (Explored / Full map) and Style (Plain / Graphic).
- Graphic style: real top-down view from a camera above your head, with all players, mimics and monsters hidden from it.
- Moved to the bottom-left corner; works in the tram/lobby too (A* graph fallback).
- Main menu chips are hidden outside the main menu (all patches).

## SpectatorCam 1.0.0
- Free camera for dead players (F / R3), within 8 m of the spectated player and never through walls.

## Minimap 1.0.0
- Bottom-left floor-plan minimap built from the level's NavMesh: layout and your own arrow only.
- Explored-only or whole-map mode, switched by clicking the Minimap chip on the main menu. M toggles it in game.

## BiggerLobby 1.2.0
- Main menu chip turns orange when a newer release is available.

## PSController 1.2.0
- Main menu chip turns orange when a newer release is available.

## BiggerLobby 1.1.0
- Shows a "BiggerLobby v1.1.0" chip in the bottom-left of the main menu; clicking it opens this repo.
- Lobby list: "Copy invite code" centred under the player slots.

## PSController 1.1.0
- Shows a "PSController v1.1.0" chip in the bottom-left of the main menu; clicking it opens this repo.

## BiggerLobby 1.0.0
- Lobbies hold 10 players (fixed; the Steam lobby and all 4 join checks raised).
- Pause/lobby player list: 10 numbered slots with backgrounds; free slots shown as "Empty slot". Volume, mute, profile, kick and ping work for every player.
- End-of-level and death match result screens show up to 10 players (2 × 5 grid).
- Difficulty scales above 4 players: quota, shop prices, monster budget and mimic count.

## PSController 1.0.0
- PlayStation button icons (✕ ○ □ △, L1/R1/L2/R2, L3/R3) for every in-game prompt.
- Prefers the XInput (Xbox) pad so DS4Windows' Xbox 360 output reads correctly.
- Optional button-press debug log (`MIMESIS_Data/PSIcons/debug.txt`).
