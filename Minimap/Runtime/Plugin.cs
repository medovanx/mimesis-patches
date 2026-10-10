// MIMESIS Minimap - startup
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System;
using UnityEngine;

namespace Minimap
{
    public static class Plugin
    {
        static bool _initialized;

        // Called once at the start of Hub.Awake (see Common/BepInExPlugin.cs).
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            var version = typeof(Plugin).Assembly.GetName().Version;
            try
            {
                Minimap.Create();
                // The chip shows the current settings; clicking it opens the settings window.
                MimesisPatches.PatchMenu.Register("Minimap", version, status: () => Minimap.Summary, build: (page, font) =>
                {
                    MimesisPatches.PatchUi.OnOff(page, font, "Minimap", () => Minimap.Visible, v => Minimap.Visible = v);
                    MimesisPatches.PatchUi.Options(page, font, "Reveal",
                        ("Explored", () => Minimap.CurrentReveal == Minimap.Reveal.Explored, () => Minimap.CurrentReveal = Minimap.Reveal.Explored),
                        ("Full map", () => Minimap.CurrentReveal == Minimap.Reveal.Full, () => Minimap.CurrentReveal = Minimap.Reveal.Full));
                    MimesisPatches.PatchUi.Options(page, font, "Style",
                        ("Plain", () => Minimap.CurrentStyle == Minimap.Style.Plain, () => Minimap.CurrentStyle = Minimap.Style.Plain),
                        ("Graphic", () => Minimap.CurrentStyle == Minimap.Style.Graphic, () => Minimap.CurrentStyle = Minimap.Style.Graphic));
                    MimesisPatches.PatchUi.Options(page, font, "Show",
                        ("Players", () => Minimap.ShowPlayers, () => Minimap.ShowPlayers = !Minimap.ShowPlayers),
                        ("Monsters", () => Minimap.ShowMonsters, () => Minimap.ShowMonsters = !Minimap.ShowMonsters),
                        ("Items", () => Minimap.ShowItems, () => Minimap.ShowItems = !Minimap.ShowItems));
                    MimesisPatches.PatchUi.Label(page, font,
                        "M shows/hides it in game. Mimics count as monsters: with Players on and Monsters off, a \"player\" without a dot is a mimic.",
                        20f, MimesisPatches.PatchUi.Dim);
                });
                Debug.Log($"[Minimap] v{version.ToString(3)}: loaded ({Minimap.Summary})");
            }
            catch (Exception e)
            {
                Debug.LogError("[Minimap] Failed to start: " + e);
            }
        }
    }
}
