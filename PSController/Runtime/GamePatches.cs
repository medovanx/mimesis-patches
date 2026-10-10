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
        [HarmonyPrefix, HarmonyPatch(typeof(KeyImageData), nameof(KeyImageData.GetKeyImage), typeof(string))]
        static bool KeyImage(string __0, ref Sprite __result)
        {
            var icon = PSIcons.Get(__0);
            if (icon == null) return true;
            __result = icon;
            return false;
        }

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
