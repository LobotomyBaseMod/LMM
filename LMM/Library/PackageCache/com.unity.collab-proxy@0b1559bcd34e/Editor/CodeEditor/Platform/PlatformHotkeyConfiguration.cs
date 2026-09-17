using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.CodeEditor.Platform
{
    internal sealed class PlatformHotkeyConfiguration
    {
        private static PlatformHotkeyConfiguration s_Current;

        internal static PlatformHotkeyConfiguration Current
        {
            get
            {
                if (s_Current != null)
                    return s_Current;

                s_Current = Init();
                return s_Current;
            }
        }

        static PlatformHotkeyConfiguration Init()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new PlatformHotkeyConfiguration(
                    KeyModifiers.Meta,
                    KeyModifiers.Shift,
                    KeyModifiers.Alt,
                    KeyModifiers.Control,
                    KeyModifiers.Alt);
            }

            return new PlatformHotkeyConfiguration(KeyModifiers.Control);
        }

        PlatformHotkeyConfiguration(KeyModifiers commandModifiers,
            KeyModifiers selectionModifiers = KeyModifiers.Shift,
            KeyModifiers wholeWordTextActionModifiers = KeyModifiers.Control,
            KeyModifiers boxSelectionModifiers = KeyModifiers.Alt,
            KeyModifiers deleteWholeLineModifiers = KeyModifiers.Shift)
        {
            CommandModifiers = commandModifiers;
            SelectionModifiers = selectionModifiers;
            WholeWordTextActionModifiers = wholeWordTextActionModifiers;
            BoxSelectionModifiers = boxSelectionModifiers;
            Copy = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.C, commandModifiers),
                new KeyGesture(KeyCode.Insert, KeyModifiers.Control)
            };
            Cut = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.X, commandModifiers)
            };
            Paste = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.V, commandModifiers),
                new KeyGesture(KeyCode.Insert, KeyModifiers.Shift)
            };
            Undo = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.Z, commandModifiers)
            };
            Redo = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.Y, commandModifiers),
                new KeyGesture(KeyCode.Z, commandModifiers | selectionModifiers)
            };
            SelectAll = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.A, commandModifiers)
            };
            MoveCursorToTheStartOfLine = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.Home)
            };
            MoveCursorToTheEndOfLine = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.End)
            };
            MoveCursorToTheStartOfDocument = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.Home, commandModifiers)
            };
            MoveCursorToTheEndOfDocument = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.End, commandModifiers)
            };
            MoveCursorToTheStartOfLineWithSelection = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.Home, selectionModifiers)
            };
            MoveCursorToTheEndOfLineWithSelection = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.End, selectionModifiers)
            };
            MoveCursorToTheStartOfDocumentWithSelection = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.Home, commandModifiers | selectionModifiers)
            };
            MoveCursorToTheEndOfDocumentWithSelection = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.End, commandModifiers | selectionModifiers)
            };
            Back = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.LeftArrow, KeyModifiers.Alt)
            };
            PageLeft = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.PageUp, KeyModifiers.Shift)
            };
            PageRight = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.PageDown, KeyModifiers.Shift)
            };
            PageUp = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.PageUp)
            };
            PageDown = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.PageDown)
            };
            DeleteWholeLine = new List<KeyGesture>
            {
                new KeyGesture(KeyCode.Delete, deleteWholeLineModifiers)
            };
        }

        internal KeyModifiers CommandModifiers { get; set; }
        internal KeyModifiers WholeWordTextActionModifiers { get; set; }
        internal KeyModifiers SelectionModifiers { get; set; }
        internal KeyModifiers BoxSelectionModifiers { get; set; }
        internal List<KeyGesture> Copy { get; set; }
        internal List<KeyGesture> Cut { get; set; }
        internal List<KeyGesture> Paste { get; set; }
        internal List<KeyGesture> Undo { get; set; }
        internal List<KeyGesture> Redo { get; set; }
        internal List<KeyGesture> SelectAll { get; set; }
        internal List<KeyGesture> MoveCursorToTheStartOfLine { get; set; }
        internal List<KeyGesture> MoveCursorToTheEndOfLine { get; set; }
        internal List<KeyGesture> MoveCursorToTheStartOfDocument { get; set; }
        internal List<KeyGesture> MoveCursorToTheEndOfDocument { get; set; }
        internal List<KeyGesture> MoveCursorToTheStartOfLineWithSelection { get; set; }
        internal List<KeyGesture> MoveCursorToTheEndOfLineWithSelection { get; set; }
        internal List<KeyGesture> MoveCursorToTheStartOfDocumentWithSelection { get; set; }
        internal List<KeyGesture> MoveCursorToTheEndOfDocumentWithSelection { get; set; }
        internal List<KeyGesture> Back { get; set; }
        internal List<KeyGesture> PageUp { get; set; }
        internal List<KeyGesture> PageDown { get; set; }
        internal List<KeyGesture> PageRight { get; set; }
        internal List<KeyGesture> PageLeft { get; set; }
        internal List<KeyGesture> DeleteWholeLine { get; set; }
    }
}