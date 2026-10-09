# MIMESIS Patches

Patches for MIMESIS (EA 0.3.1) by Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

No mod loader is needed. Each patch is a standalone exe that edits `MIMESIS_Data/Managed/Assembly-CSharp.dll` directly with Mono.Cecil. Patches can be combined in any order. A **Patches** chip in the bottom-left of the main menu opens a window listing every installed patch (left) with its options (right): turn patches on/off and change their settings. It turns orange when an update is available, and each patch page links to its new release. The first run backs up the original as `Assembly-CSharp.dll.bak`.

| Patch | Version | What it does |
|---|---|---|
| [BiggerLobby](BiggerLobby/) | 1.3.0 | 10-player lobbies, with UI for 10 and difficulty that scales with player count |
| [PSController](PSController/) | 1.3.0 | Replaces Xbox button icons with PlayStation icons |
| [SpectatorCam](SpectatorCam/) | 1.1.0 | Free spectator camera, limited to the area around the player you're watching |
| [Minimap](Minimap/) | 1.4.0 | Minimap: explored or full map, plain or graphic, optional players/monsters/items |
| [HostOptions](HostOptions/) | 1.1.0 | Host lobby options: infinite stamina, starting money (only the host needs it) |
| [HudPercent](HudPercent/) | 1.1.0 | Health and radiation percentages next to the HUD bars |

Each patch has its own folder and README. `Common/` holds code shared by all of them (the Patches window + update check, option-page UI helpers, patcher core). Build with the .NET 10 SDK; the `.csproj` files expect the game at `O:\Games\MIMESIS v0.3.1`, so change the paths there if yours is elsewhere.

Downloads are on the [Releases page](https://github.com/medovanx/mimesis-patches/releases). See [CHANGELOG.md](CHANGELOG.md) for what changed in each version.
