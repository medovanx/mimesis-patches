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
                MimesisPatches.PatchMenu.Register("HostOptions", version, build: (page, font) =>
                {
                    MimesisPatches.PatchUi.OnOff(page, font, "Infinite stamina", () => HostOptionsPatch.InfiniteStamina, v => HostOptionsPatch.InfiniteStamina = v);
                    MimesisPatches.PatchUi.Number(page, font, "Starting money",
                        () => HostOptionsPatch.StartMoney >= 0 ? HostOptionsPatch.StartMoney : HostOptionsPatch.DefaultMoney,
                        v => { HostOptionsPatch.StartMoney = v == HostOptionsPatch.DefaultMoney ? -1 : v; HostOptionsPatch.ApplyNow(v); },
                        HostOptionsPatch.DefaultMoney);
                    MimesisPatches.PatchUi.Label(page, font,
                        "Host only; also in the lobby menu (Esc). Starting money applies to new runs, and sets the current funds before the first departure.",
                        20f, MimesisPatches.PatchUi.Dim);
                }, status: () =>
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
