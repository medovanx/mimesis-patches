// MIMESIS BiggerLobby - spectator player list for more than 4 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// While dead, the spectator HUD lists every player (alive white, dead red). The prefab has 4 fixed rows and the game
// only fills the rows that exist, so a 5th+ player was missing. Copy the last row until there are MaxPlayers rows,
// continuing the same spacing, and hand the game the longer row list. Empty rows stay blank, as the game already does.

using HarmonyLib;
using UnityEngine;

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

            var first = (RectTransform)rows[0].transform;
            var last = (RectTransform)rows[rows.Length - 1].transform;
            // Spacing between rows, from the prefab's own layout (a layout group would place copies by itself anyway).
            var step = (last.anchoredPosition - first.anchoredPosition) / (rows.Length - 1);

            var all = new UIPrefab_Spectator_PlayerListViewItem[Plugin.MaxPlayers];
            rows.CopyTo(all, 0);
            for (int i = rows.Length; i < all.Length; i++)
            {
                var copy = Object.Instantiate(last.gameObject, last.parent, false);
                copy.name = last.gameObject.name + "_" + i;
                ((RectTransform)copy.transform).anchoredPosition = last.anchoredPosition + step * (i - rows.Length + 1);
                var item = copy.GetComponent<UIPrefab_Spectator_PlayerListViewItem>();
                item.UE_Name_Text.SetText(string.Empty);
                item.SpriteChangeAnimation.TurnOff();
                item.IsPossessor.gameObject.SetActive(false);
                all[i] = item;
            }
            __instance.playerListViewItems = all;
            Debug.Log($"[BiggerLobby] Spectator list: {rows.Length} -> {all.Length} rows");
        }
    }
}
