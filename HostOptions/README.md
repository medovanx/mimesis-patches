# HostOptions (HostOptionsPatcher)

**Version 1.1.0**

Extra lobby options for the host: **infinite stamina** and **starting money**.

## Use
1. Close the game.
2. Put `HostOptionsPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

**Only the host needs it.** In the lobby menu (Esc), under "Use Entry Password", the host gets:
- **Infinite Stamina** (checkbox): nobody's stamina drains.
- **STARTING MONEY** (number field): the funds a run starts with (the game's default is $15). Type a value and press Enter or click away. Empty or invalid resets to the default.

Other players never see these controls and don't need the patch. Settings are saved on the host's PC. They can also be changed in **Patches → HostOptions** on the main menu.

## How it works
Both are simulated on the host, and every player's game only displays what the host sends:
- **Stamina**: the server-side movement code drains it through `StatController.ConsumeStamina`. While the option is on, the host skips that for players (`VPlayer`). Monsters are unaffected.
- **Money**: the lobby room (`MaintenanceRoom`) sets its funds from `C_InitialMoney` when it's created and when a new run starts. The patch replaces that value. Changing it in the lobby before the run's first departure also sets the current funds to that amount. `Player.log` notes each change.

## Files installed
- `MIMESIS_Data/Managed/HostOptionsRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `HostOptions.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)
