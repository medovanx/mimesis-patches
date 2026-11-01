// MIMESIS HostOptions - host lobby options: infinite stamina and starting money
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Both are simulated on the host, so only the host needs this patch:
// - Stamina: the server-side movement code drains it through StatController.ConsumeStamina and every
//   player's game only displays the synced value. With "Infinite Stamina" on, players never drain.
// - Money: the lobby room (MaintenanceRoom) sets its funds from C_InitialMoney when it's created and when
//   a new run starts, then sends the amount to everyone. "Starting money" replaces that value.
// The controls sit in the host's lobby TRAM Settings popup under "Use Entry Password"; other players never see them.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ReluNetwork.ConstEnum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HostOptions
{
    [HarmonyPatch]
    static class HostOptionsPatch
    {
        const string StaminaKey = "medovanx.Stamina.Infinite";
        const string MoneyKey = "medovanx.HostOptions.StartMoney";
        const string StaminaRow = "MedovanxInfiniteStamina", MoneyRow = "MedovanxStartMoney";

        public static bool InfiniteStamina
        {
            get => PlayerPrefs.GetInt(StaminaKey, 0) == 1;
            set { PlayerPrefs.SetInt(StaminaKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Starting money, or -1 to keep the game's default.</summary>
        public static int StartMoney
        {
            get => PlayerPrefs.GetInt(MoneyKey, -1);
            set { PlayerPrefs.SetInt(MoneyKey, value); PlayerPrefs.Save(); }
        }

        static bool IsHost => Hub.s != null && Hub.s.pdata != null && Hub.s.pdata.ClientMode == NetworkClientMode.Host;
        public static int DefaultMoney => Hub.s.dataman.ExcelDataManager.Consts.C_InitialMoney;

        // ---------------- Stamina (server side) ----------------

        [HarmonyPrefix, HarmonyPatch(typeof(StatController), nameof(StatController.ConsumeStamina))]
        static bool Consume(StatController __instance) => !(InfiniteStamina && IsHost && __instance.Self is VPlayer);

        // ---------------- Starting money (server side) ----------------

        static MaintenanceRoom _room;
        static int _lastStart = -1;

        public static int Starting(int gameDefault)
        {
            var value = StartMoney >= 0 ? StartMoney : gameDefault;
            _lastStart = value;
            return value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MaintenanceRoom), MethodType.Constructor, typeof(VRoomManager), typeof(long), typeof(IVRoomProperty))]
        static void TrackRoom(MaintenanceRoom __instance) => _room = __instance;

        // Changing the value in the lobby also sets the current funds to it, but only before the run's first departure.
        public static void ApplyNow(int value)
        {
            if (_room == null) { Debug.Log("[HostOptions] Starting money saved; no lobby room yet, applies to the next run"); return; }
            if (_room._everDeparted) { Debug.Log("[HostOptions] Starting money saved; this run already departed, applies to the next run"); return; }
            // Set the funds to the amount (a continued save loads its own funds, so a difference-based
            // adjustment can't know what the run "started" with).
            _lastStart = value;
            _room.AddCurrency(value - _room.Currency);
            Debug.Log($"[HostOptions] Funds set to {_room.Currency}");
        }

        // ---------------- Lobby menu controls (host only) ----------------

        // EA 0.4.0 moved "Allow Public Match" and "Use Entry Password" out of the Esc menu (UIPrefab_InGameMenu is
        // no longer created) into the host's TRAM Settings popup in the lobby, so the controls are added to its list.
        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_TramSettingPopup), "OnEnable")]
        static void AddControls(UIPrefab_TramSettingPopup __instance)
        {
            var publicItem = __instance.UE_UIPrefab_TramSettingListItemPublicToggle;
            var titleItem = __instance.UE_UIPrefab_TramSettingListItemPublicToggle_Sub;
            if (publicItem == null || titleItem == null) { Warn("TRAM Settings list not found; lobby controls not added"); return; }
            var list = publicItem.parent;

            // Appended below "Use Entry Password" (the list stacks its items): "Infinite Stamina [ON/OFF]", then a
            // "STARTING MONEY: [field]" row, like the "TITLE:" row under "Allow Public Match".
            var stamina = list.Find(StaminaRow) ?? BuildStaminaRow(publicItem);
            var money = list.Find(MoneyRow) ?? BuildMoneyRow(titleItem);

            stamina.gameObject.SetActive(IsHost);
            money.gameObject.SetActive(IsHost);
            var toggle = stamina.GetComponentInChildren<Toggle>(true);
            if (toggle != null) { toggle.SetIsOnWithoutNotify(InfiniteStamina); ShowOnOff(toggle); }
            var input = money.GetComponentInChildren<TMP_InputField>(true);
            if (input != null) input.SetTextWithoutNotify((StartMoney >= 0 ? StartMoney : DefaultMoney).ToString());
        }

        static bool _warned;
        static void Warn(string message)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning("[HostOptions] " + message);
        }

        // Copies one of the popup's list items. Its localized labels would put the game's text back (on a language
        // change, or when a copy of a hidden item is first shown), so the copy's texts are plain.
        static Transform CloneItem(Transform item, string name)
        {
            var block = Object.Instantiate(item.gameObject, item.parent, false).transform;
            block.name = name;
            foreach (var l10n in block.GetComponentsInChildren<UIApplyL10N>(true)) Object.DestroyImmediate(l10n);
            block.gameObject.SetActive(true);
            return block;
        }

        static void SetLabel(Transform block, string path, string text)
        {
            var label = block.Find(path)?.GetComponent<TMP_Text>();
            if (label != null) label.text = text;
            else Warn($"{block.name}: label '{path}' not found");
        }

        // Same as the game's ON/OFF text on its TRAM Settings toggles.
        static void ShowOnOff(Toggle toggle)
        {
            var text = toggle.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = Hub.GetL10NText(toggle.isOn ? "UI_COMMON_BUTTON_ON" : "UI_COMMON_BUTTON_OFF");
        }

        // Copies the "Allow Public Match [ON/OFF]" item.
        static Transform BuildStaminaRow(Transform publicItem)
        {
            var block = CloneItem(publicItem, StaminaRow);
            SetLabel(block, "InfoText", "Infinite Stamina");

            var toggle = block.GetComponentInChildren<Toggle>(true);
            if (toggle == null) { Warn("Infinite Stamina: toggle not found"); return block; }
            toggle.onValueChanged.RemoveAllListeners();
            toggle.SetIsOnWithoutNotify(InfiniteStamina);
            ShowOnOff(toggle);
            toggle.onValueChanged.AddListener(on =>
            {
                InfiniteStamina = on;
                ShowOnOff(toggle);
                Debug.Log($"[HostOptions] Infinite stamina {(on ? "enabled" : "disabled")}");
            });
            return block;
        }

        // Copies the "TITLE: [field] [Apply]" row as "STARTING MONEY: [amount]".
        static Transform BuildMoneyRow(Transform titleItem)
        {
            var block = CloneItem(titleItem, MoneyRow);
            SetLabel(block, "Layout/InfoText", "STARTING MONEY:");

            // The game's "Apply" and clear buttons for the room title aren't needed: the amount applies when you finish typing.
            foreach (var b in block.GetComponentsInChildren<Button>(true)) b.gameObject.SetActive(false);

            var input = block.GetComponentInChildren<TMP_InputField>(true);
            if (input == null) { Warn("Starting money: input field not found"); return block; }
            input.onValueChanged.RemoveAllListeners();
            input.onEndEdit.RemoveAllListeners();
            input.onSubmit.RemoveAllListeners();
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 7;
            if (input.placeholder is TMP_Text ph) ph.text = DefaultMoney.ToString();
            input.onEndEdit.AddListener(text =>
            {
                int value = int.TryParse(text, out var v) && v >= 0 ? v : DefaultMoney;
                StartMoney = value == DefaultMoney ? -1 : value;
                input.SetTextWithoutNotify(value.ToString());
                ApplyNow(value);
                Debug.Log($"[HostOptions] Starting money set to {value}");
            });
            return block;
        }
    }

    // Harmony doesn't allow TargetMethods() next to per-method annotations, so the money transpiler lives here.
    [HarmonyPatch]
    static class StartMoneyPatch
    {
        // Every MaintenanceRoom method (incl. compiler-generated lambdas) that reads C_InitialMoney.
        static IEnumerable<MethodBase> MoneyMethods()
        {
            var field = AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), "C_InitialMoney");
            var types = new[] { typeof(MaintenanceRoom) }.Concat(typeof(MaintenanceRoom).GetNestedTypes(AccessTools.all));
            foreach (var t in types)
            foreach (var m in t.GetMethods(AccessTools.allDeclared).Cast<MethodBase>().Concat(t.GetConstructors(AccessTools.allDeclared)))
            {
                List<KeyValuePair<OpCode, object>> body;
                try { body = PatchProcessor.ReadMethodBody(m).ToList(); } catch { continue; }
                if (body.Any(i => Equals(i.Value, field))) yield return m;
            }
        }

        static IEnumerable<MethodBase> TargetMethods() => MoneyMethods();

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UseStartMoney(IEnumerable<CodeInstruction> code)
        {
            var field = AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), "C_InitialMoney");
            foreach (var x in code)
            {
                yield return x;
                if (x.LoadsField(field)) yield return CodeInstruction.Call(typeof(HostOptionsPatch), nameof(HostOptionsPatch.Starting));
            }
        }

    }
}
