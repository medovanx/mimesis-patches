# Fov (FovPatcher)

**Version 1.3.0**

Adds a **field of view** setting (60°-110°, or the game default) for your first-person view.

## Use
1. Close the game.
2. Put `FovPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

Set it with the slider in **Patches → Fov** on the main menu (50°-120°, or reset to the game default), or in game tap **[** / **]** for 5° steps or hold them to change it smoothly (50°-120°, with a short on-screen message). It applies right away and is saved. Only you need it (client-side).

## How it works
The game has no FOV option: the player camera (`CameraManager.playerCamera`, Cinemachine) keeps its prefab value. The patch keeps that camera at your chosen FOV every frame, except while the game's own "zoom to face" effect is running.

## Files installed
- `MIMESIS_Data/Managed/FovRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `Fov.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)
