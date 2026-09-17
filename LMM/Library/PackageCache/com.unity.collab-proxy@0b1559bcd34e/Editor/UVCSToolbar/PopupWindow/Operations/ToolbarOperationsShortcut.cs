using UnityEditor.ShortcutManagement;
using UnityEngine;

using Codice.Utils;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.Operations
{
    internal static class ToolbarOperationsShortcut
    {
        internal const KeyCode PendingChangesShortcutKey = KeyCode.K;
        internal const ShortcutModifiers PendingChangesShortcutModifiers =
            ShortcutModifiers.Alt | ShortcutModifiers.Action;

        internal static string GetPendingChangesShortcutString()
        {
            if (PlatformIdentifier.IsMac())
                return "⌥ ⌘ K";

            return "Alt+Ctrl+K";
        }

        internal const KeyCode IncomingChangesShortcutKey = KeyCode.I;
        internal const ShortcutModifiers IncomingChangesShortcutModifiers =
            ShortcutModifiers.Alt | ShortcutModifiers.Action;

        internal static string GetIncomingChangesShortcutString()
        {
            if (PlatformIdentifier.IsMac())
                return "⌥ ⌘ I";

            return "Alt+Ctrl+I";
        }
    }
}
