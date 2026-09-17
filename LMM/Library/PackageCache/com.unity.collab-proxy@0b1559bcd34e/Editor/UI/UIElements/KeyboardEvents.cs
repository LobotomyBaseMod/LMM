using Codice.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.UI.UIElements
{
    internal static class KeyboardEvents
    {
        internal static bool IsAltPressed(KeyDownEvent evt)
        {
            return evt.altKey;
        }

        internal static bool IsControlPressed(KeyDownEvent evt)
        {
            return evt.ctrlKey;
        }

        internal static bool IsCommandPressed(KeyDownEvent evt)
        {
            return evt.commandKey;
        }

        internal static bool IsShiftPressed(KeyDownEvent evt)
        {
            return evt.shiftKey;
        }

        internal static bool IsEscapePressed(KeyDownEvent evt)
        {
            return evt.keyCode == KeyCode.Escape;
        }

        internal static bool IsEnterPressed(KeyDownEvent evt)
        {
            return evt.keyCode == KeyCode.Return ||
                   evt.keyCode == KeyCode.KeypadEnter;
        }

        internal static bool IsFindShortcutPressed(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.F)
                return false;

            if (PlatformIdentifier.IsMac())
                return evt.commandKey;

            return evt.ctrlKey;
        }
    }
}
