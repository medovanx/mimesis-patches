// MIMESIS LateJoin - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace LateJoin
{
    public static class Plugin
    {
        static bool _initialized;

        // Called once at the start of Hub.Awake (see Common/MelonEntry.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                new HarmonyLib.Harmony("medovanx.latejoin").PatchAll(typeof(Plugin).Assembly);
                var go = new GameObject("MedovanxLateJoin");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Ticker>();
                MimesisPatches.PatchMenu.Register("LateJoin", version,
                    status: () => LateJoinPatch.Enabled ? "Players can join a run in progress" : "Off (game default)",
                    build: (page, font) =>
                    {
                        MimesisPatches.PatchUi.OnOff(page, font, "Allow late join", () => LateJoinPatch.Enabled, v => LateJoinPatch.Enabled = v);
                        // EA 0.4.0 lets players join mid-run itself; this only covers joiners who end up at the tram.
                        MimesisPatches.PatchUi.Label(page, font,
                            "The game lets friends join mid-run (in a level they spectate until it ends).\n" +
                            "If one reaches the tram while the team is away, they wait there instead of being sent back.\n" +
                            "You and the joining player both need this patch.", 20f, MimesisPatches.PatchUi.Dim);
                    });
                Debug.Log($"[LateJoin] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[LateJoin] Failed to apply patches: " + e);
            }
        }
    }
}
