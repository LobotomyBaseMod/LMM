using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.PendingChanges;
using PlasticGui.WorkspaceWindow.PendingChanges.Changelists;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal class ChangelistTreeViewItem : TreeViewItem
    {
        internal ChangelistNode Changelist { get; private set; }

        internal ChangelistTreeViewItem(int id, ChangelistNode changelist)
            : base(id, 0)
        {
            Changelist = changelist;
        }
    }
}
