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

        // Called once from the patched Hub.Awake.
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                new Harmony("medovanx.latejoin").PatchAll(typeof(Plugin).Assembly);
                var go = new GameObject("MedovanxLateJoin");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Ticker>();
                MimesisPatches.PatchMenu.Register("LateJoin", version,
                    status: () => LateJoinPatch.Enabled ? "Players can join a run in progress" : "Off (join in lobby only)",
                    build: (page, font) =>
                    {
                        MimesisPatches.PatchUi.OnOff(page, font, "Allow late join", () => LateJoinPatch.Enabled, v => LateJoinPatch.Enabled = v);
                        MimesisPatches.PatchUi.Label(page, font,
                            "When you host, friends can join mid-run. They wait, then join at the tram when the team returns.\n" +
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
