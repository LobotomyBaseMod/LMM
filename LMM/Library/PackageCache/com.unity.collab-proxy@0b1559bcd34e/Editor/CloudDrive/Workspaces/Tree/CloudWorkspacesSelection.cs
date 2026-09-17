using System.Collections.Generic;

using PlasticGui.WorkspaceWindow.Items;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.Tree
{
    internal static class CloudWorkspacesSelection
    {
        internal static List<ExpandedTreeNode> GetSelectedNodes(
            CloudWorkspacesTreeView treeView)
        {
            return treeView.GetSelectedNodes();
        }
    }
}
