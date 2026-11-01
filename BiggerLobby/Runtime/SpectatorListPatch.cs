// MIMESIS BiggerLobby - spectator player list for more than 4 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// While dead, the spectator HUD lists every player (alive white, dead red). The prefab has 4 fixed rows and the game
// only fills the rows that exist, so a 5th+ player was missing. Copy the last row until there are MaxPlayers rows and
// hand the game the longer row list. The list is a vertical layout, so the copies line up below the first four.
// Empty rows stay blank, as the game already does.

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BiggerLobby
{
    [HarmonyPatch]
    static class SpectatorListPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_Spectator_PlayerListView), "Start")]
        static void AddRows(UIPrefab_Spectator_PlayerListView __instance)
        {
            UIPrefab_Spectator_PlayerListViewItem[] all = null;
            try
            {
                var rows = __instance.playerListViewItems;
                if (rows == null || rows.Length < 2 || rows.Length >= Plugin.MaxPlayers) return;
                all = UiUtil.Extend(rows, Plugin.MaxPlayers, "UIPrefab_Spectator_PlayerListViewItem");
                if (all == null) { UiUtil.WarnOnce("Spectator list: rows not found, showing 4 players"); return; }
                for (int i = rows.Length; i < all.Length; i++)
                {
                    // As the game's Start does for its rows: activate twice (a row hides itself the first time), alive, blank.
                    all[i].gameObject.SetActive(true);
                    if (!all[i].gameObject.activeSelf) all[i].gameObject.SetActive(true);
                    all[i].SetDead(false);
                    all[i].Clear();
                }

                // Without a layout group (an older or changed prefab), continue the spacing of the first rows by hand.
                var parent = (RectTransform)rows[0].transform.parent;
                if (parent.GetComponent<LayoutGroup>() == null)
                {
                    var first = (RectTransform)rows[0].transform;
                    var last = (RectTransform)rows[rows.Length - 1].transform;
                    var step = ((RectTransform)rows[1].transform).anchoredPosition - first.anchoredPosition;
                    if (step.sqrMagnitude < 1f) step = new Vector2(0f, -Mathf.Max(first.rect.height, 40f) * 1.3f);
                    for (int i = rows.Length; i < all.Length; i++)
                        ((RectTransform)all[i].transform).anchoredPosition = last.anchoredPosition + step * (i - rows.Length + 1);
                }
                __instance.playerListViewItems = all;
                Debug.Log($"[BiggerLobby] Spectator list: {rows.Length} -> {all.Length} rows, parent {parent.name} " +
                          $"layout {parent.GetComponent<LayoutGroup>()?.GetType().Name ?? "none"}");
            }
            catch (Exception e)
            {
                if (all != null && __instance.playerListViewItems != all)
                    for (int i = 4; i < all.Length; i++) if (all[i] != null) all[i].gameObject.SetActive(false);
                UiUtil.WarnOnce("Spectator list: could not add rows 5+", e);
            }
        }

        // Diagnostics: once per list, log where every row is and what it shows after the game fills it.
        static readonly HashSet<int> _logged = new HashSet<int>();

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_Spectator_PlayerListView), nameof(UIPrefab_Spectator_PlayerListView.UpdatePlayerListView))]
        static void LogRows(UIPrefab_Spectator_PlayerListView __instance, List<Tuple<int, bool, bool, bool>> actorsInfo)
        {
            if (actorsInfo == null || actorsInfo.Count <= 4 || !_logged.Add(__instance.GetInstanceID())) return;
            var rows = __instance.playerListViewItems ?? new UIPrefab_Spectator_PlayerListViewItem[0];
            Debug.Log($"[BiggerLobby] Spectator list update: {actorsInfo.Count} players, {rows.Length} rows:\n" + string.Join("\n", rows.Where(r => r != null).Select((r, i) =>
            {
                var rt = (RectTransform)r.transform;
                return $"  {i}: active={r.gameObject.activeInHierarchy} pos={rt.anchoredPosition} size={rt.rect.size} text='{r.UE_UI_Text_PlayerName?.text}'";
            })));
        }
    }
}
