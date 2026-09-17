using UnityEditor.IMGUI.Controls;

using PlasticGui.WorkspaceWindow.Diff;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal class ClientDiffTreeViewItem : TreeViewItem
    {
        internal ClientDiffInfo Difference { get; private set; }

        internal ClientDiffTreeViewItem(
            int id, int depth, ClientDiffInfo diff)
            : base(id, depth)
        {
            Difference = diff;

            displayName = diff.PathString;
        }
    }
}
