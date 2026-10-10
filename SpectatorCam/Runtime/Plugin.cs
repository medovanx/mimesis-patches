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

        // Called once at the start of Hub.Awake (see Common/BepInExPlugin.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                new Harmony("medovanx.spectatorcam").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchMenu.Register("SpectatorCam", version,
                    status: () => FreeCamPatch.Enabled ? "Free camera: F / R3 while spectating" : "Off",
                    build: (page, font) =>
                    {
                        MimesisPatches.PatchUi.OnOff(page, font, "Free camera", () => FreeCamPatch.Enabled, v => FreeCamPatch.Enabled = v);
                        MimesisPatches.PatchUi.Label(page, font,
                            "While spectating: F / R3 toggles. Move WASD / left stick, up E or Space / RB, down Q or Ctrl / LB, look mouse / right stick, faster Shift / L3.\n" +
                            "Stays within 8 m of the spectated player and never passes walls.", 20f, MimesisPatches.PatchUi.Dim);
                    });
                Debug.Log($"[SpectatorCam] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[SpectatorCam] Failed to apply patches: " + e);
            }
        }
    }
}
