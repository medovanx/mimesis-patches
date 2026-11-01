// MIMESIS PSController - game patches
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PSController
{
    [HarmonyPatch]
    static class GamePatches
    {
        // Button prompts: a PS icon for gamepad keys, or the game's own icon when there's none (or icons are off).
        // In the current game only the old key-bind screen (UIPrefab_KeyBind) still uses this.
        [HarmonyPrefix, HarmonyPatch(typeof(KeyImageData), nameof(KeyImageData.GetKeyImage), typeof(string))]
        static bool KeyImage(string __0, ref Sprite __result)
        {
            var icon = PSIcons.Get(__0);
            if (icon == null) return true;
            __result = icon;
            return false;
        }

        // The game now draws its gamepad prompts (crosshair, HUD and spectator tips, mimic possession, Controls settings)
        // with UIPrefab_GamePadKeyIcon, built from Xbox parts, instead of KeyImageData.
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_GamePadKeyIcon), nameof(UIPrefab_GamePadKeyIcon.SetKey), typeof(string))]
        static void PadKeyIcon(UIPrefab_GamePadKeyIcon __instance, string __0) => PSIcons.Apply(__instance, __0);

        // Every Gamepad.current read prefers the DS4Windows Xbox pad (see Pads). Pads itself reads Gamepad.current,
        // so the original getter runs while it does.
        static bool _inPads;

        [HarmonyPrefix, HarmonyPatch(typeof(Gamepad), nameof(Gamepad.current), MethodType.Getter)]
        static bool CurrentPad(ref Gamepad __result)
        {
            if (_inPads) return true;
            _inPads = true;
            try { __result = Pads.Current; }
            finally { _inPads = false; }
            return false;
        }
    }
}
