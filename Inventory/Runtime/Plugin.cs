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
                        // 1-8 as a 2 x 4 grid of buttons, like the in-game slots.
                        MimesisPatches.PatchUi.Label(page, font, "Slots when hosting", 22f, null, 32f);
                        for (int rowStart = 1; rowStart <= InventoryPatch.Max; rowStart += InventoryPatch.PerRow)
                        {
                            var row = MimesisPatches.PatchUi.Row(page);
                            for (int n = rowStart; n < rowStart + InventoryPatch.PerRow; n++)
                            {
                                int value = n;
                                var img = MimesisPatches.PatchUi.Button(row, font, value.ToString(), () => InventoryPatch.HostSlots = value, 120f)
                                    .GetComponent<UnityEngine.UI.Image>();
                                MimesisPatches.PatchUi.Refreshers.Add(() => img.color = InventoryPatch.HostSlots == value ? MimesisPatches.PatchUi.On : MimesisPatches.PatchUi.Off);
                            }
                        }
                        MimesisPatches.PatchUi.Label(page, font,
                            "The host's number applies to everyone; more than 4 shows as a 2 × 4 grid in game.\n" +
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
