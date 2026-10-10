# MIMESIS Patches

Patches for MIMESIS (EA 0.3.1). No mod loader needed, and you can combine any of them.

![Main menu with the Patches chip bottom-left, and the Patches window open](screenshots/menu.png)

## Install
1. Download **`MimesisPatchesInstaller.exe`** from the [Releases page](https://github.com/medovanx/mimesis-patches/releases).
2. Run it, tick the patches you want and click **Install / update selected**. See [Installer](Installer/).
3. In game, click the **Patches** chip in the bottom-left of the main menu to turn patches on/off and change their options. It turns orange when an update is available.

![Installer window](Installer/screenshots/installer.png)

Each patch also has its own patcher exe if you prefer: close the game, put the exe next to `MIMESIS.exe` and run it. Your original game file is backed up as `Assembly-CSharp.dll.bak`.

## Patches

| Patch | Version | Host | Player | What it does |
|---|---|---|---|---|
| [BiggerLobby](BiggerLobby/) | 1.3.2 | Yes | No | 10-player lobbies, with UI for 10 and difficulty that scales with player count |
| [PSController](PSController/) | 1.3.2 | Optional | Optional | PlayStation button icons instead of Xbox ones |
| [SpectatorCam](SpectatorCam/) | 1.1.2 | Optional | Optional | Free spectator camera, limited to the area around the player you're watching |
| [Minimap](Minimap/) | 1.4.2 | Optional | Optional | Minimap: explored or full map, plain or graphic, optional players/monsters/items |
| [HostOptions](HostOptions/) | 1.2.0 | Yes | No | Lobby options: infinite stamina, starting money, difficulty and economy |
| [Inventory](Inventory/) | 1.2.9 | Yes | Yes | 1-8 inventory slots (2 × 4 grid), set by the host |
| [LateJoin](LateJoin/) | 1.0.2 | Yes | Yes | Join a game in progress; you enter at the tram when the team returns |
| [Fov](Fov/) | 1.3.1 | Optional | Optional | Field of view setting; [ and ] change it in game |
| [Revive](Revive/) | 1.0.1 | Yes | No | Stand next to a dead teammate to revive them |
| [HudPercent](HudPercent/) | 1.1.4 | Optional | Optional | Health and radiation percentages next to the HUD bars |

**Host / Player**: who needs the patch. *Yes* = required, *No* = not needed, *Optional* = personal feature, install it if you want it.

## Screenshots

<table>
<tr><td align="center"><a href="BiggerLobby/"><img src="BiggerLobby/screenshots/lobby.png" width="260"><br>BiggerLobby</a></td><td align="center"><a href="PSController/"><img src="PSController/screenshots/prompts.png" width="260"><br>PSController</a></td><td align="center"><a href="Minimap/"><img src="Minimap/screenshots/graphic.png" width="260"><br>Minimap</a></td></tr>
<tr><td align="center"><a href="HostOptions/"><img src="HostOptions/screenshots/options.png" width="260"><br>HostOptions</a></td><td align="center"><a href="Inventory/"><img src="Inventory/screenshots/inventory.png" width="260"><br>Inventory</a></td><td align="center"><a href="Fov/"><img src="Fov/screenshots/toast-120.png" width="260"><br>Fov</a></td></tr>
<tr><td align="center"><a href="HudPercent/"><img src="HudPercent/screenshots/hud.png" width="260"><br>HudPercent</a></td><td align="center"><a href="SpectatorCam/"><img src="SpectatorCam/screenshots/settings.png" width="260"><br>SpectatorCam</a></td><td align="center"><a href="LateJoin/"><img src="LateJoin/screenshots/settings.png" width="260"><br>LateJoin</a></td></tr>
<tr><td align="center"><a href="Revive/"><img src="Revive/screenshots/settings.png" width="260"><br>Revive</a></td></tr>
</table>

## Uninstall
Run the installer and click **Uninstall selected** or **Uninstall all**.

## Build
Use the .NET 10 SDK. The `.csproj` files expect the game at `O:\Games\MIMESIS v0.3.1`; change the paths if yours is elsewhere. `Common/` holds code shared by all patches.

See [CHANGELOG.md](CHANGELOG.md) for what changed in each version.
