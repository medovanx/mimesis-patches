// MIMESIS Stamina - host-controlled infinite stamina
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Stamina is simulated on the host: the server-side movement code drains it through
// StatController.ConsumeStamina and clients only display the synced value. So when the host turns
// "Infinite Stamina" on, no player's stamina drains, and other players don't need this patch.
// The checkbox sits in the host's lobby menu (under "Use Entry Password"); other players never see it.

using HarmonyLib;
using ReluNetwork.ConstEnum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stamina
{
    [HarmonyPatch]
    static class StaminaPatch
    {
        const string Key = "medovanx.Stamina.Infinite";
        const string RowName = "MedovanxInfiniteStamina";

        public static bool Infinite
        {
            get => PlayerPrefs.GetInt(Key, 0) == 1;
            set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static bool IsHost => Hub.s != null && Hub.s.pdata != null && Hub.s.pdata.ClientMode == NetworkClientMode.Host;

        // Server side: players don't spend stamina while the host has it enabled. Monsters are unaffected.
        [HarmonyPrefix, HarmonyPatch(typeof(StatController), nameof(StatController.ConsumeStamina))]
        static bool Consume(StatController __instance) => !(Infinite && IsHost && __instance.Self is VPlayer);

        // Lobby menu: add the host-only checkbox, cloned from "Allow Public Match" so it matches the game's style.
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
        static void AddToggle(UIPrefab_InGameMenu __instance)
        {
            var menu = __instance.transform;
            var row = menu.Find(RowName);
            if (row == null)
            {
                var source = __instance.UE_PublicRoomToggle;
                var anchor = __instance.UE_RoomPassword as RectTransform;
                if (source == null || anchor == null) return;

                var container = new GameObject(RowName, typeof(RectTransform)).GetComponent<RectTransform>();
                container.SetParent(menu, false);
                container.anchorMin = anchor.anchorMin;
                container.anchorMax = anchor.anchorMax;
                container.pivot = anchor.pivot;
                container.sizeDelta = new Vector2(500f, 60f);
                // Same spacing as "Allow Public Match" -> "Use Entry Password", below the password row.
                var publicRoom = __instance.UE_PublicRoom as RectTransform;
                float step = publicRoom != null ? publicRoom.anchoredPosition.y - anchor.anchoredPosition.y : 115f;
                container.anchoredPosition = anchor.anchoredPosition - new Vector2(0f, step * 1.15f);

                var clone = Object.Instantiate(source.gameObject, container, false);
                clone.name = "Toggle";
                var label = clone.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "Infinite Stamina";
                var toggle = clone.GetComponent<Toggle>();
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = Infinite;
                toggle.onValueChanged.AddListener(on =>
                {
                    Infinite = on;
                    Debug.Log($"[Stamina] Infinite stamina {(on ? "enabled" : "disabled")} by host");
                });
                row = container;
            }
            row.gameObject.SetActive(IsHost);
            var t = row.GetComponentInChildren<Toggle>(true);
            if (t != null) t.SetIsOnWithoutNotify(Infinite);
        }
    }
}
