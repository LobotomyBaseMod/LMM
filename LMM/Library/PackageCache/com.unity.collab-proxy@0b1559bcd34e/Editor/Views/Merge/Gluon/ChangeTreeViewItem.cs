using UnityEditor.IMGUI.Controls;

using PlasticGui.Gluon.WorkspaceWindow.Views.IncomingChanges;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.IncomingChanges.Gluon
{
    internal class ChangeTreeViewItem : TreeViewItem
    {
        internal IncomingChangeInfo ChangeInfo { get; private set; }

        internal ChangeTreeViewItem(int id, IncomingChangeInfo change)
            : base(id, 1)
        {
            ChangeInfo = change;

            displayName = change.GetPathString();
        }
    }
}
