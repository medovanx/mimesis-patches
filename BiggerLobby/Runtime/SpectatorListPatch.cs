// MIMESIS BiggerLobby - spectator player list for more than 4 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// While dead, the spectator HUD lists every player (alive white, dead red). The prefab has 4 fixed rows and the game
// only fills the rows that exist, so a 5th+ player was missing. Copy the last row until there are MaxPlayers rows,
// continuing the same spacing, and hand the game the longer row list. Empty rows stay blank, as the game already does.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            var rows = __instance.playerListViewItems;
            if (rows == null || rows.Length < 2 || rows.Length >= Plugin.MaxPlayers) return;

            // Rows may be placed by a layout group that hasn't run yet; lay out first so positions are real.
            Canvas.ForceUpdateCanvases();
            var parent = (RectTransform)rows[0].transform.parent;
            if (parent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

            var first = (RectTransform)rows[0].transform;
            var second = (RectTransform)rows[1].transform;
            var last = (RectTransform)rows[rows.Length - 1].transform;
            var step = second.anchoredPosition - first.anchoredPosition;
            if (step.sqrMagnitude < 1f) step = new Vector2(0f, -Mathf.Max(first.rect.height, 40f) * 1.3f);   // fallback: below each other

            var all = new UIPrefab_Spectator_PlayerListViewItem[Plugin.MaxPlayers];
            rows.CopyTo(all, 0);
            for (int i = rows.Length; i < all.Length; i++)
            {
                var copy = Object.Instantiate(last.gameObject, last.parent, false);
                copy.name = last.gameObject.name + "_" + i;
                copy.SetActive(true);
                ((RectTransform)copy.transform).anchoredPosition = last.anchoredPosition + step * (i - rows.Length + 1);
                var item = copy.GetComponent<UIPrefab_Spectator_PlayerListViewItem>();
                item.UE_Name_Text.SetText(string.Empty);
                item.SpriteChangeAnimation.TurnOff();
                item.IsPossessor.gameObject.SetActive(false);
                all[i] = item;
            }
            __instance.playerListViewItems = all;
            Debug.Log($"[BiggerLobby] Spectator list: {rows.Length} -> {all.Length} rows, step {step}, parent {parent?.name} " +
                      $"layout {parent?.GetComponent<LayoutGroup>()?.GetType().Name ?? "none"}, mask {(parent?.GetComponentInParent<RectMask2D>() != null || parent?.GetComponentInParent<Mask>() != null)}");
        }

        // Diagnostics: once per list, log where every row is and what it shows after the game fills it.
        static readonly HashSet<int> _logged = new HashSet<int>();

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_Spectator_PlayerListView), nameof(UIPrefab_Spectator_PlayerListView.UpdatePlayerListView))]
        static void LogRows(UIPrefab_Spectator_PlayerListView __instance, List<System.Tuple<int, bool, bool, bool>> actorsInfo)
        {
            if (actorsInfo == null || actorsInfo.Count <= 4 || !_logged.Add(__instance.GetInstanceID())) return;
            var rows = __instance.playerListViewItems ?? new UIPrefab_Spectator_PlayerListViewItem[0];
            Debug.Log($"[BiggerLobby] Spectator list update: {actorsInfo.Count} players, {rows.Length} rows:\n" + string.Join("\n", rows.Select((r, i) =>
            {
                var rt = (RectTransform)r.transform;
                return $"  {i}: active={r.gameObject.activeInHierarchy} pos={rt.anchoredPosition} size={rt.rect.size} text='{r.UE_Name_Text.text}'";
            })));
        }
    }
}
