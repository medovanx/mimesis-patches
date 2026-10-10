// MIMESIS Patches - BepInEx entry point, compiled into every patch
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx
//
// The game files stay untouched: <Patch>.Plugin.Init() runs from a Harmony prefix on Hub.Awake, when the game's
// managers exist, and sets up the patch's Harmony patches and its page in the Patches window.
using System;
using BepInEx;
using HarmonyLib;

namespace MimesisPatches
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public sealed class BepInExPlugin : BaseUnityPlugin
    {
        static Action _init;

        void Awake()
        {
            var plugin = typeof(BepInExPlugin).Assembly.GetType(MyPluginInfo.PLUGIN_NAME + ".Plugin");
            var init = plugin?.GetMethod("Init", Type.EmptyTypes);
            if (init == null) { Logger.LogError($"{MyPluginInfo.PLUGIN_NAME}.Plugin.Init not found"); return; }
            _init = (Action)Delegate.CreateDelegate(typeof(Action), init);

            var awake = AccessTools.Method(typeof(Hub), "Awake");
            new Harmony(MyPluginInfo.PLUGIN_GUID + ".startup").Patch(awake, prefix: new HarmonyMethod(typeof(BepInExPlugin), nameof(HubAwake)));
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded; starts with the game");
        }

        static void HubAwake() => _init?.Invoke();
    }
}
