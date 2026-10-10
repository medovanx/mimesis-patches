// MIMESIS Revive - revive a dead teammate by standing over their body
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The game already has a server-side revive (VPlayer.Revive, used to bring the host back at the tram); it
// resets the player, moves them, and tells every client (PlayerReviveSig), which brings them out of
// spectating. This patch calls it during a level: when a living player stays within Range of a dead
// teammate's body for HoldSeconds, the host revives that teammate on the spot.
// Everything runs on the host, so only the host needs this patch.

using System;
using System.Collections.Generic;
using HarmonyLib;
using MimesisPatches;
using ReluProtocol.Enum;
using UnityEngine;

namespace Revive
{
    public static class Plugin
    {
        static bool _initialized;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("medovanx.Revive.Enabled", 1) == 1;
            set { PlayerPrefs.SetInt("medovanx.Revive.Enabled", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Seconds a teammate must stay next to the body.</summary>
        public static int HoldSeconds
        {
            get => PlayerPrefs.GetInt("medovanx.Revive.Hold", 5);
            set { PlayerPrefs.SetInt("medovanx.Revive.Hold", value); PlayerPrefs.Save(); }
        }

        /// <summary>Revives allowed per player per level (0 = unlimited).</summary>
        public static int PerLevel
        {
            get => PlayerPrefs.GetInt("medovanx.Revive.PerLevel", 1);
            set { PlayerPrefs.SetInt("medovanx.Revive.PerLevel", value); PlayerPrefs.Save(); }
        }

        // Called once from the patched Hub.Awake.
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                new Harmony("medovanx.revive").PatchAll(typeof(Plugin).Assembly);
                PatchMenu.Register("Revive", version,
                    status: () => Enabled ? $"Stand over a dead teammate for {HoldSeconds}s to revive them" : "Off",
                    build: (page, font) =>
                    {
                        PatchUi.OnOff(page, font, "Revive", () => Enabled, v => Enabled = v);
                        PatchUi.Options(page, font, "Hold time",
                            ("3s", () => HoldSeconds == 3, () => HoldSeconds = 3),
                            ("5s", () => HoldSeconds == 5, () => HoldSeconds = 5),
                            ("10s", () => HoldSeconds == 10, () => HoldSeconds = 10));
                        PatchUi.Options(page, font, "Per player",
                            ("Once per level", () => PerLevel == 1, () => PerLevel = 1),
                            ("Unlimited", () => PerLevel == 0, () => PerLevel = 0));
                        PatchUi.Label(page, font,
                            "Stand next to a dead teammate's body to bring them back with low health.\n" +
                            "Only the host needs this patch.", 20f, PatchUi.Dim);
                    });
                Debug.Log($"[Revive] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[Revive] Failed to apply patches: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), nameof(DungeonRoom.OnUpdate))]
    static class ReviveTick
    {
        const float Range = 1.8f;

        // Per dead player: milliseconds a living teammate has stood next to them.
        static readonly Dictionary<long, long> Progress = new Dictionary<long, long>();
        // Per player: revives used in the current level (keyed by room so a new level resets it).
        static readonly Dictionary<(long room, long player), int> Used = new Dictionary<(long, long), int>();

        static void Postfix(DungeonRoom __instance, long delta)
        {
            if (!Plugin.Enabled) { Progress.Clear(); return; }
            var players = __instance._vPlayerDict.Values;
            foreach (var dead in players)
            {
                if (dead.LifeCycle != VCreatureLifeCycle.Dead) { Progress.Remove(dead.UID); continue; }
                Used.TryGetValue((__instance.RoomID, dead.UID), out var used);
                if (Plugin.PerLevel > 0 && used >= Plugin.PerLevel) continue;

                bool helper = false;
                foreach (var p in players)
                    if (p != dead && p.LifeCycle == VCreatureLifeCycle.Alive
                        && (p.PositionVector - dead.PositionVector).sqrMagnitude <= Range * Range)
                    { helper = true; break; }

                if (!helper) { Progress.Remove(dead.UID); continue; }
                Progress.TryGetValue(dead.UID, out var held);
                held += delta;
                if (held < Plugin.HoldSeconds * 1000L) { Progress[dead.UID] = held; continue; }

                Progress.Remove(dead.UID);
                if (dead.Revive(dead.Position))
                {
                    Used[(__instance.RoomID, dead.UID)] = used + 1;
                    Debug.Log($"[Revive] Revived player {dead.UID} in room {__instance.RoomID}");
                }
            }
        }
    }
}
