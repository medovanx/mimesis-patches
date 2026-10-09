// MIMESIS Minimap - settings window (opened by clicking the Minimap chip on the main menu)
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minimap
{
    static class SettingsWindow
    {
        static readonly Color Panel = new Color(0.06f, 0.06f, 0.06f, 0.94f);
        static readonly Color Off = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color On = new Color(0.85f, 0.55f, 0.1f, 0.95f);
        static readonly Color Text = new Color(0.9f, 0.92f, 0.88f, 1f);

        static GameObject _window;
        static readonly List<Action> Refresh = new List<Action>();

        public static void Open()
        {
            if (_window != null) { _window.SetActive(true); Sync(); return; }
            var font = GameObject.Find("UIPrefab_MainMenu(Clone)/VersionTextRect/versionText")?.GetComponent<TMP_Text>()?.font;

            var canvas = new GameObject("MinimapSettings", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            UnityEngine.Object.DontDestroyOnLoad(canvas.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _window = canvas.gameObject;

            // Dim the screen behind; clicking it closes the window.
            var dim = Box(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.5f));
            Stretch(dim);
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Close);

            var panel = Box(canvas.transform, "Panel", Panel);
            panel.sizeDelta = new Vector2(520f, 290f);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 16f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            Label(panel, font, "Minimap settings", 30f, TextAlignmentOptions.Left);
            Row(panel, font, "Reveal",
                ("Explored", () => Minimap.CurrentReveal == Minimap.Reveal.Explored, () => Minimap.CurrentReveal = Minimap.Reveal.Explored),
                ("Full map", () => Minimap.CurrentReveal == Minimap.Reveal.Full, () => Minimap.CurrentReveal = Minimap.Reveal.Full));
            Row(panel, font, "Style",
                ("Plain", () => Minimap.CurrentStyle == Minimap.Style.Plain, () => Minimap.CurrentStyle = Minimap.Style.Plain),
                ("Graphic", () => Minimap.CurrentStyle == Minimap.Style.Graphic, () => Minimap.CurrentStyle = Minimap.Style.Graphic));

            var close = Button(panel, font, "Close", Close);
            close.GetComponent<LayoutElement>().preferredWidth = 140f;
            Sync();
        }

        static void Close() { if (_window != null) _window.SetActive(false); }

        static void Sync() { foreach (var r in Refresh) r(); }

        static void Row(RectTransform parent, TMP_FontAsset font, string title, params (string label, Func<bool> selected, Action select)[] options)
        {
            var row = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            Label(row, font, title, 24f, TextAlignmentOptions.Left).GetComponent<LayoutElement>().preferredWidth = 120f;
            foreach (var (label, selected, select) in options)
            {
                var b = Button(row, font, label, () => { select(); Sync(); });
                b.GetComponent<LayoutElement>().preferredWidth = 160f;
                var img = b.GetComponent<Image>();
                Refresh.Add(() => img.color = selected() ? On : Off);
            }
        }

        static Button Button(RectTransform parent, TMP_FontAsset font, string label, Action onClick)
        {
            var rt = Box(parent, label, Off);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(() => onClick());
            var text = Label(rt, font, label, 22f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            UnityEngine.Object.Destroy(text.GetComponent<LayoutElement>());
            return button;
        }

        static TMP_Text Label(RectTransform parent, TMP_FontAsset font, string text, float size, TextAlignmentOptions align)
        {
            var t = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(parent, false);
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = Text;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.gameObject.AddComponent<LayoutElement>();
            return t;
        }

        static RectTransform Box(Transform parent, string name, Color color)
        {
            var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.GetComponent<Image>().color = color;
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
