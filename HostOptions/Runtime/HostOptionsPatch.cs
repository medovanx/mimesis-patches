// MIMESIS HostOptions - host lobby options: infinite stamina and starting money
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Both are simulated on the host, so only the host needs this patch:
// - Stamina: the server-side movement code drains it through StatController.ConsumeStamina and every
//   player's game only displays the synced value. With "Infinite Stamina" on, players never drain.
// - Money: the lobby room (MaintenanceRoom) sets its funds from C_InitialMoney when it's created and when
//   a new run starts, then sends the amount to everyone. "Starting money" replaces that value.
// The controls sit in the host's lobby menu under "Use Entry Password"; other players never see them.

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
        static int DefaultMoney => Hub.s.dataman.ExcelDataManager.Consts.C_InitialMoney;

        // ---------------- Stamina (server side) ----------------

        [HarmonyPrefix, HarmonyPatch(typeof(StatController), nameof(StatController.ConsumeStamina))]
        static bool Consume(StatController __instance) => !(InfiniteStamina && IsHost && __instance.Self is VPlayer);

        // ---------------- Starting money (server side) ----------------

        static MaintenanceRoom _room;
        static int _lastStart = -1;

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
                if (x.LoadsField(field)) yield return CodeInstruction.Call(typeof(HostOptionsPatch), nameof(Starting));
            }
        }

        static int Starting(int gameDefault)
        {
            var value = StartMoney >= 0 ? StartMoney : gameDefault;
            _lastStart = value;
            return value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MaintenanceRoom), MethodType.Constructor, typeof(VRoomManager), typeof(long), typeof(IVRoomProperty))]
        static void TrackRoom(MaintenanceRoom __instance) => _room = __instance;

        // Changing the value in the lobby also updates the current funds, but only before the first departure
        // and while nothing has been spent yet.
        static void ApplyNow(int value)
        {
            if (_room == null || _room._everDeparted || _room.Currency != _lastStart) return;
            _lastStart = value;
            _room.AddCurrency(value - _room.Currency);
        }

        // ---------------- Lobby menu controls (host only) ----------------

        [HarmonyPostfix, HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
        static void AddControls(UIPrefab_InGameMenu __instance)
        {
            var publicRoom = __instance.UE_PublicRoom as RectTransform;
            var password = __instance.UE_RoomPassword as RectTransform;
            if (publicRoom == null || password == null) return;
            var menu = publicRoom.parent;
            float step = publicRoom.anchoredPosition.y - password.anchoredPosition.y;   // "Allow Public Match" -> "Use Entry Password"

            var stamina = menu.Find(StaminaRow) ?? BuildStaminaRow(publicRoom, password.anchoredPosition - new Vector2(0f, step * 0.95f));
            // The block's "TITLE:" row sits 30 units lower than its checkbox, so offset the money block by
            // a little less than that to put "STARTING MONEY:" just under "Infinite Stamina".
            var money = menu.Find(MoneyRow) ?? BuildMoneyRow(publicRoom, ((RectTransform)stamina).anchoredPosition - new Vector2(0f, step * 0.15f));

            stamina.gameObject.SetActive(IsHost);
            money.gameObject.SetActive(IsHost);
            stamina.GetComponentInChildren<Toggle>(true)?.SetIsOnWithoutNotify(InfiniteStamina);
            var input = money.GetComponentInChildren<TMP_InputField>(true);
            if (input != null) input.SetTextWithoutNotify((StartMoney >= 0 ? StartMoney : DefaultMoney).ToString());
        }

        // Copies the whole "Allow Public Match" block (its label lives on the block, not the checkbox).
        static RectTransform CloneBlock(RectTransform publicRoom, string name, Vector2 position)
        {
            var block = (RectTransform)Object.Instantiate(publicRoom.gameObject, publicRoom.parent, false).transform;
            block.name = name;
            block.anchoredPosition = position;
            return block;
        }

        static TMP_Text BlockLabel(RectTransform block) =>
            block.GetComponent<TMP_Text>() ?? block.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(t => t.GetComponentInParent<Toggle>() == null && t.GetComponentInParent<TMP_InputField>() == null && t.name != "title");

        static RectTransform BuildStaminaRow(RectTransform publicRoom, Vector2 position)
        {
            var block = CloneBlock(publicRoom, StaminaRow, position);
            foreach (var n in new[] { "title", "RoomName" })
                if (block.Find(n) != null) Object.Destroy(block.Find(n).gameObject);
            var label = BlockLabel(block);
            if (label != null) label.text = "Infinite Stamina";

            var toggle = block.GetComponentInChildren<Toggle>(true);
            toggle.onValueChanged.RemoveAllListeners();
            toggle.SetIsOnWithoutNotify(InfiniteStamina);
            toggle.onValueChanged.AddListener(on =>
            {
                InfiniteStamina = on;
                Debug.Log($"[HostOptions] Infinite stamina {(on ? "enabled" : "disabled")}");
            });
            return block;
        }

        // Copies the block's "TITLE: [field]" row as "STARTING MONEY: [amount]".
        static RectTransform BuildMoneyRow(RectTransform publicRoom, Vector2 position)
        {
            var block = CloneBlock(publicRoom, MoneyRow, position);
            var toggle = block.GetComponentInChildren<Toggle>(true);
            if (toggle != null) Object.Destroy(toggle.gameObject);
            var label = BlockLabel(block);
            if (label != null) label.text = "";
            var title = block.Find("title")?.GetComponent<TMP_Text>();
            if (title != null) title.text = "STARTING MONEY:";

            // The game's "Apply" button next to the room title isn't needed: the amount applies when you finish typing.
            foreach (var b in block.GetComponentsInChildren<Button>(true)) b.gameObject.SetActive(false);

            var input = block.GetComponentInChildren<TMP_InputField>(true);
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
}
