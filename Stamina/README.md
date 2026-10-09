# Stamina (StaminaPatcher)

**Version 1.0.0**

Lets the host turn on **infinite stamina** for everyone in the lobby.
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `StaminaPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

**Only the host needs it.** In the lobby menu (Esc), the host gets an **Infinite Stamina** checkbox under "Use Entry Password". Other players never see it and don't need the patch: their stamina bar simply stays full.

The setting is saved on the host's PC, and the main menu chip shows it (`Stamina v1.0.0 · Infinite` / `· Normal`).

## How it works
Stamina is simulated on the host: the server-side movement code drains it through `StatController.ConsumeStamina`, and every player's game only displays the value the host sends. While the checkbox is on, the host skips that drain for **players** (`VPlayer`), so nobody runs out. Monsters are unaffected.

## Files installed
- `MIMESIS_Data/Managed/StaminaRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `Stamina.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)
