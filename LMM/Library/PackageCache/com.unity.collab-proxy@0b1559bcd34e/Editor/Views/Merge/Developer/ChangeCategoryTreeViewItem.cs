using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.Merge;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Merge.Developer
{
    internal class ChangeCategoryTreeViewItem : TreeViewItem
    {
        internal MergeChangesCategory Category { get; private set; }

        internal ChangeCategoryTreeViewItem(int id, MergeChangesCategory category)
            : base(id, 0, category.CategoryType.ToString())
        {
            Category = category;
        }
    }
}
