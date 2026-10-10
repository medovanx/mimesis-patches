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

        // Called once at the start of Hub.Awake (see Common/BepInExPlugin.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                new HarmonyLib.Harmony("medovanx.hostoptions").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchMenu.Register("HostOptions", version, build: (page, font) =>
                {
                    MimesisPatches.PatchUi.Section(page, font, "Lobby");
                    MimesisPatches.PatchUi.OnOff(page, font, "Infinite stamina", () => HostOptionsPatch.InfiniteStamina, v => HostOptionsPatch.InfiniteStamina = v);
                    MimesisPatches.PatchUi.Number(page, font, "Starting money",
                        () => HostOptionsPatch.StartMoney >= 0 ? HostOptionsPatch.StartMoney : HostOptionsPatch.DefaultMoney,
                        v => { HostOptionsPatch.StartMoney = v == HostOptionsPatch.DefaultMoney ? -1 : v; HostOptionsPatch.ApplyNow(v); },
                        HostOptionsPatch.DefaultMoney);
                    MimesisPatches.PatchUi.Section(page, font, "Difficulty and economy");
                    // "Custom" lights up when the sliders don't match a preset; clicking it does nothing.
                    MimesisPatches.PatchUi.Options(page, font, "Difficulty", 122f,
                        Difficulty.Presets.Select(p => (p.name, (Func<bool>)(() => Difficulty.Current == p.name), (Action)(() => Difficulty.Apply(p))))
                            .Append(("Custom", () => Difficulty.Current == "Custom", () => { })).ToArray());
                    MimesisPatches.PatchUi.Slider(page, font, "Quota / repair", Difficulty.Min, Difficulty.Max, () => Difficulty.Quota, v => Difficulty.Quota = v, "%", "Money needed to repair the tram.");
                    MimesisPatches.PatchUi.Slider(page, font, "Shop prices", Difficulty.Min, Difficulty.Max, () => Difficulty.Prices, v => Difficulty.Prices = v, "%", "Cost of items in the shop.");
                    MimesisPatches.PatchUi.Slider(page, font, "Sell value", Difficulty.Min, Difficulty.Max, () => Difficulty.SellValue, v => Difficulty.SellValue = v, "%", "Money you get for each item you sell.");
                    MimesisPatches.PatchUi.Slider(page, font, "Monsters", Difficulty.Min, Difficulty.Max, () => Difficulty.Monsters, v => Difficulty.Monsters = v, "%", "How many monsters and mimics spawn in a level.");
                    MimesisPatches.PatchUi.Options(page, font, "Apply to", 200f,
                        ("All games", () => Difficulty.AffectSaves, () => Difficulty.AffectSaves = true),
                        ("New games only", () => !Difficulty.AffectSaves, () => Difficulty.AffectSaves = false));
                    MimesisPatches.PatchUi.Label(page, font,
                        "Host only. Stamina and money are also in the lobby menu (Esc). Starting money applies to new runs, and to current funds before the first departure.",
                        16f, MimesisPatches.PatchUi.Dim);
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
