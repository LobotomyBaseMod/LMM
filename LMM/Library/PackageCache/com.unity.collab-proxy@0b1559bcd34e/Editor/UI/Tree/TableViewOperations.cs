using System.Collections.Generic;

using UnityEditor.IMGUI.Controls;
#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.UI.Tree
{
    internal static class TableViewOperations
    {
        internal static int GetFirstSelectedRow(
            TreeView treeView)
        {
            IList<int> selectedIds = treeView.GetSelection();

            if (selectedIds.Count == 0)
                return -1;

            return selectedIds[0];
        }

        internal static void SelectFirstRow(
            TreeView treeView)
        {
            IList<TreeViewItem> rows = treeView.GetRows();

            if (rows.Count == 0)
                return;

            SetSelectionAndScroll(
                treeView, new List<int> { rows[0].id });
        }

        internal static void SelectDefaultRow(
            TreeView treeView, int defaultRow)
        {
            int rowCount = treeView.GetRows().Count;

            if (defaultRow == -1 || rowCount == 0)
                return;

            if (defaultRow >= rowCount)
                defaultRow = rowCount - 1;

            SetSelectionAndScroll(
                treeView, new List<int> { defaultRow });
        }

        internal static void SetSelectionAndScroll(
            TreeView treeView, List<int> idsToSelect)
        {
            treeView.SetSelection(
                idsToSelect,
                TreeViewSelectionOptions.FireSelectionChanged |
                TreeViewSelectionOptions.RevealAndFrame);
        }

        internal static void ScrollToSelection(
            TreeView treeView)
        {
            if (!treeView.HasSelection())
                return;

            int itemId = treeView.GetSelection()[0];

            if (!IsVisible(itemId, treeView))
                return;

            treeView.FrameItem(itemId);
        }

        static bool IsVisible(int id, TreeView treeView)
        {
            foreach (TreeViewItem item in treeView.GetRows())
            {
                if (item.id == id)
                    return true;
            }

            return false;
        }
    }
}
