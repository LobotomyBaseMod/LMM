using Codice.CM.Common;
using UnityEditor.IMGUI.Controls;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Locks
{
    internal sealed class LocksListViewItem : TreeViewItem
    {
        internal LockInfo LockInfo { get; private set; }

        internal LocksListViewItem(int id, LockInfo lockInfo)
            : base(id, 1)
        {
            LockInfo = lockInfo;

            displayName = id.ToString();
        }
    }
}
