using System.Collections.Generic;

using Codice.CM.Common;
using Unity.PlasticSCM.Editor.UI.Tree;
using UnityEditor.IMGUI.Controls;

#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Locks
{
    internal sealed class LocksSelector
    {
        internal LocksSelector(TreeView treeView, ListViewItemIds<LockInfo> listViewItemIds)
        {
            mTreeView = treeView;
            mListViewItemIds = listViewItemIds;
        }

        internal void SaveSelectedLocks()
        {
            mSelectedLocks = GetSelectedLocks();
        }

        internal List<LockInfo> GetSelectedLocks()
        {
            var result = new List<LockInfo>();

            var selectedIds = mTreeView.GetSelection();

            if (selectedIds.Count == 0)
            {
                return result;
            }

            foreach (var item in mListViewItemIds.GetInfoItems())
            {
                if (selectedIds.Contains(item.Value))
                {
                    result.Add(item.Key);
                }
            }

            return result;
        }

        internal void RestoreSelectedLocks()
        {
            if (mSelectedLocks == null || mSelectedLocks.Count == 0)
            {
                TableViewOperations.SelectFirstRow(mTreeView);
                return;
            }

            SelectLockItems(mSelectedLocks);

            if (!mTreeView.HasSelection())
            {
                TableViewOperations.SelectFirstRow(mTreeView);
            }
        }

        void SelectLockItems(List<LockInfo> locksToSelect)
        {
            var idsToSelect = new List<int>();
            var rows = mTreeView.GetRows();
            var existingIds = new HashSet<int>();

            foreach (var row in rows)
            {
                existingIds.Add(row.id);
            }

            foreach (var lockInfo in locksToSelect)
            {
                var lockInfoId = GetTreeIdForItem(lockInfo);

                if (lockInfoId == -1)
                {
                    continue;
                }

                if (!existingIds.Contains(lockInfoId))
                {
                    continue;
                }

                idsToSelect.Add(lockInfoId);
            }

            if (idsToSelect.Count == 0)
                return;

            TableViewOperations.SetSelectionAndScroll(mTreeView, idsToSelect);
        }

        int GetTreeIdForItem(LockInfo lockInfo)
        {
            foreach (var item in mListViewItemIds.GetInfoItems())
            {
                if (!lockInfo.ItemGuid.Equals(item.Key.ItemGuid))
                {
                    continue;
                }

                return item.Value;
            }

            return -1;
        }

        List<LockInfo> mSelectedLocks;

        readonly TreeView mTreeView;
        readonly ListViewItemIds<LockInfo> mListViewItemIds;
    }
}
