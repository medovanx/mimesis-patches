// MIMESIS Patches - shared "installed patch" chip on the main menu, with update check
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Compiled into every patch's runtime (see each Runtime .csproj). Every patch adds its own chip to one
// "PatchChips" row in the bottom-left of the main menu. Each chip checks GitHub releases once per launch:
// if a newer "<Patch>-vX.Y.Z" release exists, the chip turns orange and clicking it opens that release.
// Otherwise clicking opens the repo, or runs the patch's own action if it registered one.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MimesisPatches
{
    sealed class PatchChip : MonoBehaviour
    {
        public const string Repo = "medovanx/mimesis-patches";
        public const string RepoUrl = "https://github.com/" + Repo;

        static readonly Color Normal = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color UpdateColor = new Color(0.85f, 0.45f, 0.05f, 0.85f);
        static readonly Color TextColor = new Color(0.85f, 0.95f, 0.85f, 1f);

        string _patch;
        Version _version;
        Func<string> _status;   // optional extra text, e.g. "Explored"
        Action _onClick;        // optional action instead of opening the repo
        Version _latest;
        TMP_Text _text;
        Image _bg;

        /// <param name="status">Extra text shown after the version (refreshed while the menu is open).</param>
        /// <param name="onClick">Runs on click when no update is available; defaults to opening the repo.</param>
        public static void Register(string patch, Version version, Func<string> status = null, Action onClick = null)
        {
            var go = new GameObject("PatchChip_" + patch);
            DontDestroyOnLoad(go);
            var chip = go.AddComponent<PatchChip>();
            chip._patch = patch;
            chip._version = new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
            chip._status = status;
            chip._onClick = onClick;
        }

        string Label
        {
            get
            {
                var label = $"{_patch} v{_version.ToString(3)}";
                if (_latest != null) return $"{label} → {_latest.ToString(3)} available";
                var status = _status?.Invoke();
                return string.IsNullOrEmpty(status) ? label : $"{label} · {status}";
            }
        }

        void Click()
        {
            if (_latest != null) Application.OpenURL($"{RepoUrl}/releases/tag/{_patch}-v{_latest.ToString(3)}");
            else if (_onClick != null) _onClick();
            else Application.OpenURL(RepoUrl);
        }

        IEnumerator Start()
        {
            StartCoroutine(CheckForUpdate());
            var wait = new WaitForSeconds(0.5f);
            while (true)
            {
                // Spawned UI prefabs are usually named "<prefab>(Clone)".
                var menu = GameObject.Find("UIPrefab_MainMenu(Clone)") ?? GameObject.Find("UIPrefab_MainMenu");
                var version = menu != null ? menu.transform.Find("VersionTextRect/versionText")?.GetComponent<TMP_Text>() : null;
                if (version != null && version.canvas != null)
                {
                    var row = Row(version.canvas.rootCanvas.transform);
                    if (_text == null) Build(row, version);
                    _text.text = Label;
                    _bg.color = _latest != null ? UpdateColor : Normal;
                    row.gameObject.SetActive(menu.activeInHierarchy);
                    if (!menu.activeInHierarchy) _hovered = 0;
                }
                yield return wait;
            }
        }

        IEnumerator CheckForUpdate()
        {
            using (var req = UnityWebRequest.Get($"https://api.github.com/repos/{Repo}/releases?per_page=50"))
            {
                req.SetRequestHeader("User-Agent", "mimesis-patches");
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) yield break;   // offline etc.: just no update hint
                foreach (Match m in Regex.Matches(req.downloadHandler.text, $"\"tag_name\"\\s*:\\s*\"{Regex.Escape(_patch)}-v(\\d+\\.\\d+\\.\\d+)\""))
                {
                    var v = new Version(m.Groups[1].Value);
                    if (v > _version && (_latest == null || v > _latest)) _latest = v;
                }
                if (_latest != null) Debug.Log($"[{_patch}] Update available: {_version.ToString(3)} -> {_latest.ToString(3)}");
            }
        }

        // The row lives on the root canvas (bottom-left corner); it's hidden whenever the main menu isn't shown.
        static RectTransform Row(Transform canvas)
        {
            var row = canvas.Find("PatchChips") as RectTransform;
            if (row != null) return row;
            row = new GameObject("PatchChips", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(canvas, false);
            row.SetAsLastSibling();
            row.anchorMin = row.anchorMax = row.pivot = Vector2.zero;
            row.anchoredPosition = new Vector2(24f, 20f);
            row.sizeDelta = new Vector2(1400f, 30f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row;
        }

        void Build(RectTransform row, TMP_Text version)
        {
            var name = "Chip_" + _patch;
            var existing = row.Find(name);
            if (existing != null) Destroy(existing.gameObject);
            Debug.Log($"[{_patch}] Main menu chip added");

            var chip = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            chip.transform.SetParent(row, false);
            _bg = chip.GetComponent<Image>();
            var button = chip.GetComponent<Button>();
            button.targetGraphic = _bg;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            button.colors = colors;
            button.onClick.AddListener(Click);

            var hover = chip.AddComponent<EventTrigger>();
            void On(EventTriggerType type, int delta)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ => _hovered = Mathf.Max(0, _hovered + delta));
                hover.triggers.Add(entry);
            }
            On(EventTriggerType.PointerEnter, 1);
            On(EventTriggerType.PointerExit, -1);

            var pad = chip.AddComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(10, 10, 3, 3);
            pad.childControlWidth = pad.childControlHeight = true;
            pad.childForceExpandWidth = pad.childForceExpandHeight = false;

            _text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _text.transform.SetParent(chip.transform, false);
            _text.font = version.font;
            _text.fontSize = version.fontSize * 0.8f;
            _text.color = TextColor;
            _text.raycastTarget = false;
        }

        // The game uses the normal Windows cursor; show the system hand while a chip is hovered.
        static int _hovered;
        static IntPtr _hand;
        const int IDC_HAND = 32649;
        [DllImport("user32.dll")] static extern IntPtr LoadCursor(IntPtr instance, int name);
        [DllImport("user32.dll")] static extern IntPtr SetCursor(IntPtr cursor);

        void LateUpdate()
        {
            if (_hovered <= 0) return;
            if (_hand == IntPtr.Zero) _hand = LoadCursor(IntPtr.Zero, IDC_HAND);
            SetCursor(_hand);   // every frame, since Windows resets it on mouse move
        }
    }
}
