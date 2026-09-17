using System.Collections.Generic;

using UnityGenericMenu = UnityEditor.GenericMenu;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class GenericMenuExtensions
    {
        internal static List<GenericMenu.MenuItem> menuItems(this UnityGenericMenu genericMenu)
        {
            return GetMenuItems(genericMenu);
        }

        internal delegate List<GenericMenu.MenuItem> GetMenuItemsDelegate(UnityGenericMenu genericMenu);

        internal static GetMenuItemsDelegate GetMenuItems { get; set; }
    }
}
