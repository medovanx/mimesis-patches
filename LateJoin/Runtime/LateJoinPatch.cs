// MIMESIS LateJoin - join a game that's already running; you enter at the tram when the team gets back
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Vanilla blocks joining once the team departs:
//   host   GameSessionInfo.CanEnterSession refuses new players outside the lobby -> GameAlreadyStarted
//   host   the tram/lobby room (MaintenanceRoom) is stopped while the team is in a level -> RoomBlocked
//   client a joining player can only load into the tram/lobby (the join reply only describes that room)
// So:
//   host   accepts joins at any time and publishes "team is in a level" in the Steam lobby data
//   client after joining, waits on a message until that flag clears, then enters the tram as usual
// Both the host and the joining player need this patch.

using System.Collections;
using HarmonyLib;
using ReluNetwork.ConstEnum;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LateJoin
{
    [HarmonyPatch]
    static class LateJoinPatch
    {
        const string LobbyKey = "medovanx.inlevel";

        /// <summary>Host setting (Patches window): accept players while a run is in progress.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("medovanx.LateJoin.Enabled", 1) == 1;
            set { PlayerPrefs.SetInt("medovanx.LateJoin.Enabled", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static bool IsHost => Hub.s != null && Hub.s.pdata != null && Hub.s.pdata.ClientMode == NetworkClientMode.Host;
        static CSteamID Lobby => Hub.s != null && Hub.s.steamInviteDispatcher != null ? Hub.s.steamInviteDispatcher.joinedLobbyID : CSteamID.Nil;

        // ---------------- Host ----------------

        [HarmonyPostfix, HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.CanEnterSession))]
        static void AllowJoin(ref bool __result)
        {
            if (!__result && Enabled) __result = true;
        }

        /// <summary>True while the tram/lobby room is stopped, i.e. the team is in a level or a death match.</summary>
        static bool TeamAway()
        {
            var rooms = Hub.s?.vworld?.VRoomManager;
            if (rooms == null) return false;
            foreach (var room in rooms._vrooms.Values)
                if (room is MaintenanceRoom m) return !m.IsPlayable();
            return false;
        }

        static string _published;

        // Called every second by Ticker: keep the lobby flag current.
        public static void HostTick()
        {
            if (!IsHost || Lobby == CSteamID.Nil) { _published = null; return; }
            var value = Enabled && TeamAway() ? "1" : "0";
            if (value == _published) return;
            try { SteamMatchmaking.SetLobbyData(Lobby, LobbyKey, value); _published = value; } catch { }
        }

        // ---------------- Joining player ----------------

        static bool HostSaysAway()
        {
            if (IsHost || Lobby == CSteamID.Nil) return false;
            try { return SteamMatchmaking.GetLobbyData(Lobby, LobbyKey) == "1"; } catch { return false; }
        }

        // Entering the tram fails while it's stopped, so wait (with a message) until the team is back first.
        [HarmonyPostfix, HarmonyPatch(typeof(MaintenanceScene), "TryEnterMaintenanceRoom")]
        static void WaitForTeam(ref IEnumerator __result) => __result = WaitThenEnter(__result);

        static IEnumerator WaitThenEnter(IEnumerator enter)
        {
            if (HostSaysAway())
            {
                Debug.Log("[LateJoin] Team is in a level; waiting for them to return to the tram");
                var overlay = Overlay("The team is in a level.\nYou'll join them at the tram when they return.");
                var wait = new WaitForSecondsRealtime(2f);
                while (HostSaysAway()) yield return wait;
                Object.Destroy(overlay);
                Debug.Log("[LateJoin] Team is back; entering the tram");
            }
            yield return enter;
        }

        static GameObject Overlay(string message)
        {
            var canvas = new GameObject("LateJoinWaiting", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(canvas.transform, false);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.color = new Color(0f, 0f, 0f, 0.85f);
            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(canvas.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            var font = Object.FindAnyObjectByType<TMP_Text>()?.font;
            if (font != null) text.font = font;
            text.fontSize = 34f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.9f, 0.92f, 0.88f, 1f);
            text.text = message;
            Object.DontDestroyOnLoad(canvas.gameObject);
            return canvas.gameObject;
        }
    }

    sealed class Ticker : MonoBehaviour
    {
        float _next;
        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            LateJoinPatch.HostTick();
        }
    }
}
