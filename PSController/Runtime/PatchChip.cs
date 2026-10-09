// MIMESIS PSController - "installed patch" chip on the main menu
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
// Shared design: every patch adds its own chip to one "PatchChips" row above the main menu version text.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PSController
{
    sealed class PatchChip : MonoBehaviour
    {
        string _label;

        public static void Register(string label)
        {
            var go = new GameObject("PatchChip_" + label);
            DontDestroyOnLoad(go);
            go.AddComponent<PatchChip>()._label = label;
        }

        IEnumerator Start()
        {
            var wait = new WaitForSeconds(0.5f);
            while (true)
            {
                var menu = GameObject.Find("UIPrefab_MainMenu");
                var rect = menu != null ? menu.transform.Find("VersionTextRect") : null;
                var version = rect != null ? rect.Find("versionText")?.GetComponent<TMP_Text>() : null;
                if (version != null) AddChip(Row(rect, version), version);
                yield return wait;
            }
        }

        static RectTransform Row(Transform rect, TMP_Text version)
        {
            var row = rect.Find("PatchChips") as RectTransform;
            if (row != null) return row;
            row = new GameObject("PatchChips", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(rect, false);
            var v = version.rectTransform;
            row.anchorMin = v.anchorMin;
            row.anchorMax = v.anchorMax;
            row.pivot = new Vector2(1f, 0f);
            row.sizeDelta = new Vector2(800f, 30f);
            // Right-aligned with the version text, just above it.
            row.anchoredPosition = v.anchoredPosition + new Vector2(v.rect.width * (1f - v.pivot.x), v.rect.height * (1f - v.pivot.y) - 30f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerRight;
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row;
        }

        void AddChip(RectTransform row, TMP_Text version)
        {
            var name = "Chip_" + _label;
            if (row.Find(name) != null) return;
            var chip = new GameObject(name, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(row, false);
            var bg = chip.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;
            var pad = chip.AddComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(10, 10, 3, 3);
            pad.childControlWidth = pad.childControlHeight = true;
            chip.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(chip.transform, false);
            text.font = version.font;
            text.fontSize = version.fontSize * 0.8f;
            text.color = new Color(0.85f, 0.95f, 0.85f, 1f);
            text.text = _label;
            text.raycastTarget = false;
        }
    }
}
