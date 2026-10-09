// MIMESIS SpectatorCam - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace SpectatorCam
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
                new Harmony("medovanx.spectatorcam").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchChip.Register("SpectatorCam", version);
                Debug.Log($"[SpectatorCam] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[SpectatorCam] Failed to apply patches: " + e);
            }
        }
    }
}
