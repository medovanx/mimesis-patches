// MIMESIS BiggerLobby - pause/lobby menu player list with a row for each of up to 10 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The menu (UIPrefab_InGameMenu_new) has 4 player rows. Everything else in it already goes by the length of its row
// list: filling the rows, volume, mute, profile, kick and ping. So rows 5-10 are copies of the 4th, wired the way the
// game wires its own, and the list is handed over. The game shows a row per joined player and hides the rest.

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BiggerLobby
{
    [HarmonyPatch(typeof(UIPrefab_InGameMenu_new))]
    static class InGameMenuPatch
    {
        const float Gap = 24f;          // between the last row, the divider line and the menu buttons
        const float TightSpacing = 6f;  // row spacing once the rows no longer fit with the game's

        // Awake builds the row list and wires each row's volume and kick buttons to its index.
        [HarmonyPostfix, HarmonyPatch("Awake")]
        static void AddRows(UIPrefab_InGameMenu_new __instance)
        {
            try
            {
                var rows = __instance.playerItems;
                if (rows == null || rows.Length == 0 || rows.Length >= Plugin.MaxPlayers) return;
                var all = UiUtil.Extend(rows, Plugin.MaxPlayers, "UI_PlayerList");
                if (all == null) { UiUtil.WarnOnce("Menu player list: rows not found, showing 4 players"); return; }
                for (int i = rows.Length; i < all.Length; i++)
                {
                    int index = i;
                    all[i].OnUI_Button_Icon_volume = _ => __instance.OnClickSpeakButton(index);
                    all[i].OnUI_Button_IconBox_kick = _ => __instance.OpenKickPlayerPopup(index);
                }
                __instance.playerItems = all;
            }
            catch (Exception e)
            {
                UiUtil.WarnOnce("Menu player list: could not add rows 5+", e);
            }
        }

        // The game's spacing and divider position, to put back when 4 or fewer players are shown again.
        static readonly Dictionary<int, (float spacing, Vector2 divider)> Original = new Dictionary<int, (float, Vector2)>();

        // The list has room for 4 rows above the divider line and the menu buttons below it. With more players, use the
        // space down to the buttons, tighten the spacing and if needed shrink the rows; the divider follows the list.
        [HarmonyPostfix, HarmonyPatch(nameof(UIPrefab_InGameMenu_new.SetRemoteVolumeController_v2))]
        static void FitRows(UIPrefab_InGameMenu_new __instance)
        {
            try
            {
                var rows = __instance.playerItems;
                if (rows == null || rows.Length <= 4 || rows[0] == null) return;
                var list = (RectTransform)rows[0].transform.parent;   // PlayerGroup
                var group = list.GetComponent<VerticalLayoutGroup>();
                var divider = list.parent.Find("UI_Deco_DivideLine_Horizon") as RectTransform;
                var buttons = list.parent.Find("MenuGroup");
                if (group == null || buttons == null) { UiUtil.WarnOnce("Menu player list: layout not found, rows not fitted"); return; }
                int id = __instance.GetInstanceID();
                if (!Original.TryGetValue(id, out var original))
                    Original[id] = original = (group.spacing, divider != null ? divider.anchoredPosition : Vector2.zero);

                int shown = rows.Count(r => r != null && r.gameObject.activeSelf);
                float rowH = ((RectTransform)rows[0].transform).rect.height, spacing = original.spacing, scale = 1f;
                float room = list.rect.height - StackHeight(buttons) - 2 * Gap - (divider != null ? divider.rect.height : 0f);
                if (shown > 4 && shown * rowH + (shown - 1) * spacing > room)
                {
                    spacing = TightSpacing;
                    scale = Mathf.Clamp((room - (shown - 1) * spacing) / (shown * rowH), 0.5f, 1f);
                }
                group.spacing = spacing;
                group.childScaleHeight = true;   // pack the rows at their scaled height
                foreach (var row in rows)
                    if (row != null) row.transform.localScale = new Vector3(scale, scale, 1f);

                // The divider is anchored to the top like the list: put it under the last row.
                if (divider != null && Mathf.Approximately(divider.anchorMin.y, 1f) && Mathf.Approximately(divider.anchorMax.y, 1f))
                    divider.anchoredPosition = shown <= 4 ? original.divider
                        : new Vector2(original.divider.x, list.offsetMax.y - (shown * rowH * scale + (shown - 1) * spacing) - Gap);
            }
            catch (Exception e)
            {
                UiUtil.WarnOnce("Menu player list: could not fit the rows", e);
            }
        }

        // Height of a vertical layout whose rows keep their own height (the menu buttons).
        static float StackHeight(Transform t)
        {
            var g = t.GetComponent<VerticalLayoutGroup>();
            var shown = t.Cast<RectTransform>().Where(c => c.gameObject.activeSelf).ToList();
            if (shown.Count == 0) return 0f;
            return shown.Sum(c => c.rect.height) + (g != null ? g.padding.vertical + g.spacing * (shown.Count - 1) : 0f);
        }
    }
}
