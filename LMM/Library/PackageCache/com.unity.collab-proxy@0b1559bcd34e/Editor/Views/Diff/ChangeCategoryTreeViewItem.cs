using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.Diff;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal class ChangeCategoryTreeViewItem : TreeViewItem
    {
        internal ChangeCategory Category { get; private set; }

        internal ChangeCategoryTreeViewItem(
            int id, int depth, ChangeCategory category)
            : base(id, depth, category.GetHeaderText())
        {
            Category = category;
        }
    }
}
