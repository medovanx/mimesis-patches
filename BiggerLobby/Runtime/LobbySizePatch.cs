// MIMESIS BiggerLobby - lobby size
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The game's player limit is one value from its data tables, DataConsts.C_MaxPlayerCount (4). Every join check
// reads it (GameSessionInfo, VRoom, VRoomManager x2), so it's raised to MaxPlayers when the tables load. The only
// other reader, an admin test helper, keeps 4 (TestHelperPatch). The Steam lobby itself is created with room for
// MaxPlayers. The result screens no longer read the limit; they stop at their own 4 columns (ResultScreenPatch.cs).

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Bifrost.ConstEnum;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace BiggerLobby
{
    [HarmonyPatch]
    static class LobbySizePatch
    {
        internal static readonly FieldInfo MaxField = AccessTools.Field(typeof(DataConsts), nameof(DataConsts.C_MaxPlayerCount));

        [HarmonyPostfix, HarmonyPatch(typeof(DataConsts), MethodType.Constructor, typeof(Bifrost.Const.Const_MasterDataHolder))]
        static void RaiseLimit(DataConsts __instance)
        {
            // An exception here would fail the game's data loading, which closes the game: just report it.
            if (MaxField == null) { Debug.LogError("[BiggerLobby] DataConsts.C_MaxPlayerCount not found, lobby size unchanged"); return; }
            MaxField.SetValue(__instance, Plugin.MaxPlayers);   // readonly field, so set it by reflection
            Debug.Log($"[BiggerLobby] Player limit set to {Plugin.MaxPlayers}");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.CreateLobby))]
        static void LobbyRoom(ref int cMaxMembers) => cMaxMembers = Plugin.MaxPlayers;
    }

    // Its own class, so if a game update removes the helper, the lobby size above still applies.
    [HarmonyPatch]
    static class TestHelperPatch
    {
        // The admin save command fills the save's player list up to the limit with fake names: keep it at 4,
        // by replacing "consts.C_MaxPlayerCount" with "pop; 4".
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(VWorld), nameof(VWorld.AdminCommandSaveGameData))]
        static IEnumerable<CodeInstruction> TestHelperKeepsFour(IEnumerable<CodeInstruction> code)
        {
            foreach (var x in code)
            {
                if (x.LoadsField(LobbySizePatch.MaxField))
                {
                    yield return new CodeInstruction(OpCodes.Pop).MoveLabelsFrom(x);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                }
                else yield return x;
            }
        }
    }
}
