// MIMESIS LateJoin - join a game that's already running; you enter at the tram when the team gets back
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Since EA 0.4.0 vanilla accepts joins mid-run itself ("barge-in"): the join reply names the current room and
// the joiner loads straight into the level (as a spectator), the running tram or the death match.
// GameSessionInfo.CanEnterSession is no longer called, so the old host override of it is gone.
// But the tram/lobby room (MaintenanceRoom) still refuses entry while the session is not in the tram
// (VRoomManager.EnterMaintenenceRoom -> RoomBlocked), and a player who reaches the tram then (e.g. joined just as
// the team left) is sent back to the menu. So:
//   host   publishes "team is away" in the Steam lobby data
//   client before entering the tram, waits on a message until that flag clears, then enters the tram as usual
// Both the host and the joining player need this patch.

using System.Collections;
using HarmonyLib;
using ReluNetwork.ConstEnum;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LateJoin
{
    [HarmonyPatch]
    static class LateJoinPatch
    {
        const string LobbyKey = "medovanx.inlevel";

        /// <summary>Host setting (Patches window): let players who reach the tram mid-run wait for the team.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("medovanx.LateJoin.Enabled", 1) == 1;
            set { PlayerPrefs.SetInt("medovanx.LateJoin.Enabled", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static bool IsHost => Hub.s != null && Hub.s.pdata != null && Hub.s.pdata.ClientMode == NetworkClientMode.Host;
        static CSteamID Lobby => Hub.s != null && Hub.s.steamInviteDispatcher != null ? Hub.s.steamInviteDispatcher.joinedLobbyID : CSteamID.Nil;

        // ---------------- Host ----------------

        /// <summary>True while the tram/lobby room can't be entered, i.e. the team is on the way, in a level or a death match.</summary>
        static bool TeamAway()
        {
            // The tram room is no longer stopped during a level (EA 0.4.0); the server refuses tram entry by session
            // state instead (same states as VRoomManager.GetExpectedRoomTypeForState != Maintenance).
            var rooms = Hub.s?.vworld?.VRoomManager;
            if (rooms == null) return false;
            var state = rooms.GetGameSessionInfo().GameSessionState;
            return state == VGameSessionState.PreGame || state == VGameSessionState.OnPlaying || state == VGameSessionState.DeathMatch;
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

        // Entering the tram fails while the team is away, so wait (with a message) until the team is back first.
        // Also runs on reconnect (MaintenanceScene.CorReenterRoomAfterReconnect); the flag is normally clear then.
        [HarmonyPostfix, HarmonyPatch(typeof(MaintenanceScene), "TryEnterMaintenanceRoom")]
        static void WaitForTeam(MaintenanceScene __instance, ref IEnumerator __result) => __result = WaitThenEnter(__instance, __result);

        static IEnumerator WaitThenEnter(MaintenanceScene scene, IEnumerator enter)
        {
            if (HostSaysAway())
            {
                Debug.Log("[LateJoin] Team is in a level; waiting for them to return to the tram");
                var overlay = Overlay("The team is in a level.\nYou'll join them at the tram when they return.");
                // Belongs to the tram scene: if that unloads mid-wait (host left, back to the menu) this coroutine dies
                // and the Destroy below never runs, so let the overlay go with the scene.
                SceneManager.MoveGameObjectToScene(overlay, scene.gameObject.scene);
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
