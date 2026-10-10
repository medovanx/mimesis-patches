# Fov

**Version 1.4.0**

Adds a **field of view** setting for your first-person view.

Pressing [ or ] in game:

<p>
  <img src="screenshots/toast-50.png" alt="FOV at 50" width="49%">
  <img src="screenshots/toast-120.png" alt="FOV at 120" width="49%">
</p>

**Only you need it.**

## Install
Use the [installer](../Installer/), or install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into the game folder, run the game once, then copy `FovRuntime.dll` from this patch's [release](https://github.com/medovanx/mimesis-patches/releases) into `BepInEx\plugins`.

## Use

![Patches > Fov settings](screenshots/settings.png)

- **Patches > Fov** on the main menu: slider (50°-120°) or **Reset to default**.
- In game: tap **[** / **]** for 5° steps, or hold them to change it smoothly.

Changes apply right away and are saved.

## Uninstall
Use the installer's **Uninstall selected**, or delete `FovRuntime.dll` from `BepInEx\plugins`.
