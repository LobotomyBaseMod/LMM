using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.BrowseRepository;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.BrowseRepository
{
    internal class BrowseRepositoryViewItem : TreeViewItem
    {
        internal BrowseRepositoryTreeNode TreeNode { get; private set; }

        internal BrowseRepositoryViewItem(
            int id,
            BrowseRepositoryTreeNode treeNode,
            int depth)
            : base(id, depth)
        {
            TreeNode = treeNode;
            displayName = treeNode.RelativePath;
        }
    }
}
