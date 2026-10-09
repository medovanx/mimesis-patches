// MIMESIS PSController - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

namespace PSController
{
    public static class Plugin
    {
        static bool _initialized;

        // Called once from the patched Hub.Awake.
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            MimesisPatches.PatchChip.Register("PSController", typeof(Plugin).Assembly.GetName().Version);
        }
    }
}
