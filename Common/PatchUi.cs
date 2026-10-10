// MIMESIS Patches - small UGUI helpers for building patch option pages
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Compiled into every runtime. Each patch fills its own page in the Patches window with these.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPatches
{
    static class PatchUi
    {
        public static readonly Color Text = new Color(0.9f, 0.92f, 0.88f, 1f);
        public static readonly Color Dim = new Color(0.65f, 0.67f, 0.63f, 1f);
        public static readonly Color Off = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color On = new Color(0.85f, 0.55f, 0.1f, 0.95f);
        public static readonly Color Accent = new Color(0.85f, 0.45f, 0.05f, 1f);

        /// <summary>Refresh callbacks of the page being built; the window runs them after any click.</summary>
        public static readonly List<Action> Refreshers = new List<Action>();

        public static void RefreshAll() { foreach (var r in Refreshers) r(); }

        public static TMP_Text Label(Transform parent, TMP_FontAsset font, string text, float size = 22f, Color? color = null, float height = 0f)
        {
            var t = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(parent, false);
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color ?? Text;
            t.text = text;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            var el = t.gameObject.AddComponent<LayoutElement>();
            if (height > 0f) el.preferredHeight = height;
            return t;
        }

        public static RectTransform Box(Transform parent, string name, Color color)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.GetComponent<Image>().color = color;
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static Button Button(Transform parent, TMP_FontAsset font, string label, Action onClick, float width = 160f, float height = 44f)
        {
            var rt = Box(parent, label, Off);
            var el = rt.gameObject.AddComponent<LayoutElement>();
            el.preferredWidth = width;
            el.preferredHeight = height;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(() => { onClick(); RefreshAll(); });
            var text = Label(rt, font, label, 20f);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
            UnityEngine.Object.Destroy(text.GetComponent<LayoutElement>());
            return button;
        }

        public static RectTransform Row(Transform parent, float height = 48f)
        {
            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        /// <summary>A titled row of buttons; the selected one(s) are highlighted.</summary>
        public static void Options(Transform parent, TMP_FontAsset font, string title, params (string label, Func<bool> selected, Action select)[] options) =>
            Options(parent, font, title, 160f, options);

        public static void Options(Transform parent, TMP_FontAsset font, string title, float width, params (string label, Func<bool> selected, Action select)[] options)
        {
            var row = Row(parent);
            Label(row, font, title, 22f).GetComponent<LayoutElement>().preferredWidth = 190f;
            foreach (var (label, selected, select) in options)
            {
                var img = Button(row, font, label, select, width).GetComponent<Image>();
                Refreshers.Add(() => img.color = selected() ? On : Off);
            }
        }

        public static void OnOff(Transform parent, TMP_FontAsset font, string title, Func<bool> get, Action<bool> set) =>
            Options(parent, font, title, ("On", get, () => set(true)), ("Off", () => !get(), () => set(false)));

        /// <summary>A titled whole-number field. Applies when editing ends; empty/invalid falls back to the default.</summary>
        public static void Number(Transform parent, TMP_FontAsset font, string title, Func<int> get, Action<int> set, int fallback)
        {
            var row = Row(parent);
            Label(row, font, title, 22f).GetComponent<LayoutElement>().preferredWidth = 190f;
            var box = Box(row, "Field", new Color(1f, 1f, 1f, 0.08f));
            var el = box.gameObject.AddComponent<LayoutElement>();
            el.preferredWidth = 200f;
            el.preferredHeight = 44f;

            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            area.SetParent(box, false);
            Stretch(area);
            area.offsetMin = new Vector2(12f, 4f);
            area.offsetMax = new Vector2(-12f, -4f);
            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(area, false);
            Stretch(text.rectTransform);
            if (font != null) text.font = font;
            text.fontSize = 22f;
            text.color = Text;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var input = box.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 7;
            input.SetTextWithoutNotify(get().ToString());
            input.onEndEdit.AddListener(s =>
            {
                set(int.TryParse(s, out var v) && v >= 0 ? v : fallback);
                RefreshAll();
            });
            Refreshers.Add(() => { if (!input.isFocused) input.SetTextWithoutNotify(get().ToString()); });
        }

        /// <summary>A titled whole-number slider with its value shown next to it. Applies while dragging.</summary>
        /// <summary>A heading with a thin line under it, to group related options.</summary>
        public static void Section(Transform parent, TMP_FontAsset font, string title)
        {
            var box = new GameObject("Section", typeof(RectTransform)).GetComponent<RectTransform>();
            box.SetParent(parent, false);
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            v.spacing = 4f;
            v.padding = new RectOffset(0, 0, 10, 2);
            Label(box, font, title.ToUpperInvariant(), 17f, Accent, 24f).characterSpacing = 6f;
            var line = Box(box, "Line", new Color(1f, 1f, 1f, 0.12f));
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
        }

        public static Slider Slider(Transform parent, TMP_FontAsset font, string title, int min, int max, Func<int> get, Action<int> set, string suffix = "", string description = null)
        {
            var row = Row(parent, description == null ? 48f : 58f);
            if (description == null)
                Label(row, font, title, 22f).GetComponent<LayoutElement>().preferredWidth = 190f;
            else
            {
                // Title with a small description under it, in the same left column.
                var col = new GameObject("Title", typeof(RectTransform)).GetComponent<RectTransform>();
                col.SetParent(row, false);
                var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandHeight = false;
                v.childAlignment = TextAnchor.MiddleLeft;
                v.spacing = 0f;
                col.gameObject.AddComponent<LayoutElement>().preferredWidth = 190f;
                Label(col, font, title, 22f, null, 28f);
                Label(col, font, description, 15f, Dim, 20f);
            }

            var root = new GameObject("Slider", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(row, false);
            var el = root.gameObject.AddComponent<LayoutElement>();
            el.preferredWidth = 360f;
            el.preferredHeight = 28f;

            var track = Box(root, "Track", Off);
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, 8f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(root, false);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-16f, 8f);
            var fill = Box(fillArea, "Fill", On);
            fill.sizeDelta = Vector2.zero;

            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(root, false);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);
            var handle = Box(handleArea, "Handle", Text);
            handle.sizeDelta = new Vector2(16f, 0f);

            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(get());

            var value = Label(row, font, get() + suffix, 22f);
            value.GetComponent<LayoutElement>().preferredWidth = 80f;
            slider.onValueChanged.AddListener(v => { set((int)v); RefreshAll(); });   // others (e.g. preset buttons) may depend on it
            Refreshers.Add(() => { slider.SetValueWithoutNotify(get()); value.text = get() + suffix; });
            return slider;
        }
    }
}
