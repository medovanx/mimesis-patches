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
            PatchChip.Register("PSController v" + typeof(Plugin).Assembly.GetName().Version.ToString(3));
        }
    }
}
