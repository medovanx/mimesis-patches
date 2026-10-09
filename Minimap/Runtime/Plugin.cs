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
                // The chip shows the map mode; clicking it switches Explored <-> Whole.
                MimesisPatches.PatchChip.Register("Minimap", version,
                    status: () => Minimap.CurrentMode == Minimap.Mode.Whole ? "Whole map" : "Explored only",
                    onClick: () => Minimap.CurrentMode = Minimap.CurrentMode == Minimap.Mode.Whole ? Minimap.Mode.Explored : Minimap.Mode.Whole);
                Debug.Log($"[Minimap] v{version.ToString(3)}: loaded (mode: {Minimap.CurrentMode})");
            }
            catch (Exception e)
            {
                Debug.LogError("[Minimap] Failed to start: " + e);
            }
        }
    }
}
