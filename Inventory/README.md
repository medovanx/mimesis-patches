# Inventory (InventoryPatcher)

**Version 1.2.3**

Lets the host set the inventory size to **1-8 slots**. More than 4 shows as a **2 × 4 grid** (slots 1-4 on the bottom row, 5-8 above).
By Mohamed Darwesh ([@medovanx](https://github.com/medovanx)).

## Use
1. Close the game.
2. Put `InventoryPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

**Every player needs it.** The host picks the number in **Patches → Inventory** on the main menu; everyone else gets it from the host automatically. It takes effect when characters spawn (the next level, or after reloading the lobby).

A player without the patch keeps 4 slots and can run into problems when the host's game hands them items in slots 5-8, so make sure everyone installs it before raising the number.

## How it works
- **Host**: `InventoryController.Reset` builds slots 1..4 in a fixed loop; the patch makes it 1..N.
- **Every player**: the client inventory (`ProtoActor.Inventory`) sizes itself from `gameConfig.playerActor.maxGenericInventorySlot`; the patch sets that to N before it's created. N comes from the host through the Steam lobby data (`medovanx.invslots`).
- **HUD**: `UIPrefab_Inventory` has 4 fixed slot widgets; the patch clones them up to 8 and lays them out 4 per row.
- **Saves** don't record slots (saved items are dropped in the lobby on load), so changing the number never loses items.

## Files installed
- `MIMESIS_Data/Managed/InventoryRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `Inventory.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)
