// MIMESIS Patches - one "Patches" chip on the main menu that opens a window listing every installed patch
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Compiled into every runtime, but each patch is its own DLL, so they share state through the AppDomain
// using only BCL/Unity types: every patch adds an entry (a Dictionary) to one list, and the first patch to
// start becomes the "owner" that draws the chip and the window. Each entry's page is built by its own patch.
//
// The window: left sidebar = installed patches (orange dot = update available); right = the selected
// patch's version, status, update link and options. Updates are checked once per launch on GitHub releases.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MimesisPatches
{
    sealed class PatchMenu : MonoBehaviour
    {
        public const string Repo = "medovanx/mimesis-patches";
        public const string RepoUrl = "https://github.com/" + Repo;
        const string RegistryKey = "medovanx.patches.registry", OwnerKey = "medovanx.patches.owner";

        static readonly Color ChipColor = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color UpdateColor = new Color(0.85f, 0.45f, 0.05f, 0.85f);

        /// <param name="build">Fills the patch's options page (parent has a vertical layout). Optional.</param>
        public static void Register(string name, Version version, Func<string> status = null, Action<RectTransform, TMP_FontAsset> build = null)
        {
            var list = AppDomain.CurrentDomain.GetData(RegistryKey) as List<Dictionary<string, object>>;
            if (list == null) AppDomain.CurrentDomain.SetData(RegistryKey, list = new List<Dictionary<string, object>>());
            list.RemoveAll(e => (string)e["name"] == name);
            list.Add(new Dictionary<string, object>
            {
                ["name"] = name,
                ["version"] = new Version(version.Major, version.Minor, Math.Max(version.Build, 0)),
                ["status"] = status,
                // Clear this assembly's refreshers before building its page, so only the visible page refreshes.
                ["build"] = build == null ? null : (Action<RectTransform, TMP_FontAsset>)((rt, font) => { PatchUi.Refreshers.Clear(); build(rt, font); PatchUi.RefreshAll(); }),
            });
            list.Sort((a, b) => string.CompareOrdinal((string)a["name"], (string)b["name"]));

            if (AppDomain.CurrentDomain.GetData(OwnerKey) == null)
            {
                AppDomain.CurrentDomain.SetData(OwnerKey, name);
                var go = new GameObject("MedovanxPatchMenu");
                DontDestroyOnLoad(go);
                go.AddComponent<PatchMenu>();
            }
        }

        static List<Dictionary<string, object>> Entries =>
            AppDomain.CurrentDomain.GetData(RegistryKey) as List<Dictionary<string, object>> ?? new List<Dictionary<string, object>>();

        static Version Latest(Dictionary<string, object> e) => e.TryGetValue("latest", out var v) ? v as Version : null;
        static bool HasUpdate(Dictionary<string, object> e) => Latest(e) != null && Latest(e) > (Version)e["version"];

        RectTransform _row;
        TMP_Text _chipText;
        Image _chipBg;
        TMP_FontAsset _font;

        IEnumerator Start()
        {
            StartCoroutine(CheckForUpdates());
            var wait = new WaitForSeconds(0.5f);
            while (true)
            {
                // Spawned UI prefabs are usually named "<prefab>(Clone)".
                var menu = GameObject.Find("UIPrefab_MainMenu(Clone)") ?? GameObject.Find("UIPrefab_MainMenu");
                var version = menu != null ? menu.transform.Find("VersionTextRect/versionText")?.GetComponent<TMP_Text>() : null;
                if (version == null)
                {
                    if (_row != null) _row.gameObject.SetActive(false);   // menu gone: hide
                    _hovered = 0;
                }
                else if (version.canvas != null)
                {
                    _font = version.font;
                    if (_row == null) BuildChip(version.canvas.rootCanvas.transform, version);
                    var entries = Entries;
                    int updates = entries.Count(HasUpdate);
                    _chipText.text = $"Patches ({entries.Count})" + (updates > 0 ? $" · {updates} update{(updates > 1 ? "s" : "")}" : "");
                    _chipBg.color = updates > 0 ? UpdateColor : ChipColor;
                    // The main menu object stays active behind game scenes, so also require that no game scene is loaded.
                    bool onMenu = menu.activeInHierarchy && (Hub.s == null || Hub.s.pdata == null || Hub.s.pdata.main == null);
                    _row.gameObject.SetActive(onMenu);
                    if (!onMenu) { _hovered = 0; Close(); }
                }
                yield return wait;
            }
        }

        IEnumerator CheckForUpdates()
        {
            using (var req = UnityWebRequest.Get($"https://api.github.com/repos/{Repo}/releases?per_page=100"))
            {
                req.SetRequestHeader("User-Agent", "mimesis-patches");
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) yield break;   // offline etc.: no update hints
                foreach (Match m in Regex.Matches(req.downloadHandler.text, "\"tag_name\"\\s*:\\s*\"([A-Za-z]+)-v(\\d+\\.\\d+\\.\\d+)\""))
                {
                    var entry = Entries.FirstOrDefault(e => (string)e["name"] == m.Groups[1].Value);
                    if (entry == null) continue;
                    var v = new Version(m.Groups[2].Value);
                    if (Latest(entry) == null || v > Latest(entry)) entry["latest"] = v;
                }
                foreach (var e in Entries.Where(HasUpdate))
                    Debug.Log($"[{e["name"]}] Update available: {((Version)e["version"]).ToString(3)} -> {Latest(e).ToString(3)}");
            }
        }

        // ---------------- Chip (bottom-left of the main menu) ----------------

        void BuildChip(Transform canvas, TMP_Text version)
        {
            _row = new GameObject("PatchChips", typeof(RectTransform)).GetComponent<RectTransform>();
            _row.SetParent(canvas, false);
            _row.SetAsLastSibling();
            _row.anchorMin = _row.anchorMax = _row.pivot = Vector2.zero;
            _row.anchoredPosition = new Vector2(24f, 20f);
            _row.sizeDelta = new Vector2(600f, 30f);
            var layout = _row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var chip = new GameObject("Chip_Patches", typeof(RectTransform), typeof(Image), typeof(Button));
            chip.transform.SetParent(_row, false);
            _chipBg = chip.GetComponent<Image>();
            var button = chip.GetComponent<Button>();
            button.targetGraphic = _chipBg;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            button.colors = colors;
            button.onClick.AddListener(Open);
            Hover(chip);
            var pad = chip.AddComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(12, 12, 4, 4);
            pad.childControlWidth = pad.childControlHeight = true;
            pad.childForceExpandWidth = pad.childForceExpandHeight = false;

            _chipText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _chipText.transform.SetParent(chip.transform, false);
            _chipText.font = version.font;
            _chipText.fontSize = version.fontSize * 0.85f;
            _chipText.color = PatchUi.Text;
            _chipText.raycastTarget = false;
            Debug.Log("[Patches] Main menu chip added");
        }

        // ---------------- Window ----------------

        GameObject _window;
        RectTransform _sidebar, _content;
        string _selected;

        void Open()
        {
            if (_window == null) BuildWindow();
            _window.SetActive(true);
            var entries = Entries;
            if (_selected == null || entries.All(e => (string)e["name"] != _selected)) _selected = entries.FirstOrDefault()?["name"] as string;
            BuildSidebar();
            Select(_selected);
        }

        void Close() { if (_window != null) _window.SetActive(false); }

        void BuildWindow()
        {
            var canvas = new GameObject("PatchesWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _window = canvas.gameObject;

            // Dimmed backdrop; clicking it closes the window.
            var dim = PatchUi.Box(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            PatchUi.Stretch(dim);
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Close);

            var panel = PatchUi.Box(canvas.transform, "Panel", new Color(0.06f, 0.06f, 0.06f, 0.96f));
            panel.sizeDelta = new Vector2(1100f, 640f);

            // Title bar with close button.
            var title = PatchUi.Label(panel, _font, "Patches", 30f);
            Destroy(title.GetComponent<LayoutElement>());
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.offsetMin = new Vector2(28f, -64f);
            title.rectTransform.offsetMax = new Vector2(-80f, -16f);
            var close = PatchUi.Button(panel, _font, "X", Close, 44f, 44f);   // plain X: the game font has no ✕ glyph
            Destroy(close.GetComponent<LayoutElement>());
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-16f, -16f);
            crt.sizeDelta = new Vector2(44f, 44f);

            // Author footer along the bottom; clicking it opens the repo.
            var footer = PatchUi.Label(panel, _font,
                "Made by <b>Mohamed Darwesh</b> (@medovanx)  ·  <u>github.com/medovanx/mimesis-patches</u>", 18f, PatchUi.Dim);
            Destroy(footer.GetComponent<LayoutElement>());
            footer.alignment = TextAlignmentOptions.Center;
            footer.raycastTarget = true;
            var frt = footer.rectTransform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(1f, 0f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.offsetMin = new Vector2(16f, 12f);
            frt.offsetMax = new Vector2(-16f, 44f);
            footer.gameObject.AddComponent<Button>().onClick.AddListener(() => Application.OpenURL(RepoUrl));
            Hover(footer.gameObject);

            // Left sidebar.
            _sidebar = PatchUi.Box(panel, "Sidebar", new Color(1f, 1f, 1f, 0.04f));
            _sidebar.anchorMin = new Vector2(0f, 0f);
            _sidebar.anchorMax = new Vector2(0f, 1f);
            _sidebar.pivot = new Vector2(0f, 0.5f);
            _sidebar.offsetMin = new Vector2(16f, 56f);
            _sidebar.offsetMax = new Vector2(296f, -80f);
            var sideLayout = _sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            sideLayout.padding = new RectOffset(8, 8, 8, 8);
            sideLayout.spacing = 6f;
            sideLayout.childControlWidth = sideLayout.childControlHeight = true;
            sideLayout.childForceExpandHeight = false;

            // Right content.
            _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.SetParent(panel, false);
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = Vector2.one;
            _content.offsetMin = new Vector2(320f, 56f);
            _content.offsetMax = new Vector2(-24f, -80f);
            var contentLayout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 14f;
            contentLayout.childControlWidth = contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;
        }

        void BuildSidebar()
        {
            foreach (Transform c in _sidebar) Destroy(c.gameObject);
            foreach (var e in Entries)
            {
                var name = (string)e["name"];
                var label = $"{name}  <size=70%><color=#A6AAA0>v{((Version)e["version"]).ToString(3)}</color></size>" + (HasUpdate(e) ? "  <color=#E08A1E>●</color>" : "");
                var b = PatchUi.Button(_sidebar, _font, label, () => { _selected = name; Select(name); }, 260f, 48f);
                var text = b.GetComponentInChildren<TMP_Text>();
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.margin = new Vector4(14f, 0f, 8f, 0f);
                b.GetComponent<Image>().color = name == _selected ? PatchUi.On : PatchUi.Off;
            }
        }

        void Select(string name)
        {
            BuildSidebar();
            foreach (Transform c in _content) Destroy(c.gameObject);
            var e = Entries.FirstOrDefault(x => (string)x["name"] == name);
            if (e == null) return;

            PatchUi.Label(_content, _font, $"{name}  <size=70%><color=#A6AAA0>v{((Version)e["version"]).ToString(3)}</color></size>", 30f, null, 40f);
            if (e["status"] is Func<string> status)
            {
                string s = null;
                try { s = status(); } catch { }
                if (!string.IsNullOrEmpty(s)) PatchUi.Label(_content, _font, s, 18f, PatchUi.Dim, 26f);
            }
            if (HasUpdate(e))
                PatchUi.Button(PatchUi.Row(_content, 44f), _font, $"Update to {Latest(e).ToString(3)}",
                    () => Application.OpenURL($"{RepoUrl}/releases/tag/{name}-v{Latest(e).ToString(3)}"), 220f)
                    .GetComponent<Image>().color = UpdateColor;

            if (e["build"] is Action<RectTransform, TMP_FontAsset> build)
            {
                var page = new GameObject("Options", typeof(RectTransform)).GetComponent<RectTransform>();
                page.SetParent(_content, false);
                var layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.padding = new RectOffset(0, 0, 12, 0);
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                try { build(page, _font); }
                catch (Exception ex) { Debug.LogError($"[{name}] Options page failed: {ex}"); }
            }
        }

        // ---------------- Hand cursor while hovering the chip ----------------

        static int _hovered;
        static IntPtr _hand;
        const int IDC_HAND = 32649;
        [DllImport("user32.dll")] static extern IntPtr LoadCursor(IntPtr instance, int name);
        [DllImport("user32.dll")] static extern IntPtr SetCursor(IntPtr cursor);

        static void Hover(GameObject target)
        {
            var hover = target.AddComponent<EventTrigger>();
            void On(EventTriggerType type, int delta)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ => _hovered = Mathf.Max(0, _hovered + delta));
                hover.triggers.Add(entry);
            }
            On(EventTriggerType.PointerEnter, 1);
            On(EventTriggerType.PointerExit, -1);
        }

        void LateUpdate()
        {
            if (_hovered <= 0) return;
            if (_hand == IntPtr.Zero) _hand = LoadCursor(IntPtr.Zero, IDC_HAND);
            SetCursor(_hand);   // every frame, since Windows resets it on mouse move
        }
    }
}
