// MIMESIS HudPercent - health and radiation percentages next to the HUD bars (top left)
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The HUD (UIPrefab_InGame) updates its bars through OnHpChanged and OnContaChanged; these postfixes write
// the same values as a percentage into a small label right of each bar, styled like the HUD's money text.
// Health is how much you have left; radiation is how contaminated you are (100% = full bar).

using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HudPercent
{
    [HarmonyPatch(typeof(UIPrefab_InGame))]
    static class HudPercentPatch
    {
        const string LabelName = "MedovanxPercent";
        const float Gap = 12f;

        [HarmonyPostfix, HarmonyPatch(nameof(UIPrefab_InGame.OnHpChanged))]
        static void Health(UIPrefab_InGame __instance, long curr, long maxHP) =>
            Set(__instance, __instance.UE_HP_bg != null ? __instance.UE_HP_bg.rectTransform : null, curr, maxHP, new Color(0.45f, 0.95f, 0.45f));

        [HarmonyPostfix, HarmonyPatch(nameof(UIPrefab_InGame.OnContaChanged))]
        static void Radiation(UIPrefab_InGame __instance, long curr, long maxContaVal) =>
            Set(__instance, __instance.oxyGauge != null ? __instance.oxyGauge.transform as RectTransform : null, curr, maxContaVal, new Color(0.55f, 0.9f, 0.3f));

        static void Set(UIPrefab_InGame hud, RectTransform bar, long curr, long max, Color color)
        {
            if (bar == null) return;
            var label = bar.Find(LabelName)?.GetComponent<TMP_Text>() ?? Create(hud, bar, color);
            int percent = max <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(100f * curr / max), 0, 100);
            label.text = percent + "%";
        }

        // Child of the bar, anchored to its right edge so it follows the bar wherever the HUD puts it.
        static TMP_Text Create(UIPrefab_InGame hud, RectTransform bar, Color color)
        {
            var label = new GameObject(LabelName, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var rt = label.rectTransform;
            rt.SetParent(bar, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(Gap, 0f);
            rt.sizeDelta = new Vector2(80f, 30f);
            // Same look as the HUD's own text; scaled back if the bar itself is scaled.
            var style = hud.UE_Currency;
            if (style != null) { label.font = style.font; label.fontSharedMaterial = style.fontSharedMaterial; }
            label.fontSize = 20f;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = color;
            label.raycastTarget = false;
            var s = bar.lossyScale.x;
            if (s > 0.01f && hud.transform.lossyScale.x > 0.01f) rt.localScale = Vector3.one * (hud.transform.lossyScale.x / s);
            return label;
        }
    }
}
