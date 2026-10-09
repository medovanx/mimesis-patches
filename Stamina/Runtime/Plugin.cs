// MIMESIS Stamina - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace Stamina
{
    public static class Plugin
    {
        static bool _initialized;

        // Called once from the patched Hub.Awake.
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                new Harmony("medovanx.stamina").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchChip.Register("Stamina", version, status: () => StaminaPatch.Infinite ? "Infinite" : "Normal");
                Debug.Log($"[Stamina] v{version.ToString(3)}: patches applied (infinite stamina: {StaminaPatch.Infinite})");
            }
            catch (Exception e)
            {
                Debug.LogError("[Stamina] Failed to apply patches: " + e);
            }
        }
    }
}
