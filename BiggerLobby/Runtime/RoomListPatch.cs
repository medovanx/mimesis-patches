// MIMESIS BiggerLobby - public room lists show rooms with more than 4 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Both public room lists (main menu and the tram terminal) drop every room with 4 or more players, on top of Steam's
// own "has a free slot" filter, and label rooms "n/4". So a BiggerLobby room vanished from the list at 4 players.
// Keep rooms up to MaxPlayers (Steam still leaves out full ones, including full 4-player rooms) and show each room's
// real limit. This only changes what the player browsing sees: it helps players who have BiggerLobby too.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Steamworks;
using TMPro;

namespace BiggerLobby
{
    [HarmonyPatch]
    static class RoomListPatch
    {
        static readonly FieldInfo PlayerCount = AccessTools.Field(typeof(PublicRoomListData), nameof(PublicRoomListData.PlayerCount));

        [HarmonyTranspiler, HarmonyPatch(typeof(UIPrefab_PublicRoomList), nameof(UIPrefab_PublicRoomList.SetRoomList))]
        static IEnumerable<CodeInstruction> MenuList(IEnumerable<CodeInstruction> code) => KeepBigRooms(code);

        [HarmonyTranspiler, HarmonyPatch(typeof(UIPrefab_StartTram), nameof(UIPrefab_StartTram.SetRoomList))]
        static IEnumerable<CodeInstruction> TramList(IEnumerable<CodeInstruction> code) => KeepBigRooms(code);

        // "if (data.PlayerCount < 4)" becomes "< MaxPlayers".
        static IEnumerable<CodeInstruction> KeepBigRooms(IEnumerable<CodeInstruction> code)
        {
            CodeInstruction prev = null;
            foreach (var x in code)
            {
                if (prev != null && prev.LoadsField(PlayerCount) && x.opcode == OpCodes.Ldc_I4_4)
                    yield return new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)Plugin.MaxPlayers).MoveLabelsFrom(x);
                else yield return x;
                prev = x;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UiPrefab_RoomCard), nameof(UiPrefab_RoomCard.SetRoomData))]
        static void CardLimit(UiPrefab_RoomCard __instance, PublicRoomListData data) => ShowLimit(__instance.UE_PlayerCount, data);

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_PublicTramListItem), nameof(UIPrefab_PublicTramListItem.SetRoomData))]
        static void TramLimit(UIPrefab_PublicTramListItem __instance, PublicRoomListData data) => ShowLimit(__instance.UE_PublicTram_PlayerCountText, data);

        // "n/4" -> "n/10" for a room created with room for 10; 4-player rooms keep the game's text.
        static void ShowLimit(TMP_Text label, PublicRoomListData data)
        {
            try
            {
                if (label == null || data == null) return;
                int limit = SteamMatchmaking.GetLobbyMemberLimit(data.lobbyID);
                if (limit > 4) label.text = data.PlayerCount + "/" + limit;
            }
            catch (Exception e)
            {
                UiUtil.WarnOnce("Room list: could not show the room's player limit", e);
            }
        }
    }
}
