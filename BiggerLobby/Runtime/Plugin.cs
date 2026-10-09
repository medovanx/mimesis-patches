// MIMESIS BiggerLobby - runtime UI support for up to 10 players
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace BiggerLobby
{
    public static class Plugin
    {
        public const int MaxPlayers = 10;

        static bool _initialized;

        // Called once from the patched Hub.Awake.
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                new Harmony("medovanx.biggerlobby").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchChip.Register("BiggerLobby", typeof(Plugin).Assembly.GetName().Version);
                Debug.Log($"[BiggerLobby] v{typeof(Plugin).Assembly.GetName().Version.ToString(3)}: patches applied ({MaxPlayers} players)");
            }
            catch (Exception e)
            {
                Debug.LogError("[BiggerLobby] Failed to apply UI patches: " + e);
            }
        }
    }
}
