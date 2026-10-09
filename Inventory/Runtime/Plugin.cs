// MIMESIS Inventory - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using HarmonyLib;
using UnityEngine;

namespace Inventory
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
                new Harmony("medovanx.inventory").PatchAll(typeof(Plugin).Assembly);
                MimesisPatches.PatchMenu.Register("Inventory", version,
                    status: () => $"Hosting: {InventoryPatch.HostSlots} slots · every player needs this patch",
                    build: (page, font) =>
                    {
                        MimesisPatches.PatchUi.Options(page, font, "Slots when hosting",
                            ("4", () => InventoryPatch.HostSlots == 4, () => InventoryPatch.HostSlots = 4),
                            ("5", () => InventoryPatch.HostSlots == 5, () => InventoryPatch.HostSlots = 5),
                            ("6", () => InventoryPatch.HostSlots == 6, () => InventoryPatch.HostSlots = 6),
                            ("7", () => InventoryPatch.HostSlots == 7, () => InventoryPatch.HostSlots = 7),
                            ("8", () => InventoryPatch.HostSlots == 8, () => InventoryPatch.HostSlots = 8));
                        MimesisPatches.PatchUi.Label(page, font,
                            "The host's number applies to everyone; more than 4 shows as a 2 × 4 grid.\n" +
                            "Every player needs this patch. It takes effect when characters spawn (next level, or reload the lobby).",
                            20f, MimesisPatches.PatchUi.Dim);
                    });
                Debug.Log($"[Inventory] v{version.ToString(3)}: patches applied");
            }
            catch (Exception e)
            {
                Debug.LogError("[Inventory] Failed to apply patches: " + e);
            }
        }
    }
}
