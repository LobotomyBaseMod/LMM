using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.QueryViews.Shelves;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;

using Time = Codice.Client.Common.Time;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Shelves
{
    internal class ShelvesListView :
        PlasticTreeView,
        FillShelvesView.IShelvesList
    {
        internal GenericMenu Menu { get { return mMenu.Menu; } }

        internal ShelvesListView(
            ShelvesListHeaderState headerState,
            List<string> columnNames,
            ShelvesViewMenu menu,
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

        protected override void ContextClickedItem(int id)
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
            if (args.item is ShelveListViewItem)
            {
                ShelvesListViewItemGUI(
                    mQueryResult,
                    rowHeight,
                    (ShelveListViewItem)args.item,
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

        void FillShelvesView.IShelvesList.Fill(
            List<object> shelves,
            ChangesetInfo shelveToSelect,
            Filter filter)
        {
            mListViewItemIds.Clear();

            List<RepObjectInfo> shelvesToSelect = ShelvesSelection.GetShelvesToSelect(
                this, shelveToSelect);

            int defaultRow = TableViewOperations.GetFirstSelectedRow(this);

            mQueryResult = new ViewQueryResult(
                EnumQueryObjectType.Shelve, shelves, mGetRepositorySpec.Get());

            UpdateResults();

            ShelvesSelection.SelectShelves(this, shelvesToSelect, defaultRow);
        }

        void DelayedSearchChanged()
        {
            UpdateResults();

            TableViewOperations.ScrollToSelection(this);
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

                ShelveListViewItem shelveListViewItem =
                    new ShelveListViewItem(objectId, objectInfo);

                rootItem.AddChild(shelveListViewItem);
                rows.Add(shelveListViewItem);
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

        static void ShelvesListViewItemGUI(
            ViewQueryResult queryResult,
            float rowHeight,
            ShelveListViewItem item,
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

                ShelvesListColumn column =
                    (ShelvesListColumn)args.GetColumn(visibleColumnIdx);

                ShelvesListViewItemCellGUI(
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

        static void ShelvesListViewItemCellGUI(
            Rect rect,
            float rowHeight,
            ViewQueryResult queryResult,
            ShelveListViewItem item,
            ShelvesListColumn column,
            Action avatarLoadedAction,
            bool isSelected,
            bool isFocused)
        {
            RepositorySpec repSpec = queryResult.GetRepositorySpec(item.ObjectInfo);
            RepObjectInfo objectInfo = queryResult.GetRepObjectInfo(item.ObjectInfo);

            string columnText = RepObjectInfoView.GetColumnText(
                repSpec,
                objectInfo,
                ShelvesListHeaderState.GetColumnName(column));

            string userName = PlasticGui.Plastic.API.GetUserName(repSpec.Server, objectInfo.Owner);

            if (column == ShelvesListColumn.Comment)
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
                    false,
                    false);
                return;
            }

            if (column == ShelvesListColumn.CreationDate)
            {
                DrawTreeViewItem.ForLabel(
                    rect,
                    new GUIContent(
                        Time.GetLongTimeAgoString(objectInfo.LocalTimeStamp), columnText),
                    isSelected,
                    isFocused,
                    false);
                return;
            }

            DrawTreeViewItem.ForLabel(
                rect, columnText, isSelected, isFocused, false);
        }

        ListViewItemIds<object> mListViewItemIds = new ListViewItemIds<object>();

        ViewQueryResult mQueryResult;

        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;
        readonly Action<IEnumerable<object>> mAfterItemsChangedAction;
        readonly Action mDoubleClickAction;
        readonly Action mDelayedSelectionChangedAction;
        readonly IGetRepositorySpec mGetRepositorySpec;
        readonly ShelvesViewMenu mMenu;
        readonly List<string> mColumnNames;
    }
}
