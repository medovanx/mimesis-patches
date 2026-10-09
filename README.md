# MIMESIS Patches

Patches for MIMESIS (EA 0.3.1) by Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

No mod loader is needed. Each patch is a standalone exe that edits `MIMESIS_Data/Managed/Assembly-CSharp.dll` directly with Mono.Cecil. Patches can be combined in any order. Installed patches show as chips (e.g. `BiggerLobby v1.1.0`) in the bottom-left of the main menu; clicking one opens this repo. A chip turns orange when a newer release is available; clicking it then opens that release. The first run backs up the original as `Assembly-CSharp.dll.bak`.

| Patch | Version | What it does |
|---|---|---|
| [BiggerLobby](BiggerLobby/) | 1.2.0 | 10-player lobbies, with UI for 10 and difficulty that scales with player count |
| [PSController](PSController/) | 1.2.0 | Replaces Xbox button icons with PlayStation icons |
| [SpectatorCam](SpectatorCam/) | 1.0.0 | Free spectator camera, limited to the area around the player you're watching |
| [Minimap](Minimap/) | 1.3.2 | Minimap: explored or full map, plain or graphic, optional players/monsters/items |
| [HostOptions](HostOptions/) | 1.0.1 | Host lobby options: infinite stamina, starting money (only the host needs it) |

Each patch has its own folder and README. `Common/` holds code shared by all of them (main menu chip + update check, patcher core). Build with the .NET 10 SDK; the `.csproj` files expect the game at `O:\Games\MIMESIS v0.3.1`, so change the paths there if yours is elsewhere.

Downloads are on the [Releases page](https://github.com/medovanx/mimesis-patches/releases). See [CHANGELOG.md](CHANGELOG.md) for what changed in each version.
