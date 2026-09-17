using UnityEditor.IMGUI.Controls;

using Codice.CM.Common;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.UI.Errors
{
    internal class ErrorListViewItem : TreeViewItem
    {
        internal ErrorMessage ErrorMessage { get; private set; }

        internal ErrorListViewItem(int id, ErrorMessage errorMessage)
            : base(id, 0)
        {
            ErrorMessage = errorMessage;

            displayName = errorMessage.Path;
        }
    }
}
