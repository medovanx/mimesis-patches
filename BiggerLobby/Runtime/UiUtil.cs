// MIMESIS BiggerLobby - helpers for growing the game's fixed 4-slot UI
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiggerLobby
{
    static class UiUtil
    {
        static readonly HashSet<string> Warned = new HashSet<string>();

        // UI paths come from the game's prefabs and can change with an update: warn once and leave the game's UI as it is.
        public static void WarnOnce(string what, Exception e = null)
        {
            if (Warned.Add(what)) Debug.LogWarning("[BiggerLobby] " + what + (e != null ? ": " + e : ""));
        }

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

        // The game's items plus copies of the last one up to `count` (named name5, name6, ...), each placed right after
        // the previous one so layout groups keep the order. Copies from an earlier call are reused. Null if it can't.
        public static T[] Extend<T>(T[] items, int count, string name) where T : Component
        {
            if (items == null || items.Length == 0 || items.Any(x => x == null)) return null;
            var all = new T[Mathf.Max(count, items.Length)];
            items.CopyTo(all, 0);
            var last = items[items.Length - 1];
            for (int i = items.Length; i < all.Length; i++)
            {
                var old = last.transform.parent.Find(name + (i + 1));
                var go = old != null ? old.gameObject : Object.Instantiate(last.gameObject, last.transform.parent, false);
                go.name = name + (i + 1);
                go.transform.SetSiblingIndex(all[i - 1].transform.GetSiblingIndex() + 1);
                all[i] = go.GetComponent<T>();
                if (all[i] == null) return null;
            }
            return all;
        }

        // A column's height as its own vertical layout wants it (the game's row stretched every column to the tallest).
        public static float PreferredHeight(RectTransform rt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            return LayoutUtility.GetPreferredHeight(rt);
        }

        // Result screens lay their columns out in one row sized for 4. Lay them out as rows of up to 5 instead, each row
        // centred, in the same width and scaled down like the old 2 x 5 grid (x0.8). A LayoutElement on the row's
        // container reports the new size, so the popup grows to fit. heads (death match stamps) sit headY above the
        // centre of their column, in headH of extra room above each row.
        public static void Rows(IList<RectTransform> cols, int count, float cellH, IList<RectTransform> heads = null, float headH = 0f, float headY = 0f)
        {
            var block = (RectTransform)cols[0].parent;
            var group = block.GetComponent<HorizontalOrVerticalLayoutGroup>();
            var pad = group != null ? group.padding : new RectOffset();
            float gap = group != null ? group.spacing : 24f;
            if (group != null) group.enabled = false;   // it only places one row; the columns are placed below

            float cellW = cols[0].rect.width;
            float areaW = 4 * cellW + 3 * gap;   // the game's 4-column row
            int perRow = count <= 5 ? count : (count + 1) / 2;
            int rows = (count + perRow - 1) / perRow;
            float k = Mathf.Min(1f, areaW / (perRow * cellW + (perRow - 1) * gap), 1.6f / rows);
            float rowH = headH + cellH;

            var size = block.GetComponent<LayoutElement>();
            if (size == null) size = block.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = pad.horizontal + areaW;
            size.minHeight = size.preferredHeight = pad.vertical + (rows * rowH + (rows - 1) * gap) * k;

            for (int i = 0; i < count; i++)
            {
                int r = i / perRow, c = i % perRow, inRow = Mathf.Min(perRow, count - r * perRow);
                var pos = new Vector2((c - (inRow - 1) / 2f) * (cellW + gap) * k, -pad.top - (r * (rowH + gap) + headH + cellH / 2f) * k);
                Place(cols[i], pos, k, new Vector2(cellW, cellH));
                if (heads != null && heads[i] != null) Place(heads[i], pos + new Vector2(0f, headY * k), k, heads[i].sizeDelta);
            }
        }

        static void Place(RectTransform rt, Vector2 pos, float k, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.localScale = new Vector3(k, k, 1f);
            rt.anchoredPosition = pos;
        }

        // The result popups are almost screen-high already; if the extra rows make one taller than the screen,
        // shrink the whole popup (its open animation only fades and slides the frame around it).
        public static void FitPopup(Transform inside)
        {
            var popup = inside.GetComponentInParent<ContentSizeFitter>()?.transform as RectTransform;
            var screen = popup != null ? popup.parent as RectTransform : null;
            if (screen == null) { WarnOnce("Result popup not found, can't fit it to the screen"); return; }
            LayoutRebuilder.ForceRebuildLayoutImmediate(popup);
            float room = screen.rect.height * 0.96f, h = popup.rect.height;
            if (h > room) popup.localScale = new Vector3(room / h, room / h, 1f);
        }
    }
}
