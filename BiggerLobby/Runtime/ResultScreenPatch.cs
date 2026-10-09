// MIMESIS BiggerLobby - end-of-level and death match result screens for up to 10 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System.Collections.Generic;
using HarmonyLib;
using ReluProtocol.Enum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BiggerLobby
{
    // The game fills its 4 fixed columns (P1-P4) and stops. These postfixes clone P4's column for
    // players 5-10, fill them the same way, and lay all columns out as a 2x5 grid.
    [HarmonyPatch]
    static class ResultScreenPatch
    {
        // Extra columns per screen instance, created once and reused.
        static readonly Dictionary<int, List<RectTransform>> Extra = new Dictionary<int, List<RectTransform>>();

        static List<RectTransform> Columns(Component screen, RectTransform p4, int count)
        {
            var cols = new List<RectTransform>();
            var parent = p4.parent;
            for (int i = 1; i <= 4; i++) cols.Add((RectTransform)parent.Find("Player" + i));
            if (!Extra.TryGetValue(screen.GetInstanceID(), out var extra))
                Extra[screen.GetInstanceID()] = extra = new List<RectTransform>();
            while (extra.Count < Plugin.MaxPlayers - 4)
            {
                var c = (RectTransform)Object.Instantiate(p4.gameObject, parent).transform;
                c.name = "Player" + (5 + extra.Count);
                extra.Add(c);
            }
            cols.AddRange(extra);
            for (int i = 4; i < cols.Count; i++) cols[i].gameObject.SetActive(i < count);
            return cols;
        }

        static void Reset(Transform col)
        {
            foreach (var s in new[] { "Survival", "Killed", "Wasted", "Award", "WellDone" })
            {
                var part = UiUtil.Find<Graphic>(col, s);
                if (part != null) part.gameObject.SetActive(false);
            }
        }

        static string AwardText(AwardType award) => award switch
        {
            AwardType.BestCarryItem => Hub.GetL10NText("STRING_SETTLEMENT_REPORT_CASE_1"),
            AwardType.BestDamageToAlly => Hub.GetL10NText("STRING_SETTLEMENT_REPORT_CASE_2"),
            AwardType.BestMimicEncounter => Hub.GetL10NText("STRING_SETTLEMENT_REPORT_CASE_3"),
            AwardType.BestCamper => Hub.GetL10NText("STRING_SETTLEMENT_REPORT_CASE_4"),
            _ => "",
        };

        // parameters: [title cycle, hasLostScrap, playerCount, (name, survivalState, award) x playerCount, ...]
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_SurvivalResult), nameof(UIPrefab_SurvivalResult.PatchParameter))]
        static void Survival(UIPrefab_SurvivalResult __instance, object[] parameters)
        {
            int count = Mathf.Min((int)parameters[2], Plugin.MaxPlayers);
            if (count <= 4) return;
            var p4 = (RectTransform)__instance.UE_P4Name.transform.parent;
            var cols = Columns(__instance, p4, count);
            for (int i = 4; i < count; i++)
            {
                var col = cols[i];
                Reset(col);
                UiUtil.Find<TMP_Text>(col, "Name").SetText(parameters[3 * i + 3].ToString());
                var state = (UIPrefab_SurvivalResult.eActorSurvivalState)parameters[3 * i + 4];
                var stateText = state switch
                {
                    UIPrefab_SurvivalResult.eActorSurvivalState.Survive => "Survival",
                    UIPrefab_SurvivalResult.eActorSurvivalState.Killed => "Killed",
                    UIPrefab_SurvivalResult.eActorSurvivalState.Wasted => "Wasted",
                    _ => null,
                };
                if (stateText != null) UiUtil.Find<TMP_Text>(col, stateText)?.gameObject.SetActive(true);
                var award = UiUtil.Find<TMP_Text>(col, "Award");
                award.SetText(AwardText((AwardType)parameters[3 * i + 5]));
                award.gameObject.SetActive(true);
            }
            var area = ((RectTransform)p4.parent).rect;
            UiUtil.Grid(cols, count, area.width / 4f, p4.rect.height, area.width, 0f);
        }

        // parameters: (name, isSurvivor, award text) x players
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_DeathMatchResult), nameof(UIPrefab_DeathMatchResult.PatchParameter))]
        static void DeathMatch(UIPrefab_DeathMatchResult __instance, object[] parameters)
        {
            int count = Mathf.Min(parameters.Length / 3, Plugin.MaxPlayers);
            if (count <= 4) return;
            var p4 = (RectTransform)__instance.UE_P4Name.transform.parent;
            var cols = Columns(__instance, p4, count);
            var stamps = Traverse.Create(__instance).Field("stampObjectsToShow").GetValue<List<GameObject>>();
            for (int i = 4; i < count; i++)
            {
                var col = cols[i];
                Reset(col);
                UiUtil.Find<TMP_Text>(col, "Name").SetText(parameters[3 * i].ToString());
                if ((bool)parameters[3 * i + 1])
                {
                    stamps.Add(UiUtil.Find<Image>(col, "WellDone").gameObject);
                    UiUtil.Find<TMP_Text>(col, "Survival").gameObject.SetActive(true);
                    __instance.UE_NoSurvivors.gameObject.SetActive(false);
                }
                else
                {
                    var award = UiUtil.Find<TMP_Text>(col, "Award");
                    award.SetText(parameters[3 * i + 2].ToString());
                    award.gameObject.SetActive(true);
                }
            }
            UiUtil.Grid(cols, count, p4.rect.width, p4.rect.height, ((RectTransform)p4.parent).rect.width, 0f);
        }
    }
}
