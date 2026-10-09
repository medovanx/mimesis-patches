// MIMESIS BiggerLobby - "installed patch" chip on the main menu
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
// Shared design: every patch adds its own chip to one "PatchChips" row in the bottom-left of the
// main menu. Clicking a chip opens the GitHub repo.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BiggerLobby
{
    sealed class PatchChip : MonoBehaviour
    {
        const string RepoUrl = "https://github.com/medovanx/mimesis-patches";
        string _label;
        static int _hovered;

        // The game uses the normal Windows cursor; show the system hand while a chip is hovered.
        [DllImport("user32.dll")] static extern IntPtr LoadCursor(IntPtr instance, int name);
        [DllImport("user32.dll")] static extern IntPtr SetCursor(IntPtr cursor);
        const int IDC_HAND = 32649;
        static IntPtr _hand;

        void LateUpdate()
        {
            if (_hovered <= 0) return;
            if (_hand == IntPtr.Zero) _hand = LoadCursor(IntPtr.Zero, IDC_HAND);
            SetCursor(_hand);   // every frame, since Windows resets it on mouse move
        }

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
                // Spawned UI prefabs are usually named "<prefab>(Clone)".
                var menu = GameObject.Find("UIPrefab_MainMenu(Clone)") ?? GameObject.Find("UIPrefab_MainMenu");
                var version = menu != null ? menu.transform.Find("VersionTextRect/versionText")?.GetComponent<TMP_Text>() : null;
                if (version != null && version.canvas != null)
                {
                    var row = Row(version.canvas.rootCanvas.transform);
                    AddChip(row, version);
                    row.gameObject.SetActive(menu.activeInHierarchy);
                    if (!menu.activeInHierarchy) _hovered = 0;
                }
                yield return wait;
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
            row.sizeDelta = new Vector2(800f, 30f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row;
        }

        void AddChip(RectTransform row, TMP_Text version)
        {
            var name = "Chip_" + _label;
            if (row.Find(name) != null) return;
            Debug.Log($"[{nameof(BiggerLobby)}] Main menu chip added: {_label}");

            var chip = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            chip.transform.SetParent(row, false);
            var bg = chip.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            var button = chip.GetComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => Application.OpenURL(RepoUrl));
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
