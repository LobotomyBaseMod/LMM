using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;

using Time = Codice.Client.Common.Time;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Branches
{
    internal class BranchesListView :
        PlasticTreeView,
        IPlasticTree<BranchTreeNode>
    {
        internal GenericMenu Menu { get { return mMenu.Menu; } }

        internal BranchesListView(
            BranchesListHeaderState headerState,
            List<string> columnNames,
            BranchesViewMenu menu,
            IGetRepositorySpec getRepositorySpec,
            IGetWorkingObject getWorkingObject,
            Func<bool> getIsHiddenBranchesMode,
            Action delayedSelectionChangedAction,
            Action doubleClickAction,
            Action<IEnumerable<object>> afterItemsChangedAction)
        {
            mColumnNames = columnNames;
            mMenu = menu;
            mGetRepositorySpec = getRepositorySpec;
            mGetWorkingObject = getWorkingObject;
            mGetIsHiddenBranchesMode = getIsHiddenBranchesMode;
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
                rootItem, mRows);

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
            {
                e.Use();
                GUIUtility.ExitGUI();
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is BranchListViewItem)
            {
                BranchListViewItem branchListViewItem = (BranchListViewItem)args.item;

                BranchesListViewItemGUI(
                    mQueryResult,
                    rowHeight,
                    branchListViewItem,
                    args,
                    RepObjectInfoView.IsHighlighted(
                        branchListViewItem.ObjectInfo, mGetWorkingObject.Get()),
                    mGetIsHiddenBranchesMode(),
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

                result.Add(((BranchListViewItem)row).ObjectInfo);
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

        void IPlasticTree<BranchTreeNode>.FillItemsAndSelectRows(
            IPlasticTree treeModel,
            List<IPlasticTreeNode> itemsToSelect,
            Filter currentFilter)
        {
            mListViewItemIds.Clear();

            List<RepObjectInfo> branchesToSelect = BranchesSelection.GetBranchesToSelect(
                this, itemsToSelect);

            int defaultRow = TableViewOperations.GetFirstSelectedRow(this);

            mQueryResult = BuildViewQueryResult(
                EnumQueryObjectType.Branch, treeModel, mGetRepositorySpec.Get());

            UpdateResults();

            BranchesSelection.SelectBranches(this, branchesToSelect, defaultRow);
        }

        void DelayedSearchChanged()
        {
            UpdateResults();

            TableViewOperations.ScrollToSelection(this);
        }

        void UpdateResults()
        {
            Refilter();

            Sort();

            mAfterItemsChangedAction(mQueryResult.GetObjects());

            Reload();
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

                BranchListViewItem branchListViewItem =
                    new BranchListViewItem(objectId, objectInfo);

                rootItem.AddChild(branchListViewItem);
                rows.Add(branchListViewItem);
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

        static void BranchesListViewItemGUI(
            ViewQueryResult queryResult,
            float rowHeight,
            BranchListViewItem item,
            RowGUIArgs args,
            bool isBoldText,
            bool isHiddenBranchesMode,
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

                BranchesListColumn column =
                    (BranchesListColumn)args.GetColumn(visibleColumnIdx);

                BranchesListViewItemCellGUI(
                    cellRect,
                    rowHeight,
                    queryResult,
                    item,
                    column,
                    avatarLoadedAction,
                    isHiddenBranchesMode,
                    args.selected,
                    args.focused,
                    isBoldText);
            }
        }

        static void BranchesListViewItemCellGUI(
            Rect rect,
            float rowHeight,
            ViewQueryResult queryResult,
            BranchListViewItem item,
            BranchesListColumn column,
            Action avatarLoadedAction,
            bool isHiddenBranchesMode,
            bool isSelected,
            bool isFocused,
            bool isBoldText)
        {
            RepObjectInfo repObjectInfo = queryResult.GetRepObjectInfo(item.ObjectInfo);

            string columnText = RepObjectInfoView.GetColumnText(
                queryResult.GetRepositorySpec(item.ObjectInfo),
                repObjectInfo,
                BranchesListHeaderState.GetColumnName(column));

            if (column == BranchesListColumn.Name)
            {
                DrawTreeViewItem.ForItemCell(
                    rect,
                    rowHeight,
                    -1,
                    isHiddenBranchesMode ?
                        Images.GetHideIcon() :
                        Images.GetBranchIcon(),
                    null,
                    null,
                    columnText,
                    isSelected,
                    isFocused,
                    isBoldText,
                    false,
                    DrawTreeViewItem.TextTrimming.Path);
                return;
            }

            if (column == BranchesListColumn.CreatedBy)
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
                    isBoldText,
                    false);
                return;
            }

            if (column == BranchesListColumn.CreationDate)
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
                rect,
                columnText,
                isSelected,
                isFocused,
                isBoldText);
        }

        static ViewQueryResult BuildViewQueryResult(
            EnumQueryObjectType objectType, IPlasticTree treeModel, RepositorySpec repositorySpec)
        {
            IList<object> resultObjects = new List<object>();
            foreach (BranchTreeNode node in treeModel.GetNodes())
            {
                resultObjects.Add(node.BranchInfo);
            }

            return new ViewQueryResult(objectType, resultObjects, repositorySpec);
        }

        ListViewItemIds<object> mListViewItemIds = new ListViewItemIds<object>();

        ViewQueryResult mQueryResult;

        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;
        readonly Action<IEnumerable<object>> mAfterItemsChangedAction;
        readonly Func<bool> mGetIsHiddenBranchesMode;
        readonly Action mDelayedSelectionChangedAction;
        readonly IGetWorkingObject mGetWorkingObject;
        readonly IGetRepositorySpec mGetRepositorySpec;
        readonly Action mDoubleClickAction;
        readonly BranchesViewMenu mMenu;
        readonly List<string> mColumnNames;
    }
}
