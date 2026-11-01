// MIMESIS BiggerLobby - runtime UI support for up to 10 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace BiggerLobby
{
    public static class Plugin
    {
        public const int MaxPlayers = 10;

        static bool _initialized;

        // Called once at the start of Hub.Awake (see Common/MelonEntry.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                // One class at a time: PatchAll stops at the first target a game update broke and silently skips every
                // class after it. This way the rest, above all the lobby size, still applies.
                var harmony = new HarmonyLib.Harmony("medovanx.biggerlobby");
                int failed = 0;
                foreach (var type in AccessTools.GetTypesFromAssembly(typeof(Plugin).Assembly))
                {
                    try { harmony.CreateClassProcessor(type).Patch(); }
                    catch (Exception e) { failed++; Debug.LogError($"[BiggerLobby] {type.Name} not applied: {e}"); }
                }
                MimesisPatches.PatchMenu.Register("BiggerLobby", typeof(Plugin).Assembly.GetName().Version,
                    status: () => $"Lobbies hold {MaxPlayers} players, difficulty scaling {(ScalingPatch.Enabled ? "on" : "off")}",
                    build: (page, font) =>
                    {
                        MimesisPatches.PatchUi.OnOff(page, font, "Difficulty scaling", () => ScalingPatch.Enabled, v => ScalingPatch.Enabled = v);
                        MimesisPatches.PatchUi.Label(page, font,
                            "Above 4 players (host only):\n" +
                            "\u2022 Quota \u00d7 players / 4   (10 players: \u00d72.5)\n" +
                            "\u2022 Shop prices +12.5% per extra player   (\u00d71.75)\n" +
                            "\u2022 Monster budget +10% per extra player   (\u00d71.6)\n" +
                            "\u2022 +1 mimic per 3 extra players   (+2)", 20f, MimesisPatches.PatchUi.Dim);
                    });
                Debug.Log($"[BiggerLobby] v{typeof(Plugin).Assembly.GetName().Version.ToString(3)}: patches applied ({MaxPlayers} players)" +
                          (failed > 0 ? $", {failed} failed (see errors above)" : ""));
            }
            catch (Exception e)
            {
                Debug.LogError("[BiggerLobby] Failed to apply UI patches: " + e);
            }
        }
    }
}
