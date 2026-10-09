// MIMESIS Inventory - host-set inventory size (1-8 slots); more than 4 shows as a 2 x 4 grid
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The slot count lives in three places, so every player needs this patch:
//   host   InventoryController.Reset builds slots 1..4 (fixed loop)       -> 1..N
//   client ProtoActor.Inventory sizes its list from gameConfig            -> N
//   client UIPrefab_Inventory has 4 fixed slot widgets (InvenSlot1-4)     -> cloned up to 8, laid out 4 per row
// The host picks N in the Patches window (main menu). It's published in the Steam lobby data, and every
// patched player reads it from there before their character's inventory is created.
// Saves don't record slots (saved items are dropped in the lobby on load), so changing N never loses items.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Mimic.Actors;
using ReluNetwork.ConstEnum;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                if (IsHost || Lobby == CSteamID.Nil) return HostSlots;
                try { return int.TryParse(SteamMatchmaking.GetLobbyData(Lobby, LobbyKey), out var n) ? Mathf.Clamp(n, Min, Max) : Default; }
                catch { return Default; }
            }
        }

        public static void Publish()
        {
            if (!IsHost || Lobby == CSteamID.Nil) return;
            try { SteamMatchmaking.SetLobbyData(Lobby, LobbyKey, HostSlots.ToString()); } catch { }
        }

        // ---------------- Host: server-side slots ----------------

        [HarmonyTranspiler, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.Reset))]
        static IEnumerable<CodeInstruction> ServerSlots(IEnumerable<CodeInstruction> code)
        {
            foreach (var x in code)
                yield return x.opcode == OpCodes.Ldc_I4_4
                    ? new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(InventoryPatch), nameof(HostSlots))).MoveLabelsFrom(x)
                    : x;
        }

        // ---------------- Every player: client-side slots ----------------

        // The client inventory sizes itself from the game config; set it to this session's count first.
        [HarmonyPrefix, HarmonyPatch(typeof(ProtoActor.Inventory), MethodType.Constructor, typeof(ProtoActor))]
        static void ClientSlots()
        {
            Publish();
            if (Hub.s?.gameConfig?.playerActor != null) Hub.s.gameConfig.playerActor.maxGenericInventorySlot = Slots;
        }

        // ---------------- Every player: inventory HUD ----------------

        // The HUD row (inventoryFrame) holds InvenSlot1-4 plus a separate StackCount group with stackCount1-4 at
        // fixed positions. Rather than re-laying it out, slots 5-8 are a copy of the whole row placed just above.
        const string SecondRow = "MedovanxInventoryRow2";
        const float RowGap = 12f;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_Inventory), "Awake")]
        static void AddSlots(UIPrefab_Inventory __instance)
        {
            var slots = __instance.inventorySlots;
            if (slots.Count != PerRow) return;
            var row = (RectTransform)slots[0].frame.transform.parent.parent;   // inventoryFrame
            var copy = (RectTransform)UnityEngine.Object.Instantiate(row.gameObject, row.parent, false).transform;
            copy.name = SecondRow;
            foreach (var anim in copy.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.Destroy(anim);   // keep it still
            copy.anchoredPosition = row.anchoredPosition + new Vector2(0f, row.rect.height * row.localScale.y + RowGap);

            for (int i = 1; i <= PerRow; i++)
            {
                T Part<T>(string name) where T : Component =>
                    copy.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name + i);
                slots.Add(new UIPrefab_Inventory.Slot
                {
                    frame = Part<Image>("InvenFrame"),
                    image = Part<Image>("InvenImage"),
                    stackCount = Part<TMP_Text>("stackCount"),
                    waitEvent = Part<Transform>("InvenWaitEvent"),
                });
            }
            copy.gameObject.SetActive(false);
        }

        // Stack counts sit at fixed positions, but the slot row re-centres when slots are hidden; remember each
        // label's horizontal offset from its slot so it can follow the slot.
        static readonly Dictionary<TMP_Text, float> LabelOffset = new Dictionary<TMP_Text, float>();

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))]
        static void AlignLabels(UIPrefab_Inventory __instance)
        {
            var slots = __instance.inventorySlots;
            if (slots.Count == 0) return;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)slots[0].frame.transform.parent.parent);
            foreach (var slot in slots)
            {
                if (slot.stackCount == null || slot.frame == null) continue;
                var label = slot.stackCount.transform;
                var slotT = slot.frame.transform.parent;
                if (!LabelOffset.ContainsKey(slot.stackCount)) continue;
                label.position = new Vector3(slotT.position.x + LabelOffset[slot.stackCount], label.position.y, label.position.z);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))]
        static void ShowSlots(UIPrefab_Inventory __instance)
        {
            int count = Hub.s.gameConfig.playerActor.maxGenericInventorySlot;
            var slots = __instance.inventorySlots;
            if (slots.Count == 0) return;
            var second = slots[0].frame.transform.parent.parent.parent.Find(SecondRow);
            if (second != null) second.gameObject.SetActive(count > PerRow);
            MoveStaminaBar(slots[0].frame.transform.parent.parent as RectTransform, second as RectTransform, count > PerRow);
            // First time, with all slots in their original places: record label offsets.
            foreach (var slot in slots)
                if (slot.stackCount != null && slot.frame != null && !LabelOffset.ContainsKey(slot.stackCount))
                    LabelOffset[slot.stackCount] = slot.stackCount.transform.position.x - slot.frame.transform.parent.position.x;

            for (int i = 0; i < slots.Count; i++)
            {
                // A slot's stack count isn't under its InvenSlot object, so toggle both.
                bool on = i < count;
                var slot = slots[i];
                if (slot.frame != null) slot.frame.transform.parent.gameObject.SetActive(on);
                if (slot.stackCount != null) slot.stackCount.gameObject.SetActive(on);
                if (!on && slot.waitEvent != null) slot.waitEvent.gameObject.SetActive(false);
            }
        }

        // The stamina bar sits just above the slot row; with a second row it would end up between the rows, so it
        // moves up by the distance between the two rows. Its Animator rewrites its position, so the offset is
        // re-applied every frame after animation (StaminaOffset.LateUpdate).
        static void MoveStaminaBar(RectTransform row, RectTransform second, bool twoRows)
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<UIPrefab_InGame>();
            var bar = hud != null && hud.staminaGauge != null ? hud.staminaGauge.transform as RectTransform : null;
            if (bar == null || row == null || second == null) return;
            var mover = bar.GetComponent<StaminaOffset>() ?? bar.gameObject.AddComponent<StaminaOffset>();
            mover.Offset = twoRows ? (Vector2)bar.parent.InverseTransformVector(second.position - row.position) : Vector2.zero;
        }

        // Publish the host's number whenever the lobby/pause menu opens (the setting itself is in the Patches window).
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
        static void PublishOnMenu() => Publish();

    }

    // Keeps the stamina bar shifted by Offset, on top of whatever position its Animator (or layout) gives it.
    sealed class StaminaOffset : MonoBehaviour
    {
        public Vector2 Offset;
        Vector2 _applied, _base;
        bool _has;

        void LateUpdate()
        {
            var rt = (RectTransform)transform;
            // If something else moved it since our last write, that's the new base position.
            if (!_has || rt.anchoredPosition != _base + _applied) { _base = rt.anchoredPosition; _has = true; }
            if (rt.anchoredPosition == _base + _applied && _applied == Offset) return;
            rt.anchoredPosition = _base + Offset;
            _applied = Offset;
        }
    }
}
