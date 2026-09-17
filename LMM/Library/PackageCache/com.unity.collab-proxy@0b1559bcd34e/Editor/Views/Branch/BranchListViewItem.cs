using UnityEditor.IMGUI.Controls;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Branches
{
    class BranchListViewItem : TreeViewItem
    {
        internal object ObjectInfo { get; private set; }

        internal BranchListViewItem(int id, object objectInfo)
            : base(id, 1)
        {
            ObjectInfo = objectInfo;

            displayName = id.ToString();
        }
    }
}
