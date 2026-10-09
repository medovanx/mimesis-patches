# Changelog

Each patch is versioned separately and released with a tag `<Patch>-v<version>`.

## Minimap 1.3.2
- Legend lists only enabled categories (no "You"); hidden when none are enabled.

## Minimap 1.3.1
- Bigger dots on the map (12 px instead of 7).

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
