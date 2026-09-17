using System;

using UnityEditor;

#if !UNITY_6000_3_OR_NEWER
using EditorUtility = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorUtility;
using Menu = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.Menu;
#endif

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class HandleMenuItem
    {
        internal static void AddMenuItem(
            string name,
            int priority,
            Action execute,
            Func<bool> validate)
        {
            AddMenuItem(name, string.Empty, priority, execute, validate);
        }

        internal static void AddMenuItem(
            string name,
            string shortcut,
            int priority,
            Action execute,
            Func<bool> validate)
        {
            Menu.AddMenuItem(name, shortcut, false, priority, execute, validate);
        }

        internal static void RemoveMenuItem(string name)
        {
            Menu.RemoveMenuItem(name);
        }

        internal static void UpdateAllMenus()
        {
            EditorUtility.Internal_UpdateAllMenus();
        }

        internal static bool GetEnabled(string menuPath)
        {
            return Menu.GetEnabled(menuPath);
        }
    }
}
