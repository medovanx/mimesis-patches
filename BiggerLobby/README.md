# BiggerLobby

**Version 1.3.1**

Lobbies hold **10 players**, with the UI and difficulty adjusted to match.

![Lobby screen with 10 player slots filled](screenshots/lobby.png)

**Only the host needs it.**

## Install
Use the [installer](../Installer/), or: close the game, put `MorePlayersPatcher.exe` next to `MIMESIS.exe` and run it.

## What you get
- **Player list** (pause/lobby menu): 10 rows, each with volume, mute, profile, kick (host) and ping. Free slots show as **Empty slot**.
- **Result screens**: with more than 4 players, everyone is shown in a 2 × 5 grid.
- **Difficulty scaling** above 4 players:

| What | Formula | 6 players | 8 players | 10 players |
|---|---|---|---|---|
| Quota | × players ÷ 4 | ×1.5 | ×2 | ×2.5 |
| Crow shop / vending prices | +12.5% per player above 4 | ×1.25 | ×1.5 | ×1.75 |
| Monster threat (normal monsters) | +10% per player above 4 | ×1.2 | ×1.4 | ×1.6 |
| Mimics per stage | +1 per 3 players above 4 | +0 | +1 | +2 |

With 4 players or fewer, nothing changes. Loot isn't scaled. Saves stay correct if you load them with a different group size.

## Options

![Patches > BiggerLobby settings](screenshots/settings.png)

**Patches > BiggerLobby** on the main menu: turn difficulty scaling on/off.

## Uninstall
Use the installer's **Uninstall selected**, or copy `Assembly-CSharp.dll.bak` back over `Assembly-CSharp.dll` in `MIMESIS_Data/Managed`.

## Build
Build `Runtime` (`dotnet build -c Release`), then `dotnet publish -c Release` in `Patcher`.
