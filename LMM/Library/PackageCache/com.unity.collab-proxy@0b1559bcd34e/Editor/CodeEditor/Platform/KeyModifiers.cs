using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Platform
{
    [Flags]
    internal enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Meta = 8,
    }

    internal static class EventExtensions
    {
        internal static KeyModifiers GetKeyModifiers(this KeyDownEvent e)
        {
            var modifiers = KeyModifiers.None;

            if (e.altKey)
            {
                modifiers |= KeyModifiers.Alt;
            }

            if (e.ctrlKey)
            {
                modifiers |= KeyModifiers.Control;
            }

            if (e.shiftKey)
            {
                modifiers |= KeyModifiers.Shift;
            }

            if (e.commandKey)
            {
                modifiers |= KeyModifiers.Meta;
            }

            return modifiers;
        }
    }
}