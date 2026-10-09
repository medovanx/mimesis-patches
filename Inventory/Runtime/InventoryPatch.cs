// MIMESIS Inventory - host-set inventory size (1-8 slots); more than 4 shows as a 2 x 4 grid
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The slot count lives in three places, so every player needs this patch:
//   host   InventoryController.Reset builds slots 1..4 (fixed loop)       -> 1..N
//   client ProtoActor.Inventory sizes its list from gameConfig            -> N
//   client UIPrefab_Inventory has 4 fixed slot widgets (InvenSlot1-4)     -> cloned up to 8, laid out 4 per row
// The host picks N (lobby menu or Patches window). It's published in the Steam lobby data, and every
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
        const string LobbyRow = "MedovanxInventorySlots";

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

        [HarmonyPrefix, HarmonyPatch(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))]
        static void ShowSlots(UIPrefab_Inventory __instance)
        {
            int count = Hub.s.gameConfig.playerActor.maxGenericInventorySlot;
            var slots = __instance.inventorySlots;
            if (slots.Count == 0) return;
            var second = slots[0].frame.transform.parent.parent.parent.Find(SecondRow);
            if (second != null) second.gameObject.SetActive(count > PerRow);
            MoveStaminaBar(slots[0].frame.transform.parent.parent as RectTransform, second as RectTransform, count > PerRow);
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
        // moves up by the distance between the two rows (and back when there's only one row).
        static RectTransform _stamina;
        static Vector2 _staminaPos;
        static bool _staminaMoved;

        static void MoveStaminaBar(RectTransform row, RectTransform second, bool twoRows)
        {
            if (_stamina == null)
            {
                var hud = UnityEngine.Object.FindAnyObjectByType<UIPrefab_InGame>();
                _stamina = hud != null && hud.staminaGauge != null ? hud.staminaGauge.transform as RectTransform : null;
                if (_stamina == null) return;
                _staminaPos = _stamina.anchoredPosition;
                _staminaMoved = false;
            }
            if (twoRows == _staminaMoved || row == null || second == null) return;
            if (twoRows)
            {
                var worldUp = second.position - row.position;   // one row height, in world units
                var local = _stamina.parent.InverseTransformVector(worldUp);
                _stamina.anchoredPosition = _staminaPos + new Vector2(0f, local.y);
            }
            else _stamina.anchoredPosition = _staminaPos;
            _staminaMoved = twoRows;
        }

        // ---------------- Host: lobby menu row ----------------

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
        static void AddLobbyRow(UIPrefab_InGameMenu __instance)
        {
            Publish();
            var publicRoom = __instance.UE_PublicRoom as RectTransform;
            var password = __instance.UE_RoomPassword as RectTransform;
            if (publicRoom == null || password == null) return;
            var row = publicRoom.parent.Find(LobbyRow) as RectTransform;
            if (row == null)
            {
                float step = publicRoom.anchoredPosition.y - password.anchoredPosition.y;
                // Below HostOptions' rows (stamina at 0.95 x step, money row under it).
                row = (RectTransform)UnityEngine.Object.Instantiate(publicRoom.gameObject, publicRoom.parent, false).transform;
                row.name = LobbyRow;
                row.anchoredPosition = password.anchoredPosition - new Vector2(0f, step * 1.45f);
                var toggle = row.GetComponentInChildren<Toggle>(true);
                if (toggle != null) UnityEngine.Object.Destroy(toggle.gameObject);
                if (row.Find("title") != null) UnityEngine.Object.Destroy(row.Find("title").gameObject);
                var line = row.Find("RoomName");
                var label = line != null ? line.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t.GetComponentInParent<TMP_InputField>() == null && t.GetComponentInParent<Button>() == null) : null;
                if (label != null) label.text = "INVENTORY SLOTS (1-8):";
                foreach (var b in row.GetComponentsInChildren<Button>(true)) b.gameObject.SetActive(false);
                var input = row.GetComponentInChildren<TMP_InputField>(true);
                input.onValueChanged.RemoveAllListeners();
                input.onEndEdit.RemoveAllListeners();
                input.onSubmit.RemoveAllListeners();
                input.contentType = TMP_InputField.ContentType.IntegerNumber;
                input.characterLimit = 1;
                input.onEndEdit.AddListener(text =>
                {
                    HostSlots = int.TryParse(text, out var v) ? v : Default;
                    input.SetTextWithoutNotify(HostSlots.ToString());
                    Debug.Log($"[Inventory] Host set {HostSlots} slots (applies when characters spawn: next level or lobby reload)");
                });
            }
            row.gameObject.SetActive(IsHost);
            row.GetComponentInChildren<TMP_InputField>(true)?.SetTextWithoutNotify(HostSlots.ToString());
        }
    }
}
