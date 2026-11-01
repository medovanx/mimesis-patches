// MIMESIS Inventory - host-set inventory size (1-8 slots); slots 5-8 use the HUD's bag row
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The game now sizes the inventory itself: the host's InventoryController owns the slots and sends their count to
// every player (UpdateInvenSig / PlayerInfo.maxInventorySlotCount), and the HUD has 10 slot widgets. A player has
// basic slots 1-4, a cosmetic slot (5), an equipment slot (6) and extension slots from 7 that bags add (HUD: 7-10).
//   more than 4   host  N-4 extension slots on top of the base count, as if carrying a bag (bags still add more, up to the HUD's 4)
//   fewer than 4  host  basic slots N+1..4 stay empty (no item goes in)
//                 every player  those slots are hidden and skipped when scrolling (needs N, so every player needs this patch)
// The host picks N in the Patches window (main menu). It's published in the Steam lobby data, and every
// patched player reads it from there.
// Saves don't record slots (saved items are dropped in the lobby on load), so changing N never loses items.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mimic;
using Mimic.Actors;
using ReluNetwork.ConstEnum;
using Steamworks;
using UnityEngine;

namespace Inventory
{
    [HarmonyPatch]
    static class InventoryPatch
    {
        public const int Min = 1, Max = 8, PerRow = 4, Default = 4;
        const string PrefKey = "medovanx.Inventory.Slots";
        const string LobbyKey = "medovanx.invslots";

        /// <summary>The host's setting (used when you host).</summary>
        public static int HostSlots
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, Default), Min, Max);
            set { PlayerPrefs.SetInt(PrefKey, Mathf.Clamp(value, Min, Max)); PlayerPrefs.Save(); Publish(); }
        }

        static bool IsHost => Hub.s != null && Hub.s.pdata != null && Hub.s.pdata.ClientMode == NetworkClientMode.Host;
        static CSteamID Lobby => Hub.s != null && Hub.s.steamInviteDispatcher != null ? Hub.s.steamInviteDispatcher.joinedLobbyID : CSteamID.Nil;

        /// <summary>Slot count for this session: the host's setting, or what the host published in the lobby.</summary>
        public static int Slots
        {
            get
            {
                if (IsHost) return HostSlots;
                if (Lobby == CSteamID.Nil) return Default;   // joined without a Steam lobby (direct connect): N unknown, hide nothing
                try { return int.TryParse(SteamMatchmaking.GetLobbyData(Lobby, LobbyKey), out var n) ? Mathf.Clamp(n, Min, Max) : Default; }
                catch { return Default; }
            }
        }

        public static void Publish()
        {
            if (!IsHost || Lobby == CSteamID.Nil) return;
            try { SteamMatchmaking.SetLobbyData(Lobby, LobbyKey, HostSlots.ToString()); } catch { }
        }

        // Basic slots are keys 1-4 in the game (InventoryController.GetSlotKind); the HUD's bag row (BagGroup) has
        // widgets for extension slots 7-10 only, so more extension slots than that would be invisible.
        const int Basic = 4, BagSlots = 4;

        /// <summary>Extension slots the host adds (0 for 4 or fewer).</summary>
        static int Extra => Mathf.Clamp(HostSlots - Basic, 0, BagSlots);

        // ---------------- Host: server-side slots ----------------

        // Every place that builds the slot list uses "4 + GetDedicatedSlotCount()" (+ bag bonus) as the base count;
        // add the extra slots right after that call. GetDedicatedSlotCount itself is too small to patch (Mono inlines it).
        static readonly MethodInfo Dedicated = AccessTools.Method(typeof(InventoryController), nameof(InventoryController.GetDedicatedSlotCount));

        static int WithExtra(int dedicated) => dedicated + Extra;

        static IEnumerable<CodeInstruction> AddExtra(IEnumerable<CodeInstruction> code, string method)
        {
            bool found = false;
            foreach (var x in code)
            {
                yield return x;
                if (!x.Calls(Dedicated)) continue;
                found = true;
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InventoryPatch), nameof(WithExtra)));
            }
            if (!found) Debug.LogWarning($"[Inventory] {method}: GetDedicatedSlotCount call not found, extra slots not applied there");
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.Reset))]
        static IEnumerable<CodeInstruction> ResetSlots(IEnumerable<CodeInstruction> code) => AddExtra(code, nameof(InventoryController.Reset));

        [HarmonyTranspiler, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.RecalculateSlotCount))]
        static IEnumerable<CodeInstruction> RecalculateSlots(IEnumerable<CodeInstruction> code) => AddExtra(code, nameof(InventoryController.RecalculateSlotCount));

        [HarmonyTranspiler, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.BeginRestoreInventory))]
        static IEnumerable<CodeInstruction> RestoreSlots(IEnumerable<CodeInstruction> code) => AddExtra(code, nameof(InventoryController.BeginRestoreInventory));

        // Mimics copy a player's inventory slot for slot, so they need the same count.
        [HarmonyTranspiler, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.CloneInven))]
        static IEnumerable<CodeInstruction> CloneSlots(IEnumerable<CodeInstruction> code) => AddExtra(code, nameof(InventoryController.CloneInven));

        // Bags add extension slots on top of the extra ones; keep the total within the HUD's bag row.
        [HarmonyPostfix, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.CalcBagSlotBonus))]
        static void BagBonus(ref int __result)
        {
            int extra = Extra;
            if (extra > 0) __result = Math.Min(__result, BagSlots - extra);
        }

        // Fewer than 4: slot keys must stay 1..count (clients index them that way), so the unused basic slots are
        // kept but never get an item. Players only; mimics copy whatever the player has.
        [HarmonyPrefix, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.IsSlotAvailableFor))]
        static bool LockedSlots(InventoryController __instance, int slotKey, ref bool __result)
        {
            if (slotKey > Basic || slotKey <= HostSlots || !(__instance._self is VPlayer)) return true;
            __result = false;
            return false;
        }

        // ---------------- Every player: inventory HUD ----------------

        // The HUD and the scroll wheel only show/select slots this says are visible; hide the locked basic slots.
        [HarmonyPostfix, HarmonyPatch(typeof(InvenSlotLayout), nameof(InvenSlotLayout.IsVisible))]
        static void HideLocked(int slotIndex, ref bool __result)
        {
            if (__result && slotIndex < Basic && slotIndex >= Slots) __result = false;
        }

        // Publish the host's number when characters spawn and whenever the pause menu opens
        // (the setting itself is in the Patches window). EA 0.4.0's pause menu is UIPrefab_InGameMenu_new.
        [HarmonyPrefix, HarmonyPatch(typeof(ProtoActor.Inventory), MethodType.Constructor, typeof(ProtoActor))]
        static void PublishOnSpawn() => Publish();

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_InGameMenu_new), "OnEnable")]
        static void PublishOnMenu() => Publish();

    }
}
