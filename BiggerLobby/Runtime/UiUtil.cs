// MIMESIS BiggerLobby - helpers for cloning fixed 4-slot UI
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BiggerLobby
{
    static class UiUtil
    {
        // Nested child whose name ends with the given suffix (e.g. "Name" finds "P4Name").
        public static T Find<T>(Transform root, string suffix) where T : Component =>
            root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name.EndsWith(suffix));

        // Same relative path in another hierarchy (maps a template row's parts onto its clone).
        public static T Map<T>(Transform templateRoot, Transform cloneRoot, T templatePart) where T : Component
        {
            if (templatePart == null) return null;
            var path = new List<int>();
            for (var t = templatePart.transform; t != templateRoot; t = t.parent)
            {
                // Not under the template root: fall back to a same-named object in the clone.
                if (t == null) return cloneRoot.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == templatePart.name);
                path.Insert(0, t.GetSiblingIndex());
            }
            var c = cloneRoot;
            foreach (var i in path) c = c.GetChild(i);
            return c.GetComponent<T>();
        }

        // Lays out player columns as a grid of up to `perRow` per row, scaled to fit the original 4-column area.
        public static void Grid(IList<RectTransform> cols, int count, float colW, float colH, float areaW, float top, int perRow = 5)
        {
            var group = cols[0].parent.GetComponent<LayoutGroup>();
            if (group != null) group.enabled = false;

            int nCols = Mathf.Min(count, perRow);
            int nRows = Mathf.CeilToInt(count / (float)perRow);
            float scale = Mathf.Min(1f, areaW / (nCols * colW), 1.6f / nRows);
            float left = (areaW - nCols * colW * scale) / 2f;
            for (int i = 0; i < cols.Count; i++)
            {
                var rt = cols[i];
                if (i >= count) { rt.gameObject.SetActive(false); continue; }
                int r = i / perRow, c = i % perRow;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(colW, colH);
                rt.localScale = Vector3.one * scale;
                rt.anchoredPosition = new Vector2(left + (c + 0.5f) * colW * scale, top - (r + 0.5f) * colH * scale);
            }
        }
    }
}
