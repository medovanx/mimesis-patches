// MIMESIS PSController - PlayStation button icons
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace PSController
{
    // Called from patches on KeyImageData.GetKeyImage(string) and UIPrefab_GamePadKeyIcon.SetKey (GamePatches).
    // Get returns a PS icon for gamepad keys, or null to use the game's own.
    public static class PSIcons
    {
        static Dictionary<string, Sprite> _sprites;

        // The game's own sprite of each pad icon's A/B/X/Y image, put back for keys without a PS icon (or icons off).
        sealed class GameLook { public Sprite Sprite; public bool PreserveAspect; }
        static readonly ConditionalWeakTable<Image, GameLook> GameLooks = new ConditionalWeakTable<Image, GameLook>();
        static bool _warned;

        // After the game's SetKey: show the PS icon on the A/B/X/Y image (letter cleared, other Xbox parts hidden).
        public static void Apply(UIPrefab_GamePadKeyIcon icon, string keyName)
        {
            var image = icon.UE_ABXYButton;
            if (image == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[PSController] Gamepad key icon has no ABXYButton image; keeping the game's icons there"); }
                return;
            }
            var look = GameLooks.GetValue(image, i => new GameLook { Sprite = i.sprite, PreserveAspect = i.preserveAspect });
            var sprite = Get(keyName);
            if (sprite == null)
            {
                image.sprite = look.Sprite;
                image.preserveAspect = look.PreserveAspect;
                return;
            }
            icon.ResetIcons();
            image.sprite = sprite;
            image.preserveAspect = true;
            if (icon.UE_UI_ABXYText != null) icon.UE_UI_ABXYText.text = "";
            image.gameObject.SetActive(true);
        }

        // Game key id -> icon file (several ids share a physical button).
        static readonly Dictionary<string, string> Files = new Dictionary<string, string>
        {
            { "p_a", "cross" }, { "p_south", "cross" },
            { "p_b", "circle" }, { "p_east", "circle" },
            { "p_x", "square" }, { "p_west", "square" },
            { "p_y", "triangle" }, { "p_north", "triangle" },
            { "p_select", "create" }, { "p_start", "options" },
            { "p_lb", "l1" }, { "p_rb", "r1" },
            { "p_lt", "l2" }, { "p_rt", "r2" },
            { "p_lsb", "l3" }, { "p_rsb", "r3" },
            { "p_dpad_up", "dpad_up" }, { "p_dpad_down", "dpad_down" },
            { "p_dpad_left", "dpad_left" }, { "p_dpad_right", "dpad_right" },
        };

        /// <summary>PlayStation icons on/off (Patches window); off = the game's Xbox icons.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("medovanx.PSController.Icons", 1) == 1;
            set { PlayerPrefs.SetInt("medovanx.PSController.Icons", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static Sprite Get(string keyName)
        {
            if (!Enabled || keyName == null || !keyName.StartsWith("p_")) return null;
            if (_sprites == null) Load();
            _sprites.TryGetValue(keyName.Trim().ToLowerInvariant(), out var s);
            return s;
        }

        static void Load()
        {
            _sprites = new Dictionary<string, Sprite>();
            var asm = typeof(PSIcons).Assembly;   // the icons are embedded in this DLL (PSIcons.<name>.png)
            var byFile = new Dictionary<string, Sprite>();
            foreach (var kv in Files)
            {
                if (!byFile.TryGetValue(kv.Value, out var sprite))
                {
                    byte[] png;
                    using (var stream = asm.GetManifestResourceStream("PSIcons." + kv.Value + ".png"))
                    {
                        if (stream == null) continue;
                        using (var ms = new MemoryStream()) { stream.CopyTo(ms); png = ms.ToArray(); }
                    }
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(png);
                    tex.filterMode = FilterMode.Bilinear;
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = "PS_" + kv.Value;
                    byFile[kv.Value] = sprite;
                }
                _sprites[kv.Key] = sprite;
            }
            Debug.Log($"[PSController] v{typeof(PSIcons).Assembly.GetName().Version.ToString(3)}: loaded {byFile.Count} PlayStation icons");
        }
    }
}
