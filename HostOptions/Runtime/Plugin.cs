// MIMESIS HostOptions - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using System.Linq;
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
                    MimesisPatches.PatchUi.Options(page, font, "Difficulty",
                        Difficulty.Presets.Select(p => (p.name, (Func<bool>)(() => Difficulty.Current == p.name), (Action)(() => Difficulty.Apply(p)))).ToArray());
                    MimesisPatches.PatchUi.Slider(page, font, "Quota / repair", Difficulty.Min, Difficulty.Max, () => Difficulty.Quota, v => Difficulty.Quota = v, "%");
                    MimesisPatches.PatchUi.Slider(page, font, "Shop prices", Difficulty.Min, Difficulty.Max, () => Difficulty.Prices, v => Difficulty.Prices = v, "%");
                    MimesisPatches.PatchUi.Slider(page, font, "Sell value", Difficulty.Min, Difficulty.Max, () => Difficulty.SellValue, v => Difficulty.SellValue = v, "%");
                    MimesisPatches.PatchUi.Slider(page, font, "Monsters", Difficulty.Min, Difficulty.Max, () => Difficulty.Monsters, v => Difficulty.Monsters = v, "%");
                    MimesisPatches.PatchUi.Label(page, font,
                        "Host only. Also in the lobby menu (Esc). Starting money applies to new runs, and to current funds before the first departure.",
                        20f, MimesisPatches.PatchUi.Dim);
                }, status: () =>
                    (HostOptionsPatch.InfiniteStamina ? "Infinite stamina" : "Normal stamina") +
                    (HostOptionsPatch.StartMoney >= 0 ? ", $" + HostOptionsPatch.StartMoney : "") +
                    ", " + Difficulty.Current);
                Debug.Log($"[HostOptions] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[HostOptions] Failed to apply patches: " + e);
            }
        }
    }
}
