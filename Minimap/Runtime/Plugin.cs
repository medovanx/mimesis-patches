// MIMESIS Minimap - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using UnityEngine;

namespace Minimap
{
    public static class Plugin
    {
        static bool _initialized;

        // Called once from the patched Hub.Awake.
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                Minimap.Create();
                // The chip shows the current settings; clicking it opens the settings window.
                MimesisPatches.PatchChip.Register("Minimap", version, status: () => Minimap.Summary, onClick: SettingsWindow.Open);
                Debug.Log($"[Minimap] v{version.ToString(3)}: loaded ({Minimap.Summary})");
            }
            catch (Exception e)
            {
                Debug.LogError("[Minimap] Failed to start: " + e);
            }
        }
    }
}
