// MIMESIS Fov - field of view setting for the first-person camera
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The game has no FOV option: the player camera (CameraManager.playerCamera, a Cinemachine camera) keeps the
// prefab's value. This keeps that camera at the chosen FOV every frame, except while the game's own
// "zoom to face" effect runs (it animates FOV and restores it afterwards). Client-side only.
// In game, tap [ / ] for 5-degree steps or hold them to change it smoothly (50-120), with a short on-screen message.

using System;
using MimesisPatches;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fov
{
    public static class Plugin
    {
        static bool _initialized;

        /// <summary>Chosen FOV in degrees, or 0 for the game default.</summary>
        public static int Value
        {
            get => PlayerPrefs.GetInt("medovanx.Fov.Value", 0);
            set { PlayerPrefs.SetInt("medovanx.Fov.Value", value); PlayerPrefs.Save(); }
        }

        // Called once at the start of Hub.Awake (see Common/BepInExPlugin.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                var go = new GameObject("MedovanxFov");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<FovApplier>();
                PatchMenu.Register("Fov", version,
                    status: () => Value > 0 ? $"Field of view {Value}°" : $"Game default ({FovApplier.DefaultFov:0}°)",
                    build: (page, font) =>
                    {
                        PatchUi.Slider(page, font, "Field of view", FovApplier.Min, FovApplier.Max,
                            () => Value > 0 ? Value : Mathf.RoundToInt(FovApplier.DefaultFov), v => Value = v, "°");
                        PatchUi.Options(page, font, "", ("Reset to default", () => Value == 0, () => Value = 0));
                        PatchUi.Label(page, font, "Applies right away. In game, tap [ / ] for 5° steps or hold them to change it quickly. Only you need this patch.", 20f, PatchUi.Dim);
                    });
                Debug.Log($"[Fov] v{version.ToString(3)}: loaded (fov {(Value > 0 ? Value.ToString() : "default")})");
            }
            catch (Exception e)
            {
                Debug.LogError("[Fov] Failed to start: " + e);
            }
        }
    }

    sealed class FovApplier : MonoBehaviour
    {
        public static float DefaultFov = 60f;
        static bool _haveDefault;
        public const int Step = 5, Min = 50, Max = 120;

        TMP_Text _toast;
        float _toastUntil;

        void Update()
        {
            var kb = Keyboard.current;
            var cm = Hub.s != null ? Hub.s.cameraman : null;
            if (kb == null || cm == null || cm.playerCamera == null) return;
            if (Hub.s.uiman != null && Hub.s.uiman.isGameMenuOpen) return;
            // Tap: one 5-degree step. Hold: after a short delay it keeps changing smoothly (RepeatSpeed degrees/second).
            int dir = kb.rightBracketKey.isPressed ? 1 : kb.leftBracketKey.isPressed ? -1 : 0;
            if (dir == 0) { _held = 0f; return; }
            int current = Plugin.Value > 0 ? Plugin.Value : Mathf.RoundToInt(DefaultFov);
            if (kb.rightBracketKey.wasPressedThisFrame || kb.leftBracketKey.wasPressedThisFrame)
            {
                _held = 0f;
                _exact = Mathf.Clamp(current + dir * Step, Min, Max);
            }
            else
            {
                _held += Time.unscaledDeltaTime;
                if (_held < RepeatDelay) return;
                _exact = Mathf.Clamp(_exact + dir * RepeatSpeed * Time.unscaledDeltaTime, Min, Max);
            }
            int next = Mathf.RoundToInt(_exact);
            if (next == current) return;
            Plugin.Value = next;
            Toast($"FOV {next}°");
        }

        const float RepeatDelay = 0.35f, RepeatSpeed = 40f;
        float _held, _exact;

        void Toast(string text)
        {
            if (_toast == null)
            {
                var canvas = new GameObject("FovToast", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
                canvas.transform.SetParent(transform, false);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 120;
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                _toast = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                _toast.transform.SetParent(canvas.transform, false);
                var rt = _toast.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.75f);
                rt.sizeDelta = new Vector2(400f, 60f);
                var font = FindAnyObjectByType<TMP_Text>()?.font;
                if (font != null) _toast.font = font;
                _toast.fontSize = 34f;
                _toast.alignment = TextAlignmentOptions.Center;
                _toast.color = new Color(0.95f, 0.95f, 0.9f, 1f);
                _toast.raycastTarget = false;
            }
            _toast.text = text;
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + 1.5f;
        }

        void LateUpdate()
        {
            if (_toast != null && _toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil) _toast.gameObject.SetActive(false);
            var cm = Hub.s != null ? Hub.s.cameraman : null;
            var cam = cm != null ? cm.playerCamera : null;
            if (cam == null || cm.IsZoomToTargetActive) return;
            if (!_haveDefault) { DefaultFov = cam.Lens.FieldOfView; _haveDefault = true; }
            float want = Plugin.Value > 0 ? Plugin.Value : DefaultFov;
            if (!Mathf.Approximately(cam.Lens.FieldOfView, want)) cam.Lens.FieldOfView = want;
        }
    }
}
