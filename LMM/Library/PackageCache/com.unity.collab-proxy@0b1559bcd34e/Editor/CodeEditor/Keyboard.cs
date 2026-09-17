using System.Runtime.InteropServices;

using UnityEngine;

namespace Unity.CodeEditor
{
    internal static class Keyboard
    {
        internal static bool IsKeyPressed(Event e, KeyCode keyCode)
        {
            if (e == null)
                return false;

            return e.type == EventType.KeyDown
                && e.keyCode == keyCode;
        }

        internal static bool IsControlOrCommandKeyPressed(Event e)
        {
            if (e == null)
                return false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return e.type == EventType.KeyDown && e.command;

            return e.type == EventType.KeyDown && e.control;
        }
    }
}