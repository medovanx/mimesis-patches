// MIMESIS HudPercent - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace HudPercent
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
                new Harmony("medovanx.hudpercent").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchMenu.Register("HudPercent", version,
                    status: () => HudPercentPatch.Enabled ? "Health and radiation % next to the HUD bars" : "Off",
                    build: (page, font) =>
                        MimesisPatches.PatchUi.OnOff(page, font, "Percentages", () => HudPercentPatch.Enabled, v => HudPercentPatch.Enabled = v));
                Debug.Log($"[HudPercent] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[HudPercent] Failed to apply patches: " + e);
            }
        }
    }
}
