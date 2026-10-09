# MIMESIS Patches

Patches for MIMESIS (EA 0.3.1) by Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

No mod loader is needed. Each patch is a standalone exe that edits `MIMESIS_Data/Managed/Assembly-CSharp.dll` directly with Mono.Cecil. Patches can be combined in any order. The first run backs up the original as `Assembly-CSharp.dll.bak`.

| Patch | What it does |
|---|---|
| [BiggerLobby](BiggerLobby/) | Raises the lobby limit from 4 to a chosen player count (default 10) |
| [PSController](PSController/) | Replaces Xbox button icons with PlayStation icons |

Each patch has its own folder and README. Build with the .NET 10 SDK; the `.csproj` files expect the game at `O:\Games\MIMESIS v0.3.1`, so change the paths there if yours is elsewhere.
