using System.Reflection;

using UnityEditor.IMGUI.Controls;

#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
#endif

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class TreeViewExtensions
    {
        internal static void RaiseContextClickedItem(this TreeView treeView)
        {
            MethodInfo InternalContextClickedItem = treeView.GetType().GetMethod(
                "ContextClickedItem",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (InternalContextClickedItem == null)
                return;

            InternalContextClickedItem.Invoke(treeView, new object[] { -1 });
        }

        internal static void RaiseContextClicked(this TreeView treeView)
        {
            MethodInfo InternalContextClicked = treeView.GetType().GetMethod(
                "ContextClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (InternalContextClicked == null)
                return;

            InternalContextClicked.Invoke(treeView, null);
        }
    }
}
