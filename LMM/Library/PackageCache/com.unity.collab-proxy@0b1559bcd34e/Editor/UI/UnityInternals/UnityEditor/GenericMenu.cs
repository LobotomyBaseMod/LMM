using UnityEngine;

using UnityGenericMenu = UnityEditor.GenericMenu;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal class GenericMenu
    {
        internal class MenuItem
        {
            internal object InternalObject;

            internal GUIContent content => GetContent(this);
            internal UnityGenericMenu.MenuFunction func => GetFunc(this);

            internal MenuItem(object menuItem)
            {
                InternalObject = menuItem;
            }

            internal delegate GUIContent GetContentDelegate(MenuItem menuItem);

            internal static GetContentDelegate GetContent { get; set; }

            internal delegate UnityGenericMenu.MenuFunction GetFuncDelegate(MenuItem menuItem);

            internal static GetFuncDelegate GetFunc { get; set; }
        }
    }
}
