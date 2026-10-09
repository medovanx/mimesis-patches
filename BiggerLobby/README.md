# BiggerLobby (MorePlayersPatcher)

Raises the MIMESIS lobby limit from 4 players (default 10, choose 2–32).
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `MorePlayersPatcher.exe` next to `MIMESIS.exe` and run it.
3. Enter a player count (Enter = 10).

Only the host needs it. Re-run any time to change the number.

## What it changes
- The 4 join checks that compare against `C_MaxPlayerCount` (`GameSessionInfo.AddPlayerSteamID`, `IVroom.CanEnterChannel`, `VRoomManager` waiting/maintenance room entry) now read `BiggerLobbyConfig.MaxPlayers`.
- The Steam lobby is created with `MaxPlayers` slots instead of 4.
- The results screen and a test helper are left at 4 (fixed UI slots).

It patches the current `Assembly-CSharp.dll`, so other patches (PSController) are kept. The first run saves `Assembly-CSharp.dll.bak`; copy that back to undo.

## Build
`dotnet publish -c Release` produces a single-file `MorePlayersPatcher.exe`.
