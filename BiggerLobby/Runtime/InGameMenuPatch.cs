// MIMESIS BiggerLobby - pause/lobby menu player list with 10 rows and empty-slot placeholders
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Row = UIPrefab_InGameMenu.PlayerUIElement;

namespace BiggerLobby
{
    [HarmonyPatch(typeof(UIPrefab_InGameMenu))]
    static class InGameMenuPatch
    {
        // Rows are 100px in a vertical layout; shrink them so 10 fit where 4 used to.
        const float RowScale = 0.62f;   // 10 rows x 62 + spacing fit the panel's 650 height
        static readonly Color PlaceholderColor = new Color(1f, 1f, 1f, 0.35f);
        static readonly Dictionary<TMP_Text, (Color, TextAlignmentOptions, Vector2)> NameStyles = new Dictionary<TMP_Text, (Color, TextAlignmentOptions, Vector2)>();
        const float SlotCenterX = -75f;   // centre of the slot background

        static readonly Dictionary<GameObject, List<GameObject>> Hidden = new Dictionary<GameObject, List<GameObject>>();
        static readonly Color SlotBackground = new Color(0f, 0f, 0f, 0.45f);
        static readonly Dictionary<TMP_Text, TMP_Text> Numbers = new Dictionary<TMP_Text, TMP_Text>();

        // OnEnable fills the list and Start wires per-row listeners, so the rows must exist before either.
        [HarmonyPrefix, HarmonyPatch("OnEnable")]
        static void AddRowsOnEnable(UIPrefab_InGameMenu __instance) => AddRows(__instance);

        [HarmonyPrefix, HarmonyPatch("Start")]
        static void AddRowsOnStart(UIPrefab_InGameMenu __instance) => AddRows(__instance);

