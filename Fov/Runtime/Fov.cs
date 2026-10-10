// MIMESIS Fov - field of view setting for the first-person camera
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The game has no FOV option: the player camera (CameraManager.playerCamera, a Cinemachine camera) keeps the
// prefab's value. This keeps that camera at the chosen FOV every frame, except while the game's own
// "zoom to face" effect runs (it animates FOV and restores it afterwards). Client-side only.

using System;
using MimesisPatches;
using UnityEngine;

namespace Fov
{
    public static class Plugin
    {
        static bool _initialized;

        public static readonly int[] Choices = { 60, 70, 80, 90, 100, 110 };

        /// <summary>Chosen FOV in degrees, or 0 for the game default.</summary>
        public static int Value
        {
            get => PlayerPrefs.GetInt("medovanx.Fov.Value", 0);
            set { PlayerPrefs.SetInt("medovanx.Fov.Value", value); PlayerPrefs.Save(); }
        }

        // Called once from the patched Hub.Awake.
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
                        var options = new (string, Func<bool>, Action)[Choices.Length + 1];
                        options[0] = ("Default", () => Value == 0, () => Value = 0);
                        for (int i = 0; i < Choices.Length; i++)
                        {
                            int v = Choices[i];
                            options[i + 1] = (v + "°", () => Value == v, () => Value = v);
                        }
                        PatchUi.Options(page, font, "Field of view", options);
                        PatchUi.Label(page, font, "Applies to your first-person view right away. Only you need this patch.", 20f, PatchUi.Dim);
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

        void LateUpdate()
        {
            var cm = Hub.s != null ? Hub.s.cameraman : null;
            var cam = cm != null ? cm.playerCamera : null;
            if (cam == null || cm.IsZoomToTargetActive) return;
            if (!_haveDefault) { DefaultFov = cam.Lens.FieldOfView; _haveDefault = true; }
            float want = Plugin.Value > 0 ? Plugin.Value : DefaultFov;
            if (!Mathf.Approximately(cam.Lens.FieldOfView, want)) cam.Lens.FieldOfView = want;
        }
    }
}
