
using UnityEditor.IMGUI.Controls;

using Codice.CM.Common;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.History
{
    internal class HistoryListViewItem : TreeViewItem
    {
        internal RepObjectInfo Revision { get; private set; }

        internal HistoryListViewItem(int id, RepObjectInfo revision)
            : base(id, 1)
        {
            Revision = revision;

            displayName = id.ToString();
        }
    }
}
