using UnityEditor.IMGUI.Controls;

using PlasticGui;
using PlasticGui.WorkspaceWindow.PendingChanges;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal class ChangeTreeViewItem : TreeViewItem
    {
        internal PendingChangeInfo ChangeInfo { get; private set; }

        internal ChangeTreeViewItem(int id, PendingChangeInfo change, int depth)
            : base(id, depth)
        {
            ChangeInfo = change;

            displayName = change.GetColumnText(PlasticLocalization.GetString(
                PlasticLocalization.Name.ItemColumn));
        }
    }
}
