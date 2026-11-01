// MIMESIS PSController - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

namespace PSController
{
    public static class Plugin
    {
        static bool _initialized;

        // Called once at the start of Hub.Awake (see Common/MelonEntry.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            // Init runs inside Hub.Awake: a patch that no longer fits the game must not break the game's startup.
            try { new HarmonyLib.Harmony("medovanx.pscontroller").PatchAll(typeof(Plugin).Assembly); }
            catch (System.Exception e) { UnityEngine.Debug.LogError("[PSController] Failed to apply patches: " + e); }
            MimesisPatches.PatchMenu.Register("PSController", typeof(Plugin).Assembly.GetName().Version,
                status: () => PSIcons.Enabled ? "PlayStation button icons" : "Xbox button icons (game default)",
                build: (page, font) =>
                {
                    MimesisPatches.PatchUi.OnOff(page, font, "PlayStation icons", () => PSIcons.Enabled, v => PSIcons.Enabled = v);
                    MimesisPatches.PatchUi.Label(page, font,
                        "Use DS4Windows with Xbox 360 output so your buttons are read correctly.",
                        20f, MimesisPatches.PatchUi.Dim);
                });
        }
    }
}
