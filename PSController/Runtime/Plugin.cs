// MIMESIS PSController - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

namespace PSController
{
    public static class Plugin
    {
        static bool _initialized;

        // Called once at the start of Hub.Awake (see Common/BepInExPlugin.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            new HarmonyLib.Harmony("medovanx.pscontroller").PatchAll(typeof(Plugin).Assembly);
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
