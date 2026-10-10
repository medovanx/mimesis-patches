# Inventory

<p align="center"><img src="icon.png" width="160"></p>

**Version 1.3.0**

The host sets the inventory size to **1-8 slots**. More than 4 shows as a **2 × 4 grid** (slots 1-4 on the bottom row, 5-8 above).

![Inventory bar with 8 slots in a 2x4 grid](screenshots/inventory.png)

**Every player needs it.** Without it, a player keeps 4 slots and can run into problems when given items in slots 5-8, so make sure everyone installs it before raising the number.

## Install
Use the [installer](../Installer/), or install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into the game folder, run the game once, then copy `InventoryRuntime.dll` from this patch's [release](https://github.com/medovanx/mimesis-patches/releases) into `BepInEx\plugins`.

## Options

![Patches > Inventory settings](screenshots/settings.png)

**Patches > Inventory** on the main menu: pick the slot count used when you host. Players get it from the host automatically. It applies when characters spawn (next level, or reload the lobby). Changing it never loses items.

## Uninstall
Use the installer's **Uninstall selected**, or delete `InventoryRuntime.dll` from `BepInEx\plugins`.
