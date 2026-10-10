// MIMESIS HostOptions - difficulty and economy multipliers
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// All four values are used only on the host, so only the host needs the patch:
//   quota       GameSessionInfo.GetCurrencyThreshold (also the tram repair cost)
//   shop prices price list sent to players + MaintenanceRoom.TryGetShopItemPrice
//   sell value  MaintenanceRoom.PutIntoToilet (the extra money is added after the game's own payout)
//   monsters    DungeonRoom constructor: normal monster threat budget and mimic count
// They stack with BiggerLobby's player-count scaling.

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ReluNetwork.ConstEnum;
using ReluProtocol;
using ReluProtocol.Enum;
using UnityEngine;

namespace HostOptions
{
    [HarmonyPatch]
    static class Difficulty
    {
        public const int Min = 25, Max = 300;

        static int Get(string key) => Mathf.Clamp(PlayerPrefs.GetInt("medovanx.HostOptions." + key, 100), Min, Max);
        static void Set(string key, int v) { PlayerPrefs.SetInt("medovanx.HostOptions." + key, Mathf.Clamp(v, Min, Max)); PlayerPrefs.Save(); }

        public static int Quota { get => Get("Quota"); set => Set("Quota", value); }
        public static int Prices { get => Get("Prices"); set => Set("Prices", value); }
        public static int SellValue { get => Get("Sell"); set => Set("Sell", value); }
        public static int Monsters { get => Get("Monsters"); set => Set("Monsters", value); }

        public static readonly (string name, int quota, int prices, int sell, int monsters)[] Presets =
        {
            ("Easy", 75, 75, 125, 60),
            ("Normal", 100, 100, 100, 100),
            ("Hard", 125, 125, 90, 150),
        };

        public static void Apply((string name, int quota, int prices, int sell, int monsters) p)
        {
            Quota = p.quota; Prices = p.prices; SellValue = p.sell; Monsters = p.monsters;
        }

        public static string Current
        {
            get
            {
                foreach (var p in Presets)
                    if (Quota == p.quota && Prices == p.prices && SellValue == p.sell && Monsters == p.monsters) return p.name;
                return "Custom";
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.GetCurrencyThreshold))]
        static void ScaleQuota(ref int __result) => __result = Mathf.Max(1, Mathf.RoundToInt(__result * Quota / 100f));

        // Shop prices: the room keeps the game's own prices (and saves them), and the multiplier is applied
        // wherever a price leaves the room: the price list sent to players and the price charged. So it also
        // works on loaded saves, and a change takes effect the next time players enter the lobby.
        static int Price(int vanilla) => Mathf.Max(1, Mathf.RoundToInt(vanilla * Prices / 100f));

        static Dictionary<int, ShopItemPriceInfo> PriceList(Dictionary<int, ShopItemPriceInfo> vanilla)
        {
            if (vanilla == null || Prices == 100) return vanilla;
            return vanilla.ToDictionary(x => x.Key, x => x.Value == null ? null : new ShopItemPriceInfo { Price = Price(x.Value.Price), DiscountRate = x.Value.DiscountRate });
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnEnterChannel))]
        [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnCompleteGame))]
        static IEnumerable<CodeInstruction> SendScaledPrices(IEnumerable<CodeInstruction> code)
        {
            var field = AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems");
            foreach (var x in code)
            {
                yield return x;
                if (x.LoadsField(field)) yield return CodeInstruction.Call(typeof(Difficulty), nameof(PriceList));
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.TryGetShopItemPrice))]
        static void ChargeScaledPrice(bool __result, ref int price)
        {
            if (__result) price = Price(price);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.PutIntoToilet))]
        static void ScaleSell(MaintenanceRoom __instance, ref (MsgErrorCode errorCode, int currency, int price) __result)
        {
            if (__result.errorCode != MsgErrorCode.Success || SellValue == 100) return;
            int extra = Mathf.RoundToInt(__result.price * (SellValue - 100) / 100f);
            __instance.Currency = Mathf.Max(0, __instance.Currency + extra);
            __result.price += extra;
            __result.currency = __instance.Currency;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DungeonRoom), MethodType.Constructor, typeof(VRoomManager), typeof(long), typeof(IVRoomProperty))]
        static void ScaleMonsters(DungeonRoom __instance)
        {
            if (Monsters == 100) return;
            float f = Monsters / 100f;
            __instance._normalMonsterThreatLimit = Mathf.RoundToInt(__instance._normalMonsterThreatLimit * f);
            __instance._normalMonsterThreatRemain = __instance._normalMonsterThreatLimit;
            __instance._mimicSpawnCountMax = Mathf.RoundToInt(__instance._mimicSpawnCountMax * f);
            __instance._mimicSpawnCountRemain = __instance._mimicSpawnCountMax;
            Debug.Log($"[HostOptions] Monsters x{f:0.##}: threat {__instance._normalMonsterThreatLimit}, mimics {__instance._mimicSpawnCountMax}");
        }
    }
}
