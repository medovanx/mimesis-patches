// MIMESIS PSController - controller selection
// Author: Mohamed Darwesh (@medovanx) - https://github.com/medovanx

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

namespace PSController
{
    // Replaces every Gamepad.current read in the game.
    // DS4Windows exposes a virtual Xbox pad next to the real PS controller. Unity can read the raw
    // PS pad with wrong buttons (L1 shows up as LT), so prefer the Xbox pad whenever one exists.
    public static class Pads
    {
        static bool _logged;

        public static Gamepad Current
        {
            get
            {
                var xbox = Gamepad.all.FirstOrDefault(g => g is XInputController && g.added);
                if (!_logged && Gamepad.all.Count > 0)
                {
                    _logged = true;
                    Debug.Log("[PSController] Gamepads: " + string.Join(", ", Gamepad.all.Select(g => $"{g.displayName} ({g.GetType().Name})"))
                        + (xbox != null ? $" -> using {xbox.displayName}" : " -> using Gamepad.current"));
                }
                var pad = xbox ?? Gamepad.current;
                if (Debug_ && pad != null && _lastFrame != Time.frameCount)
                {
                    _lastFrame = Time.frameCount;
                    foreach (var c in pad.allControls)
                        if (c is UnityEngine.InputSystem.Controls.ButtonControl b && b.wasPressedThisFrame)
                            Debug.Log($"[PSController] {pad.displayName}: pressed {b.path} (value {b.ReadValue():0.00})");
                }
                return pad;
            }
        }

        // Logs every gamepad press to Player.log while PSController-debug.txt exists next to the mod DLL.
        static int _lastFrame = -1;
        static readonly bool Debug_ = System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Pads).Assembly.Location), "PSController-debug.txt"));
    }
}