        static void AddRows(UIPrefab_InGameMenu menu)
        {
            var rows = menu.playerUIElements;
            if (rows.Count < 2 || rows.Count >= Plugin.MaxPlayers) return;
            var parent = (RectTransform)rows[0].container.transform.parent;

            var group = parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
            LogLayout("before", parent, group);

            var template = rows[rows.Count - 1];
            while (rows.Count < Plugin.MaxPlayers)
            {
                var go = Object.Instantiate(template.container, parent);
                go.name = "Player" + (rows.Count + 1);
                go.transform.SetSiblingIndex(rows[rows.Count - 1].container.transform.GetSiblingIndex() + 1);
                Transform from = template.container.transform, to = go.transform;
                rows.Add(new Row
                {
                    container = go,
                    nickNameText = UiUtil.Map(from, to, template.nickNameText),
                    avatarButton = UiUtil.Map(from, to, template.avatarButton),
                    volumeSlider = UiUtil.Map(from, to, template.volumeSlider),
                    speakButton = UiUtil.Map(from, to, template.speakButton),
                    infoButton = UiUtil.Map(from, to, template.infoButton),
                    kickButton = UiUtil.Map(from, to, template.kickButton),
                    pingImage = UiUtil.Map(from, to, template.pingImage),
                });
            }

            // Keep the game's vertical layout, but pack rows at their scaled height instead of stretching them.
            if (group != null)
            {
                group.childForceExpandHeight = false;
                group.childControlHeight = false;
                group.childScaleHeight = true;
                group.spacing = 2f;   // was 50, sized for 4 rows
            }
            for (int i = 0; i < rows.Count; i++)
            {
                var rt = (RectTransform)rows[i].container.transform;
                rt.localScale = Vector3.one * RowScale;
                AddNumber(rows[i], i + 1);
                AddBackground(rows[i]);
                CenterNameAndSlider(rows[i]);
            }
            // The invite button sits outside the layout at a fixed spot that the 10 rows now cover; move it below the panel.
            var invite = parent.Find("InviteRinkCopy") as RectTransform;
            // Centred under the slot backgrounds (rows are centred in the panel, their boxes sit SlotCenterX off-centre).
            if (invite != null) invite.anchoredPosition = new Vector2(parent.rect.width / 2f + SlotCenterX * RowScale, -parent.rect.height - 40f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            LogLayout("after", parent, group);
        }

        static void LogLayout(string when, RectTransform parent, HorizontalOrVerticalLayoutGroup g)
        {
            var sb = new System.Text.StringBuilder($"[BiggerLobby] menu layout {when}: parent {parent.name} rect={parent.rect} ");
            sb.Append(g == null ? "no layout group" : $"{g.GetType().Name} spacing={g.spacing} align={g.childAlignment} pad={g.padding} ctrlH={g.childControlHeight} expandH={g.childForceExpandHeight} scaleH={g.childScaleHeight}");
            foreach (RectTransform c in parent)
                sb.Append($"\n  {c.name} active={c.gameObject.activeSelf} pos={c.anchoredPosition} size={c.sizeDelta} anchors={c.anchorMin}-{c.anchorMax} pivot={c.pivot} scale={c.localScale.x}");
            Debug.Log(sb.ToString());
        }

        // The name sat at the top of the avatar and the slider near its bottom; pull both toward the
        // avatar's centre so name + slider read as one block next to the picture.
        static void CenterNameAndSlider(Row row)
        {
            void SetY(Component c, float y)
            {
                if (c == null) return;
                var rt = (RectTransform)c.transform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            }
            SetY(row.nickNameText, 20f);
            SetY(row.infoButton, 20f);
            SetY(row.volumeSlider, -18f);
            SetY(row.speakButton, -18f);
            SetY(row.pingImage, -18f);
        }

        // Dark, slightly transparent box behind the whole slot (number to ping icon), drawn under the row's parts.
        static void AddBackground(Row row)
        {
            var t = row.container.transform;
            if (t.Find("SlotBackground") != null) return;
            var bg = new GameObject("SlotBackground", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)bg.transform;
            rt.SetParent(t, false);
            rt.SetAsFirstSibling();
            rt.anchoredPosition = new Vector2(SlotCenterX, 0f);
            rt.sizeDelta = new Vector2(720f, 96f);   // spans x -435..285 around the row's centre
            var img = bg.GetComponent<Image>();
            img.color = SlotBackground;
            img.raycastTarget = false;
        }

        // Slot number in its own label left of the row (the name already has the info icon next to it).
        static void AddNumber(Row row, int n)
        {
            var name = row.nickNameText;
            if (name == null || Numbers.ContainsKey(name)) return;
            var label = Object.Instantiate(name.gameObject, row.container.transform).GetComponent<TMP_Text>();
            label.name = "SlotNumber";
            label.text = n.ToString();
            label.alignment = TextAlignmentOptions.MidlineRight;
            var rt = label.rectTransform;
            rt.sizeDelta = new Vector2(60f, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(-400f, 0f);   // left of the kick button (-340) and avatar (-250)
            Numbers[name] = label;
        }

        // tempVolumeList is padded/trimmed to 4 and ping icons stop at 4: use 10.
        [HarmonyTranspiler, HarmonyPatch("OnEnable"), HarmonyPatch("SetPingImage")]
        static IEnumerable<CodeInstruction> FourToTen(IEnumerable<CodeInstruction> code)
        {
            foreach (var x in code)
                yield return x.opcode == OpCodes.Ldc_I4_4 ? new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)Plugin.MaxPlayers).MoveLabelsFrom(x) : x;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))]
        static void ClearPlaceholders(UIPrefab_InGameMenu __instance)
        {
            foreach (var row in __instance.playerUIElements) SetPlaceholder(row, false);
        }

        // Rows past the joined players become dimmed "Empty slot" boxes.
        [HarmonyPostfix, HarmonyPatch(nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))]
        static void ShowPlaceholders(UIPrefab_InGameMenu __instance)
        {
            var joined = Traverse.Create(__instance).Field("playerInfos").GetValue<IList>()?.Count ?? 0;
            var rows = __instance.playerUIElements;
            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= joined) { SetPlaceholder(rows[i], true); continue; }
                // Rows with a player always show these (info/kick stay under the game's control).
                foreach (var part in new Component[] { rows[i].avatarButton, rows[i].volumeSlider, rows[i].speakButton, rows[i].pingImage })
                    if (part != null) part.gameObject.SetActive(true);
            }
        }

        static void SetPlaceholder(Row row, bool on)
        {
            if (row?.container == null) return;
            // Only restore what the placeholder hid, so per-row game state (e.g. no info button on your own row) is kept.
            if (!Hidden.TryGetValue(row.container, out var hidden)) Hidden[row.container] = hidden = new List<GameObject>();
            if (on)
            {
                foreach (var part in new Component[] { row.avatarButton, row.volumeSlider, row.speakButton, row.infoButton, row.kickButton, row.pingImage })
                    if (part != null && part.gameObject.activeSelf) { part.gameObject.SetActive(false); hidden.Add(part.gameObject); }
            }
            else
            {
                foreach (var go in hidden) if (go != null) go.SetActive(true);
                hidden.Clear();
            }

            var name = row.nickNameText;
            if (name == null) return;
            if (!NameStyles.ContainsKey(name)) NameStyles[name] = (name.color, name.alignment, name.rectTransform.anchoredPosition);
            var (color, align, pos) = NameStyles[name];
            if (on)
            {
                // Centred in the slot box, on the same line as the slot number.
                row.container.SetActive(true);
                name.text = "Empty slot";
                name.color = PlaceholderColor;
                name.alignment = TextAlignmentOptions.Center;
                name.rectTransform.anchoredPosition = new Vector2(SlotCenterX, 0f);
            }
            else
            {
                name.color = color;
                name.alignment = align;
                name.rectTransform.anchoredPosition = pos;
            }
        }
    }
}
