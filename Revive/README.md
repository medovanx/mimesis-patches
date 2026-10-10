# Revive (RevivePatcher)

**Version 1.0.0**

Lets you **revive a dead teammate** during a level: stand next to their body for a few seconds and they come back with low health.

## Use
1. Close the game.
2. Put `RevivePatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

**Only the host needs it.** Settings in **Patches → Revive**: on/off, hold time (3 / 5 / 10 s), and once per level or unlimited per player (default: 5 s, once per level).

## How it works
The game already has a server-side revive (`VPlayer.Revive`) that it uses to bring the host back at the tram; it resets the player and tells every client, which takes them out of spectating. The patch runs it during a level (`DungeonRoom.OnUpdate`): when a living player stays within 1.8 m of a dead teammate's body for the hold time, the host revives them on the spot. The game's own revive rule sets the starting health.

There's no progress bar or prompt yet; just stay next to the body.

## Files installed
- `MIMESIS_Data/Managed/ReviveRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `Revive.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)
