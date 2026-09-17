using UnityEditor.IMGUI.Controls;
using UnityEngine;
#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
#endif
namespace Unity.PlasticSCM.Editor.UI
{
    internal static class DrawSearchField
    {
        internal static void For(
            SearchField searchField,
            TreeView treeView,
            float width)
        {
            treeView.searchString = searchField.OnToolbarGUI(
                treeView.searchString, GUILayout.MaxWidth(width / 2f));
        }
    }
}
