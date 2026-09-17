using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.Items;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.Tree
{
    internal class CloudWorkspacesTreeViewItem : TreeViewItem
    {
        internal ExpandedTreeNode ExpandedTreeNode { get; private set; }
        internal bool IsExpandable { get; private set; }

        internal CloudWorkspacesTreeViewItem(
            int id, ExpandedTreeNode expandedTreeNode, bool isExpandable, int depth)
            : base(id, depth)
        {
            ExpandedTreeNode = expandedTreeNode;
            IsExpandable = isExpandable;
            displayName = expandedTreeNode.RelativePath;
        }
    }

    internal class CloudWorkspacesLabelTreeViewItem : TreeViewItem
    {
        internal string Label { get; private set; }

        internal CloudWorkspacesLabelTreeViewItem(int id, string label) : base(id, 0)
        {
            Label = label;
            displayName = label;
        }
    }
}
