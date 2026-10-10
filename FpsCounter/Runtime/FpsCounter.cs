// MIMESIS FpsCounter - shows the frame rate in the bottom-right corner ("FPS: 144")
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// Drawn with IMGUI on top of everything, so it shows in menus and in game without touching the game's UI.
// The number is the average over the last half second, so it's readable instead of flickering. Client-side only.

using System;
using MimesisPatches;
using UnityEngine;

namespace FpsCounter
{
    public static class Plugin
    {
        static bool _initialized;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("medovanx.FpsCounter.Enabled", 1) == 1;
            set { PlayerPrefs.SetInt("medovanx.FpsCounter.Enabled", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // Called once at the start of Hub.Awake (see Common/BepInExPlugin.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                var go = new GameObject("MedovanxFpsCounter");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<FpsOverlay>();
                PatchMenu.Register("FpsCounter", version,
                    status: () => Enabled ? $"Showing FPS ({FpsOverlay.Current})" : "Hidden",
                    build: (page, font) =>
                    {
                        PatchUi.OnOff(page, font, "Show FPS", () => Enabled, v => Enabled = v);
                        PatchUi.Label(page, font, "Shows the frame rate in the bottom-right corner. Only you need this patch.", 20f, PatchUi.Dim);
                    });
                Debug.Log($"[FpsCounter] v{version.ToString(3)}: loaded");
            }
            catch (Exception e)
            {
                Debug.LogError("[FpsCounter] Failed to start: " + e);
            }
        }
    }

    sealed class FpsOverlay : MonoBehaviour
    {
        const float Window = 0.5f;   // seconds averaged per update
        public static int Current;

        int _frames;
        float _elapsed;
        string _text = "FPS: --";
        GUIStyle _style, _shadow;

        void Update()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < Window) return;
            Current = Mathf.RoundToInt(_frames / _elapsed);
            _text = "FPS: " + Current;
            _frames = 0;
            _elapsed = 0f;
        }

        void OnGUI()
        {
            if (!Plugin.Enabled || Event.current.type != EventType.Repaint) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
                _style.normal.textColor = new Color(0.9f, 0.92f, 0.88f, 0.9f);
                _shadow = new GUIStyle(_style);
                _shadow.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            }
            // Scale with the screen height (about 20 px at 1080p).
            _style.fontSize = _shadow.fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height / 54f));
            float margin = Screen.height / 90f;
            var rect = new Rect(0f, 0f, Screen.width - margin, Screen.height - margin);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), _text, _shadow);
            GUI.Label(rect, _text, _style);
        }
    }
}
