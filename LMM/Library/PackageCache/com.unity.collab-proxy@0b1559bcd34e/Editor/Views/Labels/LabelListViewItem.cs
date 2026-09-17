using UnityEditor.IMGUI.Controls;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Labels
{
    class LabelListViewItem : TreeViewItem
    {
        internal object ObjectInfo { get; private set; }

        internal LabelListViewItem(int id, object objectInfo)
            : base(id, 1)
        {
            ObjectInfo = objectInfo;

            displayName = id.ToString();
        }
    }
}
