// MIMESIS HostOptions - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace HostOptions
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
                new Harmony("medovanx.hostoptions").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchChip.Register("HostOptions", version, status: () =>
                    (HostOptionsPatch.InfiniteStamina ? "Infinite stamina" : "Normal stamina") +
                    (HostOptionsPatch.StartMoney >= 0 ? " \u00b7 $" + HostOptionsPatch.StartMoney : ""));
                Debug.Log($"[HostOptions] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[HostOptions] Failed to apply patches: " + e);
            }
        }
    }
}
