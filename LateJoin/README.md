# LateJoin (LateJoinPatcher)

**Version 1.0.0**

Lets friends **join a game that's already running**. If the team is in a level, the new player waits on a message and joins at the tram when the team returns, then plays from the next stage.

## Use
1. Close the game.
2. Put `LateJoinPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

**The host and the joining player both need it.** The host can turn it off in **Patches → LateJoin** (on by default).

## How it works
Vanilla blocks joining once the team departs: the host refuses new players outside the lobby, the tram/lobby room is frozen while the team is in a level, and a joining player can only load into the tram.
- **Host**: accepts joins at any time (`GameSessionInfo.CanEnterSession`) and publishes "team is in a level" in the Steam lobby data (`medovanx.inlevel`).
- **Joining player**: after connecting, waits (`MaintenanceScene.TryEnterMaintenanceRoom`) until that flag clears, then enters the tram normally.

Dropping someone straight into a running level isn't supported: the game has no way to load a level mid-run for a new player.

## Files installed
- `MIMESIS_Data/Managed/LateJoinRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `LateJoin.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)
