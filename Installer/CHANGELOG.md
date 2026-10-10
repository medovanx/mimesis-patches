# Installer changelog

## 3.0.0
- Finds the game automatically (next to the exe, the game's own log, or Steam libraries); runs from anywhere.
- Windows installer window: tick patches, install/update, uninstall selected or all, progress bar and log, game-folder picker.
- Removing a single patch is safe (restores the original and reinstalls the rest); MIMESIS is closed automatically.
- Lists Fov and Revive.
- About 52 MB (Windows Forms can't be trimmed).

## 2.2.0
- Checklist: [Delete]/[Backspace] uninstalls just the selected patch, without touching the others.
- The checklist redraws in place instead of clearing the screen on every key press, so it no longer flickers.
- Close button in the Patches window is drawn as a true ✕ (two crossed bars) instead of the letter "X".

## 2.1.1
- 3x smaller (12 MB instead of 38 MB) and a 30-minute download timeout, so it works on slow connections.

## 2.1.0
- Lists the LateJoin patch.

## 2.0.0
- Contains no patches: downloads each checked patch's files from its latest GitHub release.
- Checklist shows each patch's latest released version.

## 1.2.0
- Downloads and opens the newest installer from GitHub on launch, so a shared copy always installs the latest patches.
- Key hints in brackets.

## 1.1.0
- Checklist to pick which patches to install (arrow keys + Space).

## 1.0.0
- One exe that installs/updates or removes all patches.
