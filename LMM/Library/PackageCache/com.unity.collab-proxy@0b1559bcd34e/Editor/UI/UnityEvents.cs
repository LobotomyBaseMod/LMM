using UnityEngine;

using Codice.Utils;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class Keyboard
    {
        internal static bool IsTabPressed(Event e)
        {
            if (e == null)
                return false;

            return IsKeyPressed(e, KeyCode.Tab);
        }

        internal static bool IsShiftPressed(Event e)
        {
            if (e == null)
                return false;

            return e.type == EventType.KeyDown
                && e.modifiers == EventModifiers.Shift;
        }

        internal static bool HasShiftModifier(Event e)
        {
            if (e == null)
                return false;

            return (e.modifiers & EventModifiers.Shift) == EventModifiers.Shift;
        }

        internal static bool IsReturnOrEnterKeyPressed(Event e)
        {
            if (e == null)
                return false;

            return IsKeyPressed(e, KeyCode.Return) ||
                   IsKeyPressed(e, KeyCode.KeypadEnter);
        }

        internal static bool IsDeleteKeyPressed(Event e)
        {
            if (e == null)
                return false;

            return IsKeyPressed(e, KeyCode.Delete);
        }

        internal static bool IsKeyPressed(Event e, KeyCode keyCode)
        {
            if (e == null)
                return false;

            return e.type == EventType.KeyDown
                && e.keyCode == keyCode;
        }

        internal static bool IsCharacterPressed(Event e, char character)
        {
            if (e == null)
                return false;

            return e.type == EventType.KeyDown
                && e.character == character;
        }

        internal static bool IsControlOrCommandKeyPressed(Event e)
        {
            if (e == null)
                return false;

            if (PlatformIdentifier.IsMac())
                return e.type == EventType.KeyDown && e.modifiers == EventModifiers.Command;

            return e.type == EventType.KeyDown && e.modifiers == EventModifiers.Control;
        }

        internal static bool HasControlOrCommandModifier(Event e)
        {
            if (e == null)
                return false;

            if (PlatformIdentifier.IsMac())
                return (e.modifiers & EventModifiers.Command) == EventModifiers.Command;

            return (e.modifiers & EventModifiers.Control) == EventModifiers.Control;
        }

        internal static bool IsControlOrCommandAndShiftKeyPressed(Event e)
        {
            if (e == null)
                return false;

            if (PlatformIdentifier.IsMac())
                return e.type == EventType.KeyDown &&
                       e.modifiers == (EventModifiers.Command | EventModifiers.Shift);

            return e.type == EventType.KeyDown &&
                   e.modifiers == (EventModifiers.Control | EventModifiers.Shift);
        }
    }

    internal class Mouse
    {
        internal static bool IsLeftMouseButtonDoubleClicked(Event e)
        {
            return IsLeftMouseButtonPressed(e)
                && e.clickCount == 2;
        }

        internal static bool IsLeftMouseButtonPressed(Event e)
        {
            if (e == null)
                return false;

            if (!e.isMouse)
                return false;

            return (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                && e.button == UnityConstants.LEFT_MOUSE_BUTTON;
        }

        internal static bool IsRightMouseButtonPressed(Event e)
        {
            if (e == null)
                return false;

            if (!e.isMouse)
                return false;

            return (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                && e.button == UnityConstants.RIGHT_MOUSE_BUTTON;
        }
    }

    internal class CommandEvent
    {
        internal static bool IsNewKeyboardFocus(Event e)
        {
            if (e == null)
                return false;

            return e.type == EventType.ExecuteCommand
                && e.commandName == "NewKeyboardFocus";
        }
    }
}
