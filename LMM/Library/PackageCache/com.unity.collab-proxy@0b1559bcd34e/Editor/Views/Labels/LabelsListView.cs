using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;

using Time = Codice.Client.Common.Time;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Labels
{
    internal class LabelsListView :
        PlasticTreeView,
        IPlasticTable<object>
    {
        internal GenericMenu Menu { get { return mMenu.Menu; } }

        internal LabelsListView(
            LabelsListHeaderState headerState,
            List<string> columnNames,
            LabelsViewMenu menu,
            IGetRepositorySpec getRepositorySpec,
            Action delayedSelectionChangedAction,
            Action doubleClickAction,
            Action<IEnumerable<object>> afterItemsChangedAction)
        {
            mColumnNames = columnNames;
            mMenu = menu;
            mGetRepositorySpec = getRepositorySpec;
            mDelayedSelectionChangedAction = delayedSelectionChangedAction;
            mDoubleClickAction = doubleClickAction;
            mAfterItemsChangedAction = afterItemsChangedAction;

            multiColumnHeader = new MultiColumnHeader(headerState);
            multiColumnHeader.canSort = true;
            multiColumnHeader.sortingChanged += SortingChanged;

            mDelayedFilterAction = new DelayedActionBySecondsRunner(
                DelayedSearchChanged, UnityConstants.SEARCH_DELAYED_INPUT_ACTION_INTERVAL);

            mDelayedSelectionRunner = new DelayedActionBySecondsRunner(
                DelayedSelectionChanged, UnityConstants.SELECTION_DELAYED_INPUT_ACTION_INTERVAL);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            mDelayedSelectionRunner.Run();
        }

        protected override IList<TreeViewItem> BuildRows(
            TreeViewItem rootItem)
        {
            if (mQueryResult == null)
            {
                ClearRows(rootItem, mRows);

                return mRows;
            }

            RegenerateRows(
                mListViewItemIds,
                mQueryResult.GetObjects(),
                rootItem,
                mRows);

            return mRows;
        }

        protected override void SearchChanged(string newSearch)
        {
            mDelayedFilterAction.Run();
        }

        protected override void ContextClicked()
        {
            mMenu.Popup();
            Repaint();
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            if (!HasKeyboardFocus())
                return;

            Event e = Event.current;

            if (e.type != EventType.KeyDown)
                return;

            bool isProcessed = mMenu.ProcessKeyActionIfNeeded(e);

            if (isProcessed)
                e.Use();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is LabelListViewItem)
            {
                LabelListViewItem labelListViewItem = (LabelListViewItem)args.item;

                LabelsListViewItemGUI(
                    mQueryResult,
                    rowHeight,
                    labelListViewItem,
                    args,
                    Repaint);
                return;
            }

            base.RowGUI(args);
        }

        protected override void DoubleClickedItem(int id)
        {
            if (GetSelection().Count != 1)
                return;

            mDoubleClickAction();
        }

        internal void Refilter()
        {
            if (mQueryResult == null)
                return;

            Filter filter = new Filter(searchString);
            mQueryResult.ApplyFilter(filter, mColumnNames);
        }

        internal void Sort()
        {
            if (mQueryResult == null)
                return;

            int sortedColumnIdx = multiColumnHeader.state.sortedColumnIndex;
            bool sortAscending = multiColumnHeader.IsSortedAscending(sortedColumnIdx);

            mQueryResult.Sort(
                mColumnNames[sortedColumnIdx],
                sortAscending);
        }

        internal List<RepositorySpec> GetSelectedRepositories()
        {
            List<RepositorySpec> result = new List<RepositorySpec>();

            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count == 0)
                return result;

            foreach (KeyValuePair<object, int> item
                     in mListViewItemIds.GetInfoItems())
            {
                if (!selectedIds.Contains(item.Value))
                    continue;

                RepositorySpec repSpec =
                    mQueryResult.GetRepositorySpec(item.Key);
                result.Add(repSpec);
            }

            return result;
        }

        internal List<RepObjectInfo> GetSelectedRepObjectInfos()
        {
            List<RepObjectInfo> result = new List<RepObjectInfo>();

            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count == 0)
                return result;

            foreach (KeyValuePair<object, int> item
                     in mListViewItemIds.GetInfoItems())
            {
                if (!selectedIds.Contains(item.Value))
                    continue;

                RepObjectInfo repObjectInfo =
                    mQueryResult.GetRepObjectInfo(item.Key);
                result.Add(repObjectInfo);
            }

            return result;
        }

        internal List<object> GetSelectedVisibleItems()
        {
            List<object> result = new List<object>();

            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count == 0)
                return result;

            foreach (TreeViewItem row in mRows)
            {
                if (!selectedIds.Contains(row.id))
                    continue;

                result.Add(((LabelListViewItem)row).ObjectInfo);
            }

            return result;
        }

        internal void SelectRepObjectInfos(
            List<RepObjectInfo> repObjectsToSelect)
        {
            List<int> idsToSelect = new List<int>();

            foreach (RepObjectInfo repObjectInfo in repObjectsToSelect)
            {
                int repObjectInfoId = GetTreeIdForItem(repObjectInfo);

                if (repObjectInfoId == -1)
                    continue;

                idsToSelect.Add(repObjectInfoId);
            }

            TableViewOperations.SetSelectionAndScroll(this, idsToSelect);
        }

        void IPlasticTable<object>.FillEntriesAndSelectRows(
            IList<object> entries,
            List<object> entriesToSelect,
            string currentFilter)
        {
            mListViewItemIds.Clear();

            List<RepObjectInfo> labelsToSelect = LabelsSelection.GetLabelsToSelect(
                this, entriesToSelect);

            int defaultRow = TableViewOperations.GetFirstSelectedRow(this);

            mQueryResult = new ViewQueryResult(
                EnumQueryObjectType.Marker, entries, mGetRepositorySpec.Get());

            UpdateResults();

            LabelsSelection.SelectLabels(this, labelsToSelect, defaultRow);
        }

        void DelayedSearchChanged()
        {
            UpdateResults();

            TableViewOperations.ScrollToSelection(this);
        }

        void SortingChanged(MultiColumnHeader multiColumnHeader)
        {
            Sort();

            Reload();
        }

        void DelayedSelectionChanged()
        {
            if (!HasSelection())
                return;

            mDelayedSelectionChangedAction();
        }

        void UpdateResults()
        {
            Refilter();

            Sort();

            mAfterItemsChangedAction(mQueryResult.GetObjects());

            Reload();
        }

        int GetTreeIdForItem(RepObjectInfo repObjectInfo)
        {
            foreach (KeyValuePair<object, int> item in mListViewItemIds.GetInfoItems())
            {
                RepObjectInfo currentRepObjectInfo =
                    mQueryResult.GetRepObjectInfo(item.Key);

                if (!currentRepObjectInfo.Equals(repObjectInfo))
                    continue;

                if (!currentRepObjectInfo.GUID.Equals(repObjectInfo.GUID))
                    continue;

                return item.Value;
            }

            return -1;
        }

        static void RegenerateRows(
            ListViewItemIds<object> listViewItemIds,
            List<object> objectInfos,
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            ClearRows(rootItem, rows);

            if (objectInfos.Count == 0)
                return;

            foreach (object objectInfo in objectInfos)
            {
                int objectId;
                if (!listViewItemIds.TryGetInfoItemId(objectInfo, out objectId))
                    objectId = listViewItemIds.AddInfoItem(objectInfo);

                LabelListViewItem labelListViewItem =
                    new LabelListViewItem(objectId, objectInfo);

                rootItem.AddChild(labelListViewItem);
                rows.Add(labelListViewItem);
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

        static void LabelsListViewItemGUI(
            ViewQueryResult queryResult,
            float rowHeight,
            LabelListViewItem item,
            RowGUIArgs args,
            Action avatarLoadedAction)
        {
            for (int visibleColumnIdx = 0; visibleColumnIdx < args.GetNumVisibleColumns(); visibleColumnIdx++)
            {
                Rect cellRect = args.GetCellRect(visibleColumnIdx);

                if (visibleColumnIdx == 0)
                {
                    cellRect.x += UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;
                    cellRect.width -= UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;
                }

                LabelsListColumn column =
                    (LabelsListColumn)args.GetColumn(visibleColumnIdx);

                LabelsListViewItemCellGUI(
                    cellRect,
                    rowHeight,
                    queryResult,
                    item,
                    column,
                    avatarLoadedAction,
                    args.selected,
                    args.focused);
            }
        }

        static void LabelsListViewItemCellGUI(
            Rect rect,
            float rowHeight,
            ViewQueryResult queryResult,
            LabelListViewItem item,
            LabelsListColumn column,
            Action avatarLoadedAction,
            bool isSelected,
            bool isFocused)
        {
            RepObjectInfo objectInfo = queryResult.GetRepObjectInfo(item.ObjectInfo);

            string columnText = RepObjectInfoView.GetColumnText(
                queryResult.GetRepositorySpec(item.ObjectInfo),
                objectInfo,
                LabelsListHeaderState.GetColumnName(column));

            if (column == LabelsListColumn.Name)
            {
                DrawTreeViewItem.ForItemCell(
                    rect,
                    rowHeight,
                    -1,
                    Images.GetLabelIcon(),
                    null,
                    null,
                    columnText,
                    isSelected,
                    isFocused,
                    false,
                    false);
                return;
            }

            if (column == LabelsListColumn.CreatedBy)
            {
                DrawTreeViewItem.ForItemCell(
                    rect,
                    rowHeight,
                    -1,
                    GetAvatar.ForEmail(columnText, avatarLoadedAction),
                    null,
                    null,
                    columnText,
                    isSelected,
                    isFocused,
                    false,
                    false);
                return;
            }

            if (column == LabelsListColumn.CreationDate)
            {
                DrawTreeViewItem.ForLabel(
                    rect,
                    new GUIContent(Time.GetLongTimeAgoString(objectInfo.LocalTimeStamp), columnText),
                    isSelected,
                    isFocused,
                    false);
                return;
            }

            if (column == LabelsListColumn.Branch)
            {
                DrawTreeViewItem.ForSecondaryLabel(
                    rect,
                    columnText,
                    isSelected,
                    isFocused,
                    false,
                    DrawTreeViewItem.TextTrimming.Path);
                return;
            }

            DrawTreeViewItem.ForLabel(
                rect, columnText, isSelected, isFocused, false);
        }

        ListViewItemIds<object> mListViewItemIds = new ListViewItemIds<object>();

        ViewQueryResult mQueryResult;

        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly Action<IEnumerable<object>> mAfterItemsChangedAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;
        readonly Action mDoubleClickAction;
        readonly Action mDelayedSelectionChangedAction;
        readonly IGetRepositorySpec mGetRepositorySpec;
        readonly LabelsViewMenu mMenu;
        readonly List<string> mColumnNames;
    }
}
