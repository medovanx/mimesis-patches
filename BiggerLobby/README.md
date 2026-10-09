# BiggerLobby (MorePlayersPatcher)

Lets MIMESIS lobbies hold **10 players** and adapts the UI and difficulty to them.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `MorePlayersPatcher.exe` next to `MIMESIS.exe` and run it.

Only the host needs it: joining, UI layout and difficulty all run on the host. The limit is fixed at 10 because the UI layouts are built for exactly 10.

## What it changes

### Player limit
- The 4 join checks that compare against `C_MaxPlayerCount` (`GameSessionInfo.AddPlayerSteamID`, `IVroom.CanEnterChannel`, `VRoomManager` waiting/maintenance room entry) read `BiggerLobbyConfig.MaxPlayers` (10).
- The Steam lobby is created with 10 slots instead of 4.

### UI (`BiggerLobbyRuntime.dll`, applied with Harmony when the game starts)
- **Pause/lobby player list**: 10 numbered rows instead of 4. Each player gets a volume slider, mute, profile, kick (host) and ping icon. Free slots show as dimmed **Empty slot** rows.
- **End-of-level result** (survived / killed / abandoned + awards) and **death match result**: with more than 4 players, all of them are shown in a 2 × 5 grid.
- The speaking icon is attached to each character, so it already worked for any number of players.

### Difficulty scaling
The game is balanced for 4 players and never looks at the player count. Above 4, values are scaled by the number of players in the session (counted on the host when each value is used):

| What | Formula | 6 players | 8 players | 10 players |
|---|---|---|---|---|
| Quota (money needed to continue) | × players ÷ 4 | ×1.5 | ×2 | ×2.5 |
| Crow shop / vending prices | × (1 + 12.5% per player above 4) | ×1.25 | ×1.5 | ×1.75 |
| Monster threat budget (normal monsters) | × (1 + 10% per player above 4) | ×1.2 | ×1.4 | ×1.6 |
| Mimics per stage | + 1 per 3 players above 4 | +0 | +1 | +2 |

- At 4 players or fewer nothing changes.
- The quota follows the current player count (it's scaled whenever the game reads it). Saves keep the unscaled value, so loading a save with a different group size stays correct.
- Loot isn't scaled; the higher quota accounts for more people collecting.
- Tram upgrades are free picks (one of two), so there's no price to scale.
- `Player.log` shows the factors each stage: `[BiggerLobby] Scaling for N players: ...`.

The formulas live in `Runtime/ScalingPatch.cs`.

## Files installed
- `MIMESIS_Data/Managed/Assembly-CSharp.dll`: patched (original kept as `Assembly-CSharp.dll.bak`; copy it back to undo)
- `MIMESIS_Data/Managed/BiggerLobbyRuntime.dll`: UI and scaling patches
- `MIMESIS_Data/Managed/0Harmony.dll`: [Harmony](https://github.com/pardeike/Harmony), the runtime patching library

It patches the current `Assembly-CSharp.dll`, so other patches (PSController) are kept.

## Layout
- `Runtime/`: `BiggerLobbyRuntime.dll` (net472, Harmony). `Plugin.cs` starts it from `Hub.Awake`; `InGameMenuPatch.cs`, `ResultScreenPatch.cs` and `ScalingPatch.cs` hold the patches.
- `Patcher/`: the exe; it embeds the runtime DLL and Harmony.

## Build
Build `Runtime` first (`dotnet build -c Release`), then `dotnet publish -c Release` in `Patcher`.
