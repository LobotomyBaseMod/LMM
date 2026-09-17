using UnityEditor.IMGUI.Controls;

using Codice.CM.Common;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace
{
    internal class CollaboratorsListViewItem : TreeViewItem
    {
        internal SecurityMember User { get; private set; }

        internal CollaboratorsListViewItem(int id, SecurityMember user) : base(id)
        {
            User = user;
            displayName = user.Name;
        }
    }
}
