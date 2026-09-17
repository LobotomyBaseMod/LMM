using System;

using UnityMenu = UnityEditor.Menu;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class Menu
    {
        internal delegate void AddMenuItemDelegate(
            string name,
            string shortcut,
            bool @checked,
            int priority,
            Action execute,
            Func<bool> validate);

        internal static AddMenuItemDelegate AddMenuItem { get; set; }

        internal delegate void RemoveMenuItemDelegate(string name);

        internal static RemoveMenuItemDelegate RemoveMenuItem { get; set; }

        internal static bool GetEnabled(string menuPath)
        {
            return UnityMenu.GetEnabled(menuPath);
        }
    }
}
