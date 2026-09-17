using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.PendingChanges;
using Unity.PlasticSCM.Editor.UI;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.PendingChanges.PendingMergeLinks
{
    internal class MergeLinkListViewItem : TreeViewItem
    {
        internal MountPendingMergeLink MergeLink { get; private set; }

        internal MergeLinkListViewItem(int id, MountPendingMergeLink mergeLink)
            : base(id, 0)
        {
            MergeLink = mergeLink;

            displayName = mergeLink.GetPendingMergeLinkText();
            icon = Images.GetMergeLinkIcon();
        }
    }
}

