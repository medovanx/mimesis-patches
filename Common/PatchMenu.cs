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
        const string SizeKey = "medovanx.patches.window";
        static readonly Vector2 MinSize = new Vector2(900f, 560f);
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
                    _chipText.text = $"Patches ({entries.Count})" + (updates > 0 ? $", {updates} update{(updates > 1 ? "s" : "")}" : "");
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

        // ---------------- In-game update ----------------
        // Downloads the latest installer and runs it: it closes the game, installs the updates and starts the game again.

        const string InstallerUrl = RepoUrl + "/releases/latest/download/MimesisPatchesInstaller.exe";
        bool _updating;

        RectTransform _confirm;

        static string Eta(double seconds) =>
            seconds >= 3600 ? $"{(int)(seconds / 3600)}h {(int)(seconds % 3600 / 60)}m" :
            seconds >= 60 ? $"{(int)(seconds / 60)}m {(int)(seconds % 60)}s" : $"{(int)Math.Ceiling(seconds)}s";

        // A popup over the whole window: dims everything behind it and blocks clicks until closed.
        RectTransform Dialog()
        {
            if (_confirm != null) Destroy(_confirm.gameObject);
            _confirm = PatchUi.Box(_window.transform, "UpdateDialog", new Color(0f, 0f, 0f, 0.6f));
            PatchUi.Stretch(_confirm);
            var box = PatchUi.Box(_confirm, "Box", new Color(0.09f, 0.09f, 0.09f, 0.98f));
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(32, 32, 28, 28);
            v.spacing = 18f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            var fit = box.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            box.gameObject.AddComponent<LayoutElement>();
            box.sizeDelta = new Vector2(640f, 0f);
            return box;
        }

        void CloseDialog() { if (_confirm != null) Destroy(_confirm.gameObject); _confirm = null; }

        // Asks first: updating closes the game.
        void StartUpdate(string[] names)
        {
            if (_updating) return;
            var box = Dialog();
            PatchUi.Label(box, _font, names.Length == 1 ? $"Update {names[0]}?" : $"Update {names.Length} patches?", 28f, null, 36f);
            PatchUi.Label(box, _font, "The game will close, install the update and start again.", 20f, PatchUi.Dim);
            var row = PatchUi.Row(box, 44f);
            PatchUi.Button(row, _font, "Update now", () =>
            {
                _updating = true;
                StartCoroutine(RunUpdate(names));
            }, 200f).GetComponent<Image>().color = UpdateColor;
            PatchUi.Button(row, _font, "Not now", CloseDialog, 160f);
        }

        void Failed(string message)
        {
            var box = Dialog();
            PatchUi.Label(box, _font, "Update failed", 28f, null, 36f);
            PatchUi.Label(box, _font, message, 20f, PatchUi.Dim);
            PatchUi.Button(PatchUi.Row(box, 44f), _font, "OK", CloseDialog, 160f);
            _updating = false;
        }

        IEnumerator RunUpdate(string[] names)
        {
            var box = Dialog();
            PatchUi.Label(box, _font, names.Length == 1 ? $"Updating {names[0]}" : $"Updating {names.Length} patches", 28f, null, 36f);
            var status = PatchUi.Label(box, _font, "Starting the download...", 20f, PatchUi.Dim);
            var track = PatchUi.Box(box, "Progress", PatchUi.Off);
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 8f;
            var fill = PatchUi.Box(track, "Fill", UpdateColor);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            var exe = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MimesisPatchesInstaller.exe");
            using (var req = new UnityWebRequest(InstallerUrl, UnityWebRequest.kHttpVerbGET, new DownloadHandlerFile(exe), null))
            {
                req.timeout = 1800;
                req.SendWebRequest();
                float started = Time.realtimeSinceStartup, lastTime = started;
                ulong lastBytes = 0;
                double speed = 0;   // bytes per second, smoothed
                while (!req.isDone)
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                    float now = Time.realtimeSinceStartup;
                    ulong got = req.downloadedBytes;
                    float p = req.downloadProgress;
                    double instant = (got - lastBytes) / Math.Max(0.001, now - lastTime);
                    speed = speed <= 0 ? instant : speed * 0.8 + instant * 0.2;
                    lastBytes = got; lastTime = now;
                    double total = p > 0.001f ? got / p : 0;
                    fill.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
                    string text = $"Downloading the update: {got / 1048576.0:0.0}" + (total > 0 ? $" / {total / 1048576.0:0.0} MB" : " MB");
                    if (speed > 0) text += $"  |  {speed / 1048576.0:0.00} MB/s";
                    if (speed > 0 && total > got) text += $"  |  {Eta((total - got) / speed)} left";
                    status.text = text;
                }
                status.text = "Downloaded. Closing the game to install...";
                fill.anchorMax = Vector2.one;
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Failed("Couldn't download the update. Check your connection, or run MimesisPatchesInstaller.exe.");
                    yield break;
                }
            }
            var dll = System.IO.Path.Combine(Application.dataPath, "Managed", "Assembly-CSharp.dll");
            try
            {
                System.Diagnostics.Process.Start(exe, $"install:{string.Join(",", names)} \"{dll}\" --relaunch");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Patches] Couldn't start the installer: " + ex);
                Failed("Couldn't start the installer. Run MimesisPatchesInstaller.exe yourself.");
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
        Button _updateAll;
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
            panel.sizeDelta = new Vector2(
                Mathf.Clamp(PlayerPrefs.GetFloat(SizeKey + ".w", 1100f), MinSize.x, 1900f),
                Mathf.Clamp(PlayerPrefs.GetFloat(SizeKey + ".h", 760f), MinSize.y, 1060f));
            ResizeGrip.Add(panel);

            // Title bar with close button.
            var title = PatchUi.Label(panel, _font, "Patches", 30f);
            Destroy(title.GetComponent<LayoutElement>());
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.offsetMin = new Vector2(28f, -64f);
            title.rectTransform.offsetMax = new Vector2(-80f, -16f);
            // Drawn as two crossed bars (the game font has no ✕ glyph) instead of a plain "X" letter.
            var close = PatchUi.Box(panel, "Close", PatchUi.Off);
            close.anchorMin = close.anchorMax = close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-16f, -16f);
            close.sizeDelta = new Vector2(44f, 44f);
            close.GetComponent<Image>().color = PatchUi.Off;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close.GetComponent<Image>();
            closeButton.onClick.AddListener(Close);
            Hover(close.gameObject);
            foreach (var angle in new[] { 45f, -45f })
            {
                var bar = PatchUi.Box(close, "Bar", PatchUi.Text);
                bar.sizeDelta = new Vector2(22f, 3f);
                bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
                bar.anchoredPosition = Vector2.zero;
                bar.localRotation = Quaternion.Euler(0f, 0f, angle);
                bar.GetComponent<Image>().raycastTarget = false;
            }

            // "Update all" in the title bar, left of the close button; shown when any patch has an update.
            _updateAll = PatchUi.Button(panel, _font, "Update all", () =>
                StartUpdate(Entries.Where(HasUpdate).Select(x => (string)x["name"]).ToArray()), 200f);
            _updateAll.GetComponent<Image>().color = UpdateColor;
            var ua = (RectTransform)_updateAll.transform;
            Destroy(ua.GetComponent<LayoutElement>());
            ua.anchorMin = ua.anchorMax = ua.pivot = new Vector2(1f, 1f);
            ua.anchoredPosition = new Vector2(-72f, -16f);
            ua.sizeDelta = new Vector2(200f, 44f);

            // Author footer along the bottom; clicking it opens the repo.
            var footer = PatchUi.Label(panel, _font,
                "Made by <b>Mohamed Darwesh</b> (@medovanx)   |   <u>github.com/medovanx/mimesis-patches</u>", 18f, PatchUi.Dim);
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
            int updates = Entries.Count(HasUpdate);
            _updateAll.gameObject.SetActive(updates > 0);
            _updateAll.GetComponentInChildren<TMP_Text>().text = updates > 1 ? $"Update all ({updates})" : "Update";
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
            {
                var row = PatchUi.Row(_content, 44f);
                PatchUi.Button(row, _font, $"Update to {Latest(e).ToString(3)}", () => StartUpdate(new[] { name }), 220f)
                    .GetComponent<Image>().color = UpdateColor;
                PatchUi.Button(row, _font, "What's new", () => Application.OpenURL($"{RepoUrl}/blob/main/{name}/CHANGELOG.md"), 160f);
            }

            if (e["build"] is Action<RectTransform, TMP_FontAsset> build)
            {
                var page = ScrollArea(_content);
                var layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 14f;
                layout.padding = new RectOffset(0, 24, 8, 16);
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                page.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                try { build(page, _font); }
                catch (Exception ex) { Debug.LogError($"[{name}] Options page failed: {ex}"); }
            }
        }

        // Fills the rest of the content column and scrolls (mouse wheel or the bar on the right) when the
        // options don't fit. Returns the scrolled content, which grows to fit its children.
        RectTransform ScrollArea(Transform parent)
        {
            var root = new GameObject("Scroll", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(root, false);
            PatchUi.Stretch(viewport);
            // A transparent image so the wheel scrolls anywhere over the page, not just over controls.
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var content = new GameObject("Options", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;

            var bar = PatchUi.Box(root, "Scrollbar", new Color(1f, 1f, 1f, 0.05f));
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(8f, 0f);
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(bar, false);
            PatchUi.Stretch(handleArea);
            var handle = PatchUi.Box(handleArea, "Handle", new Color(1f, 1f, 1f, 0.3f));
            handle.offsetMin = handle.offsetMax = Vector2.zero;
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;   // never scroll past the last option
            scroll.scrollSensitivity = 30f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
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

    /// <summary>Bottom-right corner grip: drag to resize the window. The top-left corner stays put, and the size is remembered.</summary>
    sealed class ResizeGrip : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        RectTransform _panel;

        public static void Add(RectTransform panel)
        {
            var grip = PatchUi.Box(panel, "ResizeGrip", new Color(1f, 1f, 1f, 0.001f));
            grip.anchorMin = grip.anchorMax = grip.pivot = new Vector2(1f, 0f);
            grip.anchoredPosition = Vector2.zero;
            grip.sizeDelta = new Vector2(28f, 28f);
            // Three short diagonal lines, like a window corner grip.
            for (int i = 0; i < 3; i++)
            {
                var line = PatchUi.Box(grip, "Line", new Color(1f, 1f, 1f, 0.35f));
                float len = 6f + 6f * i;
                line.anchorMin = line.anchorMax = new Vector2(1f, 0f);
                line.pivot = new Vector2(0.5f, 0.5f);
                line.sizeDelta = new Vector2(len * 1.414f, 2f);
                line.anchoredPosition = new Vector2(-4f - len / 2f, 4f + len / 2f);
                line.localRotation = Quaternion.Euler(0f, 0f, 45f);
                line.GetComponent<Image>().raycastTarget = false;
            }
            grip.gameObject.AddComponent<ResizeGrip>()._panel = panel;
        }

        public void OnDrag(PointerEventData e)
        {
            var canvas = _panel.GetComponentInParent<Canvas>();
            var d = e.delta / (canvas != null ? canvas.scaleFactor : 1f);
            var old = _panel.sizeDelta;
            var size = new Vector2(Mathf.Clamp(old.x + d.x, 900f, 1900f), Mathf.Clamp(old.y - d.y, 560f, 1060f));
            var change = size - old;
            _panel.sizeDelta = size;
            // The panel is centred, so move it by half the change to keep its top-left corner in place.
            _panel.anchoredPosition += new Vector2(change.x / 2f, -change.y / 2f);
        }

        public void OnEndDrag(PointerEventData e)
        {
            PlayerPrefs.SetFloat("medovanx.patches.window.w", _panel.sizeDelta.x);
            PlayerPrefs.SetFloat("medovanx.patches.window.h", _panel.sizeDelta.y);
            PlayerPrefs.Save();
        }
    }
}
