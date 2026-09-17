using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Labels.Dialogs;

using Time = Codice.Client.Common.Time;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal class ChangesetsListView :
        PlasticTreeView,
        IPlasticTable<object>
    {
        internal GenericMenu Menu { get { return mMenu.Menu; } }

        internal ChangesetsListView(
            ChangesetsListHeaderState headerState,
            List<string> columnNames,
            ChangesetsViewMenu menu,
            IGetRepositorySpec getRepositorySpec,
            IGetWorkingObject getWorkingObject,
            Action delayedSelectionChangedAction,
            Action doubleClickAction,
            Action<IEnumerable<object>> afterItemsChangedAction)
        {
            mColumnNames = columnNames;
            mMenu = menu;
            mGetRepositorySpec = getRepositorySpec;
            mGetWorkingObject = getWorkingObject;
            mDelayedSelectionChangedAction = delayedSelectionChangedAction;
            mDoubleClickAction = doubleClickAction;
            mAfterItemsChangedAction = afterItemsChangedAction;

            if (headerState != null)
            {
                multiColumnHeader = new MultiColumnHeader(headerState);
                multiColumnHeader.canSort = true;
                multiColumnHeader.sortingChanged += SortingChanged;
            }

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
                rootItem, mRows);

            return mRows;
        }

        protected override void SearchChanged(string newSearch)
        {
            // HACK: if CreateLabelDialog is open, update the results
            // since the mDelayedFilterAction does not work properly
            if (EditorWindow.HasOpenInstances<CreateLabelDialog>())
            {
                UpdateResults();
                return;
            }

            mDelayedFilterAction.Run();
        }

        protected override void ContextClickedItem(int id)
        {
            if (mMenu == null)
                return;

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

            if (mMenu == null)
                return;

            bool isProcessed = mMenu.ProcessKeyActionIfNeeded(e);

            if (isProcessed)
                e.Use();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is ChangesetListViewItem)
            {
                ChangesetListViewItem changesetListViewItem = (ChangesetListViewItem)args.item;

                ChangesetsListViewItemGUI(
                    mQueryResult,
                    rowHeight,
                    changesetListViewItem,
                    args,
                    RepObjectInfoView.IsHighlighted(
                        changesetListViewItem.ObjectInfo, mGetWorkingObject.Get()),
                    multiColumnHeader != null,
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

            int sortedColumnIdx = multiColumnHeader != null ?
                multiColumnHeader.state.sortedColumnIndex : (int)ChangesetsListColumn.CreationDate;
            bool sortAscending = multiColumnHeader != null ?
                multiColumnHeader.IsSortedAscending(sortedColumnIdx) : false;

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

            List<RepObjectInfo> changesetsToSelect = ChangesetsSelection.GetChangesetsToSelect(
                this, entriesToSelect);

            int defaultRow = TableViewOperations.GetFirstSelectedRow(this);

            mQueryResult = new ViewQueryResult(
                EnumQueryObjectType.Changeset, entries, mGetRepositorySpec.Get());

            UpdateResults();

            ChangesetsSelection.SelectChangesets(this, changesetsToSelect, defaultRow);
        }

        void DelayedSearchChanged()
        {
            UpdateResults();

            TableViewOperations.ScrollToSelection(this);

            mDelayedSelectionChangedAction();
        }

        void DelayedSelectionChanged()
        {
            if (!HasSelection())
                return;

            mDelayedSelectionChangedAction();
        }

        void SortingChanged(MultiColumnHeader multiColumnHeader)
        {
            Sort();

            Reload();
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

                ChangesetListViewItem changesetListViewItem =
                    new ChangesetListViewItem(objectId, objectInfo);

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
            ViewQueryResult queryResult,
            float rowHeight,
            ChangesetListViewItem item,
            RowGUIArgs args,
            bool isBoldText,
            bool isMultiColumn,
            Action avatarLoadedAction)
        {
            if (!isMultiColumn)
            {
                ChangesetsListViewItemCellGUI(
                    args.rowRect,
                    rowHeight,
                    queryResult,
                    item,
                    ChangesetsListColumn.Comment,
                    avatarLoadedAction,
                    args.selected,
                    args.focused,
                    isBoldText);
                return;
            }

            for (int visibleColumnIdx = 0; visibleColumnIdx < args.GetNumVisibleColumns(); visibleColumnIdx++)
            {
                Rect cellRect = args.GetCellRect(visibleColumnIdx);

                ChangesetsListColumn column =
                    (ChangesetsListColumn)args.GetColumn(visibleColumnIdx);

                ChangesetsListViewItemCellGUI(
                    cellRect,
                    rowHeight,
                    queryResult,
                    item,
                    column,
                    avatarLoadedAction,
                    args.selected,
                    args.focused,
                    isBoldText);
            }
        }

        static void ChangesetsListViewItemCellGUI(
            Rect rect,
            float rowHeight,
            ViewQueryResult queryResult,
            ChangesetListViewItem item,
            ChangesetsListColumn column,
            Action avatarLoadedAction,
            bool isSelected,
            bool isFocused,
            bool isBoldText)
        {
            if (column == 0)
            {
                rect.x += UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;
                rect.width -= UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;
            }

            RepositorySpec repSpec = queryResult.GetRepositorySpec(item.ObjectInfo);
            RepObjectInfo repObjectInfo = queryResult.GetRepObjectInfo(item.ObjectInfo);

            string columnText = RepObjectInfoView.GetColumnText(
                repSpec, repObjectInfo, ChangesetsListHeaderState.GetColumnName(column));

            string userName = PlasticGui.Plastic.API.GetUserName(repSpec.Server, repObjectInfo.Owner);

            if (column == ChangesetsListColumn.Comment)
            {
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
                return;
            }


            if (column == ChangesetsListColumn.Branch ||
                column == ChangesetsListColumn.Guid)
            {
                DrawTreeViewItem.ForSecondaryLabel(
                    rect,
                    columnText,
                    isSelected,
                    isFocused,
                    isBoldText,
                    column == ChangesetsListColumn.Branch ?
                        DrawTreeViewItem.TextTrimming.Path :
                        DrawTreeViewItem.TextTrimming.None);
                return;
            }

            if (column == ChangesetsListColumn.CreationDate)
            {
                DrawTreeViewItem.ForLabel(
                    rect,
                    new GUIContent(
                        Time.GetLongTimeAgoString(repObjectInfo.LocalTimeStamp),
                        columnText),
                    isSelected,
                    isFocused,
                    isBoldText);
                return;
            }

            DrawTreeViewItem.ForLabel(
                rect, columnText, isSelected, isFocused, isBoldText);
        }

        ListViewItemIds<object> mListViewItemIds = new ListViewItemIds<object>();

        ViewQueryResult mQueryResult;

        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;
        readonly Action<IEnumerable<object>> mAfterItemsChangedAction;
        readonly Action mDoubleClickAction;
        readonly Action mDelayedSelectionChangedAction;
        readonly IGetWorkingObject mGetWorkingObject;
        readonly IGetRepositorySpec mGetRepositorySpec;
        readonly ChangesetsViewMenu mMenu;
        readonly List<string> mColumnNames;
    }
}
