// MIMESIS BiggerLobby - lobby size
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The game's player limit is one value from its data tables, DataConsts.C_MaxPlayerCount (4). Every join check
// reads it (GameSessionInfo, VRoom, VRoomManager), so it's raised to MaxPlayers when the tables load. Two readers
// are kept at 4: the survival result screen, which has 4 fixed slots (ResultScreenPatch adds the rest), and an
// admin test helper. The Steam lobby itself is created with room for MaxPlayers.

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
        static readonly FieldInfo MaxField = AccessTools.Field(typeof(DataConsts), nameof(DataConsts.C_MaxPlayerCount));

        [HarmonyPostfix, HarmonyPatch(typeof(DataConsts), MethodType.Constructor, typeof(Bifrost.Const.Const_MasterDataHolder))]
        static void RaiseLimit(DataConsts __instance)
        {
            MaxField.SetValue(__instance, Plugin.MaxPlayers);   // readonly field, so set it by reflection
            Debug.Log($"[BiggerLobby] Player limit set to {Plugin.MaxPlayers}");
        }

        // These keep reading 4: replace "consts.C_MaxPlayerCount" with "pop; 4".
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIPrefab_SurvivalResult), nameof(UIPrefab_SurvivalResult.PatchParameter))]
        static IEnumerable<CodeInstruction> ResultScreenKeepsFour(IEnumerable<CodeInstruction> code) => KeepFour(code);

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(VWorld), nameof(VWorld.AdminCommandSaveGameData))]
        static IEnumerable<CodeInstruction> TestHelperKeepsFour(IEnumerable<CodeInstruction> code) => KeepFour(code);

        static IEnumerable<CodeInstruction> KeepFour(IEnumerable<CodeInstruction> code)
        {
            foreach (var x in code)
            {
                if (x.LoadsField(MaxField))
                {
                    yield return new CodeInstruction(OpCodes.Pop).MoveLabelsFrom(x);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                }
                else yield return x;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.CreateLobby))]
        static void LobbyRoom(ref int cMaxMembers) => cMaxMembers = Plugin.MaxPlayers;
    }
}
