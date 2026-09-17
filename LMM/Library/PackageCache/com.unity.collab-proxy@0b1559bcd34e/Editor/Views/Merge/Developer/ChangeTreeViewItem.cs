using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.Merge;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Merge.Developer
{
    internal class ChangeTreeViewItem : TreeViewItem
    {
        internal MergeChangeInfo ChangeInfo { get; private set; }

        internal ChangeTreeViewItem(int id, MergeChangeInfo change)
            : base(id, 1)
        {
            ChangeInfo = change;

            displayName = id.ToString();
        }
    }
}
