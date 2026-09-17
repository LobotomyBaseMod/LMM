using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Diff.Type;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Changesets;
using Unity.PlasticSCM.Editor.Views.Labels.Dialogs;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal class ChangesetByChangesetListView :
        PlasticTreeView,
        IPlasticTable<ChangesetInfo>
    {
        internal ChangesetByChangesetListView(
            ExploreChangesets exploreChangesets,
            Func<SEID, string> resolveUserName,
            Action delayedSelectionChangedAction): base(showCustomBackground: false)
        {
            mExploreChangesets = exploreChangesets;
            mResolveUserName = resolveUserName;
            mDelayedSelectionChangedAction = delayedSelectionChangedAction;

            mDelayedFilterAction = new DelayedActionBySecondsRunner(
                DelayedSearchChanged, UnityConstants.SEARCH_DELAYED_INPUT_ACTION_INTERVAL);

            mDelayedSelectionRunner = new DelayedActionBySecondsRunner(
                DelayedSelectionChanged, UnityConstants.SELECTION_DELAYED_INPUT_ACTION_INTERVAL);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            mDelayedSelectionRunner.Run();
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem rootItem)
        {
            if (mChangesets == null)
            {
                ClearRows(rootItem, mRows);

                return mRows;
            }

            RegenerateRows(
                mListViewItemIds,
                mChangesets,
                rootItem, mRows);

            return mRows;
        }

        protected override void SearchChanged(string newSearch)
        {
            // HACK: if CreateLabelDialog is open, update the results
            // since the mDelayedFilterAction does not work properly
            if (EditorWindow.HasOpenInstances<CreateLabelDialog>())
            {
                Refilter();
                Reload();
                return;
            }

            mDelayedFilterAction.Run();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is ChangesetListViewItem)
            {
                ChangesetListViewItem changesetListViewItem = (ChangesetListViewItem)args.item;

                ChangesetsListViewItemGUI(
                    rowHeight,
                    changesetListViewItem,
                    args,
                    false,
                    false,
                    mResolveUserName,
                    Repaint);
                return;
            }

            base.RowGUI(args);
        }

        internal List<ChangesetInfo> GetSelectedChangesetInfos()
        {
            List<ChangesetInfo> result = new List<ChangesetInfo>();

            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count == 0)
                return result;

            foreach (KeyValuePair<object, int> item
                     in mListViewItemIds.GetInfoItems())
            {
                if (!selectedIds.Contains(item.Value))
                    continue;

                result.Add((ChangesetInfo)item.Key);
            }

            return result;
        }

        void Refilter()
        {
            mExploreChangesets.ApplyFilter(searchString);
        }

        void SelectChangesetInfos(
            List<ChangesetInfo> changesetsToSelect)
        {
            List<int> idsToSelect = new List<int>();

            foreach (ChangesetInfo changesetInfo in changesetsToSelect)
            {
                int changesetId = GetTreeIdForItem(changesetInfo);

                if (changesetId == -1)
                    continue;

                idsToSelect.Add(changesetId);
            }

            TableViewOperations.SetSelectionAndScroll(this, idsToSelect);
        }

        void IPlasticTable<ChangesetInfo>.FillEntriesAndSelectRows(
            IList<ChangesetInfo> entries,
            List<ChangesetInfo> entriesToSelect,
            string currentFilter)
        {
            mListViewItemIds.Clear();

            List<ChangesetInfo> changesetsToSelect =
                entriesToSelect == null || entriesToSelect.Count == 0 ?
                    GetSelectedChangesetInfos() :
                    entriesToSelect;

            int defaultRow = TableViewOperations.GetFirstSelectedRow(this);

            mChangesets = new List<ChangesetInfo>(entries);

            Reload();

            SelectChangesets(changesetsToSelect, defaultRow);
        }

        void SelectChangesets(
            List<ChangesetInfo> csetsToSelect,
            int defaultRow)
        {
            if (csetsToSelect == null || csetsToSelect.Count == 0)
            {
                TableViewOperations.SelectFirstRow(this);
                return;
            }

            SelectChangesetInfos(csetsToSelect);

            if (HasSelection())
                return;

            TableViewOperations.SelectDefaultRow(this, defaultRow);

            if (HasSelection())
                return;

            TableViewOperations.SelectFirstRow(this);
        }

        void DelayedSearchChanged()
        {
            Refilter();

            Reload();

            TableViewOperations.ScrollToSelection(this);

            mDelayedSelectionChangedAction();
        }

        void DelayedSelectionChanged()
        {
            if (!HasSelection())
                return;

            mDelayedSelectionChangedAction();
        }

        int GetTreeIdForItem(ChangesetInfo changesetInfo)
        {
            foreach (KeyValuePair<object, int> item in mListViewItemIds.GetInfoItems())
            {
                if (!item.Key.Equals(changesetInfo))
                    continue;

                if (!((ChangesetInfo)item.Key).GUID.Equals(changesetInfo.GUID))
                    continue;

                return item.Value;
            }

            return -1;
        }

        static void RegenerateRows(
            ListViewItemIds<object> listViewItemIds,
            IList<ChangesetInfo> changesets,
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            ClearRows(rootItem, rows);

            if (changesets.Count == 0)
                return;

            foreach (ChangesetInfo changeset in changesets)
            {
                int changesetId;
                if (!listViewItemIds.TryGetInfoItemId(changeset, out changesetId))
                    changesetId = listViewItemIds.AddInfoItem(changeset);

                ChangesetListViewItem changesetListViewItem =
                    new ChangesetListViewItem(changesetId, changeset);

                rootItem.AddChild(changesetListViewItem);
                rows.Add(changesetListViewItem);
            }
        }

        static void ClearRows(
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            if (rootItem.hasChildren)
                rootItem.children.Clear();

            rows.Clear();
        }

        static void ChangesetsListViewItemGUI(
            float rowHeight,
            ChangesetListViewItem item,
            RowGUIArgs args,
            bool isBoldText,
            bool isMultiColumn,
            Func<SEID, string> resolveUserName,
            Action avatarLoadedAction)
        {
            if (!isMultiColumn)
            {
                ChangesetsListViewItemCellGUI(
                    args.rowRect,
                    rowHeight,
                    item,
                    resolveUserName,
                    avatarLoadedAction,
                    args.selected,
                    args.focused,
                    isBoldText);
                return;
            }

            for (int visibleColumnIdx = 0; visibleColumnIdx < args.GetNumVisibleColumns(); visibleColumnIdx++)
            {
                Rect cellRect = args.GetCellRect(visibleColumnIdx);

                ChangesetsListViewItemCellGUI(
                    cellRect,
                    rowHeight,
                    item,
                    resolveUserName,
                    avatarLoadedAction,
                    args.selected,
                    args.focused,
                    isBoldText);
            }
        }

        static void ChangesetsListViewItemCellGUI(
            Rect rect,
            float rowHeight,
            ChangesetListViewItem item,
            Func<SEID, string> resolveUserName,
            Action avatarLoadedAction,
            bool isSelected,
            bool isFocused,
            bool isBoldText)
        {
            rect.x += UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;
            rect.width -= UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;

            string columnText = CommentFormatter.GetFormattedComment(
                ((ChangesetInfo)item.ObjectInfo).Comment);

            string userName = resolveUserName(((ChangesetInfo)item.ObjectInfo).Owner);

            DrawTreeViewItem.ForItemCell(
                rect,
                rowHeight,
                -1,
                GetAvatar.ForEmail(userName, avatarLoadedAction),
                userName,
                null,
                columnText,
                isSelected,
                isFocused,
                isBoldText,
                false);
        }

        ListViewItemIds<object> mListViewItemIds = new ListViewItemIds<object>();
        List<ChangesetInfo> mChangesets;

        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;
        readonly Action mDelayedSelectionChangedAction;
        readonly Func<SEID, string> mResolveUserName;
        readonly ExploreChangesets mExploreChangesets;
    }
}
