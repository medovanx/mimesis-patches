// MIMESIS BiggerLobby - end-of-level, death match and session report screens for up to 10 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ReluProtocol;
using ReluProtocol.Enum;
using UnityEngine;

namespace BiggerLobby
{
    // Each screen fills its 4 fixed player columns and stops. These postfixes copy the 4th column for players 5-10,
    // fill the copies with the column's own method and hand the game the longer column list (its OnShow shows the
    // first _resultCount columns). When the screen opens, all columns are laid out as rows of up to 5.
    // With 4 players or fewer nothing changes.
    //
    // One class per screen: Harmony skips a whole class when one of its targets is missing, so a game update that
    // breaks one screen leaves the others working. In each class the layout (OnShow) comes first: Harmony applies a
    // class's patches in the order they are declared and stops at the first one that fails, so a screen never gets
    // players 5-10 without the layout that fits them. On its own, the layout does nothing.
    [HarmonyPatch]
    static class SurvivalResultPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_SurvivalResult), nameof(UIPrefab_SurvivalResult.OnShow))]
        static void SurvivalLayout(UIPrefab_SurvivalResult __instance) => ResultScreen.Layout(__instance._playerColumns, __instance._resultCount, stretch: true);

        // parameters: [cycle, stage, success, playerCount, (name, survivalState, award) x playerCount, lost items...]
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_SurvivalResult), nameof(UIPrefab_SurvivalResult.PatchParameter))]
        static void Survival(UIPrefab_SurvivalResult __instance, object[] __0)
        {
            UIPrefab_SurvivalResultPlayerColumn[] all = null;
            try
            {
                var cols = __instance._playerColumns;
                if (cols == null || __0 == null || __0.Length < 4) return;
                // The game skips a player whose actor it can't find, so trust the entries rather than the count.
                int count = cols.Length, wanted = Mathf.Min((int)__0[3], Plugin.MaxPlayers);
                while (count < wanted && 3 * count + 6 < __0.Length && __0[3 * count + 5] is UIPrefab_SurvivalResult.eActorSurvivalState && __0[3 * count + 6] is AwardType) count++;
                if (count <= cols.Length) return;
                all = UiUtil.Extend(cols, count, "UIPrefab_SurvivalResultPlayerColumn");
                if (all == null) { UiUtil.WarnOnce("Survival result: columns not found, showing 4 players"); return; }
                for (int i = cols.Length; i < count; i++)
                {
                    all[i].gameObject.SetActive(true);
                    all[i].PatchResult(__0[3 * i + 4]?.ToString() ?? "", (UIPrefab_SurvivalResult.eActorSurvivalState)__0[3 * i + 5], (AwardType)__0[3 * i + 6]);
                }
                __instance._playerColumns = all;
                __instance._resultCount = count;
            }
            catch (Exception e)
            {
                ResultScreen.Hide(all, 4);
                UiUtil.WarnOnce("Survival result: could not add players 5+", e);
            }
        }
    }

    [HarmonyPatch]
    static class DeathMatchResultPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_DeathMatchResult), nameof(UIPrefab_DeathMatchResult.OnShow))]
        static void DeathMatchLayout(UIPrefab_DeathMatchResult __instance)
        {
            int count = __instance._resultCount;
            var stamps = __instance._playerWellDoneStamps;
            if (count <= 4 || stamps == null || stamps.Length < count) return;
            try
            {
                // The stamps sit in their own row above the columns, which fits 4: move them next to their column.
                // The game places a stamp 182 above its column's centre; keep that, with room for it above every row.
                var block = __instance._playerColumns[0].transform.parent;
                var row = stamps[0].transform.parent.parent;
                var heads = stamps.Take(count).Select(s => (RectTransform)s.transform.parent).ToArray();
                foreach (var head in heads) head.SetParent(block, false);
                if (row != block && row.childCount == 0) row.gameObject.SetActive(false);
                ResultScreen.Layout(__instance._playerColumns, count, stretch: false, heads, 174f, 182f);
            }
            catch (Exception e)
            {
                UiUtil.WarnOnce("Death match result: layout failed", e);
            }
        }

        // parameters: (name, isSurvivor, result text) x players
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_DeathMatchResult), nameof(UIPrefab_DeathMatchResult.PatchParameter))]
        static void DeathMatch(UIPrefab_DeathMatchResult __instance, object[] __0)
        {
            UIPrefab_ResultPlayerColumn[] all = null;
            Transform[] icons = null;
            try
            {
                var cols = __instance._playerColumns;
                int count = __0 == null ? 0 : Mathf.Min(__0.Length / 3, Plugin.MaxPlayers);
                if (cols == null || count <= cols.Length) return;
                // Each player also has a stamp (well done / failed) in an icon slot of its own.
                var icon = __instance.UE_UI_Deathmatchicon4;
                all = UiUtil.Extend(cols, count, "UIPrefab_ResultPlayerColumn");
                icons = UiUtil.Extend(new[] { __instance.UE_UI_Deathmatchicon1, __instance.UE_UI_Deathmatchicon2, __instance.UE_UI_Deathmatchicon3, icon }, count, "UI_Deathmatchicon");
                if (all == null || icons == null) { UiUtil.WarnOnce("Death match result: columns not found, showing 4 players"); ResultScreen.Hide(all, 4); ResultScreen.Hide(icons, 4); return; }
                var wellDone = ResultScreen.Grow(__instance._playerWellDoneStamps, count);
                var failed = ResultScreen.Grow(__instance._playerFailedStamps, count);
                var survived = ResultScreen.Grow(__instance._survivalResults, count);
                for (int i = cols.Length; i < count; i++)
                {
                    wellDone[i] = UiUtil.Map(icon, icons[i], __instance.UE_PlayerWellDone4);
                    failed[i] = UiUtil.Map(icon, icons[i], __instance.UE_PlayerFailed4);
                    // The game's OnShow shows one of the two stamps of every player: both must exist.
                    if (wellDone[i] == null || failed[i] == null) throw new Exception("stamp images not found in " + icons[i].name);
                    wellDone[i].gameObject.SetActive(false);
                    failed[i].gameObject.SetActive(false);
                    icons[i].gameObject.SetActive(true);
                    survived[i] = (bool)__0[3 * i + 1];
                    all[i].gameObject.SetActive(true);
                    all[i].PatchDeathMatchResult(__0[3 * i]?.ToString() ?? "", survived[i], __0[3 * i + 2]?.ToString() ?? "");
                }
                __instance._playerColumns = all;
                __instance._playerWellDoneStamps = wellDone;
                __instance._playerFailedStamps = failed;
                __instance._survivalResults = survived;
                __instance._resultCount = count;
            }
            catch (Exception e)
            {
                ResultScreen.Hide(all, 4);
                ResultScreen.Hide(icons, 4);
                UiUtil.WarnOnce("Death match result: could not add players 5+", e);
            }
        }
    }

    [HarmonyPatch]
    static class SessionReportPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_SessionReport), nameof(UIPrefab_SessionReport.OnShow))]
        static void SessionReportLayout(UIPrefab_SessionReport __instance) => ResultScreen.Layout(__instance._playerColumns, __instance._resultCount, stretch: false);

        // parameters: [play report, actor names]; the game fills one column per entry of the report's playerAwards.
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_SessionReport), nameof(UIPrefab_SessionReport.PatchParameter))]
        static void SessionReport(UIPrefab_SessionReport __instance, object[] __0)
        {
            UIPrefab_ResultPlayerColumn[] all = null;
            try
            {
                var cols = __instance._playerColumns;
                var awards = (__0?.Length > 1 ? __0[0] as GameSessionPlayReport : null)?.playerAwards;
                var names = __0?.Length > 1 ? __0[1] as Dictionary<int, string> : null;
                int count = awards == null ? 0 : Mathf.Min(awards.Count, Plugin.MaxPlayers);
                if (cols == null || count <= cols.Length) return;
                all = UiUtil.Extend(cols, count, "UIPrefab_ResultPlayerColumn");
                if (all == null) { UiUtil.WarnOnce("Session report: columns not found, showing 4 players"); return; }
                int i = 0;
                foreach (var award in awards)   // same order as the game's SetAwards
                {
                    if (i >= count) break;
                    if (i >= cols.Length)
                    {
                        all[i].PatchSessionReport(names != null && names.TryGetValue(award.Key, out var name) ? name : "???", award.Value);
                        all[i].gameObject.SetActive(true);
                    }
                    i++;
                }
                __instance._playerColumns = all;
                __instance._resultCount = count;
            }
            catch (Exception e)
            {
                ResultScreen.Hide(all, 4);
                UiUtil.WarnOnce("Session report: could not add players 5+", e);
            }
        }
    }

    // Shared by the three screens above.
    static class ResultScreen
    {
        // stretch: the survival screen's row made every column as tall as the tallest (the others have a fixed height).
        internal static void Layout(Component[] cols, int count, bool stretch, IList<RectTransform> heads = null, float headH = 0f, float headY = 0f)
        {
            if (cols == null || count <= 4 || cols.Length < count) return;
            try
            {
                var rts = cols.Take(count).Select(c => (RectTransform)c.transform).ToList();
                float cellH = stretch ? rts.Max(UiUtil.PreferredHeight) : rts[0].rect.height;
                UiUtil.Rows(rts, count, cellH, heads, headH, headY);
                UiUtil.FitPopup(rts[0]);
            }
            catch (Exception e)
            {
                UiUtil.WarnOnce("Result screen layout failed", e);
            }
        }

        internal static T[] Grow<T>(T[] items, int count)
        {
            var all = new T[count];
            Array.Copy(items, all, Mathf.Min(items.Length, count));
            return all;
        }

        // Undo a half-done extension: the game only knows its own 4 columns, so copies it doesn't fill must not show.
        internal static void Hide(Component[] items, int keep)
        {
            if (items == null) return;
            for (int i = keep; i < items.Length; i++)
                if (items[i] != null) items[i].gameObject.SetActive(false);
        }
    }
}
