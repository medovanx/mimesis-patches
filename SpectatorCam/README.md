# SpectatorCam (SpectatorCamPatcher)

**Version 1.1.0**

A free camera for dead players, limited to the area around the player you're spectating.

## Use
1. Close the game.
2. Put `SpectatorCamPatcher.exe` next to `MIMESIS.exe` and run it.
3. Choose install (`i`) or uninstall (`u`).

Each player installs it for themselves; it only changes your own camera.

## Controls (while spectating)
| | Keyboard / mouse | Controller |
|---|---|---|
| Free camera on/off | **F** | **R3** |
| Move | W A S D | Left stick |
| Up / down | E or Space / Q or Ctrl | RB / LB |
| Look | Mouse | Right stick |
| Faster | Shift | L3 |

Switching to another player (or the game changing your spectate target) returns to the normal orbit camera.

## Fairness limits
- The camera stays within **8 m** of the spectated player.
- It can't pass walls: it's stopped at anything between it and the player's head. You can look around the player you're watching, but not scout other rooms for monsters or loot.

## What it changes
- `SpectatorCamRuntime.dll` (Harmony) patches `CameraManager.UpdateSpectatorCameraInput`. While free mode is on it drives the spectator camera itself and the game's target-switch keys (A/D) are paused.
- `CameraManager.SetupSpectatorCamera` exits free mode before a target change.
- Limits are in `Runtime/FreeCamPatch.cs` (`MaxDistance`, speeds, sensitivity).

## Files installed
- `MIMESIS_Data/Managed/SpectatorCamRuntime.dll`, `0Harmony.dll`
- `Assembly-CSharp.dll`: one `SpectatorCam.Plugin.Init()` call at the start of `Hub.Awake` (original kept as `.bak`)

## Settings
Open **Patches → SpectatorCam** on the main menu to turn it on/off.
