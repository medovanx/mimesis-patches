// MIMESIS BiggerLobby - difficulty scaling for more than 4 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BiggerLobby
{
    // The game is balanced for 4 and never looks at the player count. Above 4 players (counted on the
    // host when each value is used), these patches scale:
    //   quota          x players / 4                      (8 -> 2x, 10 -> 2.5x)
    //   shop prices    x (1 + 12.5% per player above 4)   (10 -> 1.75x)
    //   monster budget x (1 + 10% per player above 4)     (10 -> 1.6x)
    //   mimics         + 1 per 3 players above 4          (10 -> +2)
    // At 4 players or fewer nothing changes. Loot is not scaled; the higher quota accounts for it.
    [HarmonyPatch]
    static class ScalingPatch
    {
        static GameSessionInfo _session;

        /// <summary>Difficulty scaling on/off (Patches window); off = vanilla balance at any player count.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("medovanx.BiggerLobby.Scaling", 1) == 1;
            set { PlayerPrefs.SetInt("medovanx.BiggerLobby.Scaling", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static int SessionPlayers => _session?.TotalPlayerSteamIDs.Count ?? 0;
        static int Players => Enabled ? Mathf.Max(SessionPlayers, 4) : 4;
        static int Extra => Players - 4;

        public static float QuotaFactor => Players / 4f;
        public static float PriceFactor => 1f + 0.125f * Extra;
        public static float MonsterFactor => 1f + 0.10f * Extra;
        public static int ExtraMimics => Extra / 3;

        [HarmonyPostfix, HarmonyPatch(typeof(GameSessionInfo), MethodType.Constructor)]
        static void TrackSession(GameSessionInfo __instance) => _session = __instance;

        // The game stores the unscaled quota; every reader goes through GetCurrencyThreshold, so the
        // quota follows the current player count and is never scaled twice.
        [HarmonyPostfix, HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.GetCurrencyThreshold))]
        static void ScaleQuota(GameSessionInfo __instance, ref int __result)
        {
            _session = __instance;
            __result = Mathf.RoundToInt(__result * QuotaFactor);
        }

        // Saves keep the unscaled quota, so loading a save with a different group size stays correct.
        [HarmonyTranspiler, HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
        static IEnumerable<CodeInstruction> SaveUnscaledQuota(IEnumerable<CodeInstruction> code)
        {
            var setter = AccessTools.PropertySetter(typeof(ReluProtocol.MMSaveGameData), nameof(ReluProtocol.MMSaveGameData.TargetCurrency));
            foreach (var x in code)
            {
                if (x.Calls(setter)) yield return CodeInstruction.Call(typeof(ScalingPatch), nameof(Unscaled));
                yield return x;
            }
        }

        static int Unscaled(int scaled) => _session != null ? _session._targetCurrency : Mathf.RoundToInt(scaled / QuotaFactor);

        [HarmonyPostfix, HarmonyPatch(typeof(ExcelDataManager), nameof(ExcelDataManager.GetShopGroupInfo))]
        static void ScalePrice(ref (int masterid, int price, float discountRate) __result) =>
            __result.price = Mathf.RoundToInt(__result.price * PriceFactor);

        [HarmonyPostfix, HarmonyPatch(typeof(DungeonRoom), MethodType.Constructor,
            typeof(VRoomManager), typeof(long), typeof(IVRoomProperty))]
        static void ScaleMonsters(DungeonRoom __instance, VRoomManager roomManager)
        {
            _session = roomManager.GetGameSessionInfo() ?? _session;
            __instance._normalMonsterThreatLimit = Mathf.RoundToInt(__instance._normalMonsterThreatLimit * MonsterFactor);
            __instance._normalMonsterThreatRemain = __instance._normalMonsterThreatLimit;
            __instance._mimicSpawnCountMax += ExtraMimics;
            __instance._mimicSpawnCountRemain = __instance._mimicSpawnCountMax;
            Debug.Log($"[BiggerLobby] Scaling for {Players} players: quota x{QuotaFactor:0.##}, prices x{PriceFactor:0.##}, " +
                      $"monsters x{MonsterFactor:0.##}, mimics +{ExtraMimics}");
        }
    }
}
