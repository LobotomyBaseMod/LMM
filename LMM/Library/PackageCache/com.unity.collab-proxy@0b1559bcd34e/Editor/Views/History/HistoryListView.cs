using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.History;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;

using Time = Codice.Client.Common.Time;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.History
{
    internal class HistoryListView : PlasticTreeView
    {
        internal GenericMenu Menu { get { return mMenu.Menu; } }

        internal HistoryListView(
            string wkPath,
            HistoryListHeaderState headerState,
            HistoryListViewMenu menu,
            List<string> columnNames,
            Action delayedSelectionChangedAction,
            Action doubleClickAction,
            Action<IEnumerable<RepObjectInfo>> afterItemsChangedAction)
        {
            mWkPath = wkPath;
            mMenu = menu;
            mColumnNames = columnNames;
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

        protected override IList<TreeViewItem> BuildRows(
            TreeViewItem rootItem)
        {
            if (mRevisionsList == null)
            {
                ClearRows(rootItem, mRows);

                return mRows;
            }

            RegenerateRows(
                mListViewItemIds,
                mRevisionsList,
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

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is HistoryListViewItem)
            {
                HistoryListViewItem historyListViewItem = (HistoryListViewItem)args.item;

                HistoryListViewItemGUI(
                    mWkPath,
                    mRepSpec,
                    rowHeight,
                    historyListViewItem,
                    args,
                    mLoadedRevisionId == historyListViewItem.Revision.Id,
                    Repaint);
                return;
            }

            base.RowGUI(args);
        }

        protected override void DoubleClickedItem(int id)
        {
            mDoubleClickAction();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            mDelayedSelectionRunner.Run();
        }

        void DelayedSelectionChanged()
        {
            if (!HasSelection())
                return;

            mDelayedSelectionChangedAction();
        }

        internal void BuildModel(
            RepositorySpec repSpec,
            HistoryRevisionList historyRevisionList,
            long loadedRevisionId)
        {
            mListViewItemIds.Clear();

            mRepSpec = repSpec;
            mRevisionsList = historyRevisionList;
            mLoadedRevisionId = loadedRevisionId;
        }

        internal void Refilter()
        {
            if (mRevisionsList == null)
                return;

            Filter filter = new Filter(searchString);
            mRevisionsList.Filter(filter, mColumnNames, null, null);

            mAfterItemsChangedAction(mRevisionsList.GetRevisions());
        }

        internal void Sort()
        {
            if (mRevisionsList == null)
                return;

            int sortedColumnIdx = multiColumnHeader.state.sortedColumnIndex;
            bool sortAscending = multiColumnHeader.IsSortedAscending(sortedColumnIdx);

            mRevisionsList.Sort(
                mColumnNames[sortedColumnIdx],
                sortAscending);
        }

        internal long GetLoadedRevisionId()
        {
            return mLoadedRevisionId;
        }

        internal List<RepObjectInfo> GetSelectedRepObjectInfos()
        {
            List<RepObjectInfo> result = new List<RepObjectInfo>();

            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count == 0)
                return result;

            foreach (KeyValuePair<RepObjectInfo, int> item
                     in mListViewItemIds.GetInfoItems())
            {
                if (!selectedIds.Contains(item.Value))
                    continue;

                result.Add(item.Key);
            }

            return result;
        }

        internal List<RepObjectInfo> GetAllRepObjectInfos()
        {
            List<RepObjectInfo> result = new List<RepObjectInfo>();

            foreach (KeyValuePair<RepObjectInfo, int> item
                     in mListViewItemIds.GetInfoItems())
            {
                result.Add(item.Key);
            }

            return result;
        }

        internal List<HistoryRevision> GetSelectedHistoryRevisions()
        {
            return GetSelectedRepObjectInfos().OfType<HistoryRevision>().ToList();
        }

        internal long GetSelectedChangesetId()
        {
            List<RepObjectInfo> repObjectInfos = GetSelectedRepObjectInfos();

            foreach (RepObjectInfo info in repObjectInfos)
            {
                if (info is HistoryRevision)
                    return ((HistoryRevision)info).ChangeSet;

                if (info is MoveRealizationInfo)
                    return ((MoveRealizationInfo)info).DstDirRev.Changeset;

                if (info is RemovedRealizationInfo)
                    return ((RemovedRealizationInfo)info).DirRev.Changeset;
            }

            return -1;
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

        int GetTreeIdForItem(RepObjectInfo currentRepObjectInfo)
        {
            foreach (KeyValuePair<RepObjectInfo, int> item in mListViewItemIds.GetInfoItems())
            {
                if (!currentRepObjectInfo.Equals(item.Key))
                    continue;

                if (!currentRepObjectInfo.GUID.Equals(item.Key.GUID))
                    continue;

                return item.Value;
            }

            return -1;
        }

        void DelayedSearchChanged()
        {
            Refilter();

            Sort();

            Reload();

            TableViewOperations.ScrollToSelection(this);
        }

        void SortingChanged(MultiColumnHeader multiColumnHeader)
        {
            Sort();

            Reload();
        }

        static void RegenerateRows(
            ListViewItemIds<RepObjectInfo> listViewItemIds,
            HistoryRevisionList revisions,
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            ClearRows(rootItem, rows);

            if (revisions.GetCount() == 0)
                return;

            foreach (RepObjectInfo objectInfo in revisions.GetRevisions())
            {
                int objectId;
                if (!listViewItemIds.TryGetInfoItemId(objectInfo, out objectId))
                    objectId = listViewItemIds.AddInfoItem(objectInfo);

                HistoryListViewItem changesetListViewItem =
                    new HistoryListViewItem(objectId, objectInfo);

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

        static void HistoryListViewItemGUI(
            string wkPath,
            RepositorySpec repSpec,
            float rowHeight,
            HistoryListViewItem item,
            RowGUIArgs args,
            bool isBoldText,
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

                HistoryListColumn column =
                    (HistoryListColumn)args.GetColumn(visibleColumnIdx);

                HistoryListViewItemCellGUI(
                    cellRect,
                    rowHeight,
                    wkPath,
                    repSpec,
                    item,
                    column,
                    avatarLoadedAction,
                    args.selected,
                    args.focused,
                    isBoldText);
            }
        }

        static void HistoryListViewItemCellGUI(
            Rect rect,
            float rowHeight,
            string wkPath,
            RepositorySpec repSpec,
            HistoryListViewItem item,
            HistoryListColumn column,
            Action avatarLoadedAction,
            bool isSelected,
            bool isFocused,
            bool isBoldText)
        {
            string columnText = HistoryInfoView.GetColumnText(
                wkPath, repSpec, item.Revision,
                HistoryListHeaderState.GetColumnName(column));
            string userName = PlasticGui.Plastic.API.GetUserName(repSpec.Server, item.Revision.Owner);

            if (column == HistoryListColumn.Comment)
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

            if (column == HistoryListColumn.Type)
            {
                DrawTreeViewItem.ForItemCell(
                    rect,
                    rowHeight,
                    -1,
                    null,
                    null,
                    null,
                    GetRevisionText(item.Revision),
                    isSelected,
                    isFocused,
                    isBoldText,
                    false);
                return;
            }

            if (column == HistoryListColumn.CreationDate)
            {
                DrawTreeViewItem.ForLabel(
                    rect,
                    new GUIContent(
                        Time.GetLongTimeAgoString(item.Revision.LocalTimeStamp),
                        columnText),
                    isSelected,
                    isFocused,
                    isBoldText);
                return;
            }

            if (column == HistoryListColumn.Branch)
            {
                DrawTreeViewItem.ForSecondaryLabel(
                    rect, columnText, isSelected, isFocused, isBoldText);
                return;
            }

            DrawTreeViewItem.ForLabel(
                rect, columnText, isSelected, isFocused, isBoldText);
        }

        static string GetRevisionText(RepObjectInfo revision)
        {
            if (revision is MoveRealizationInfo)
                return PlasticLocalization.Name.MovedDiffCategory.GetString();

            if (revision is RemovedRealizationInfo)
                return PlasticLocalization.Name.DeletedDiffCategory.GetString();

            if (((HistoryRevision)revision).ParentRevisionId == -1)
                return PlasticLocalization.Name.AddedDiffCategory.GetString();

            return PlasticLocalization.Name.ChangedDiffCategory.GetString();
        }

        ListViewItemIds<RepObjectInfo> mListViewItemIds = new ListViewItemIds<RepObjectInfo>();

        HistoryRevisionList mRevisionsList;
        long mLoadedRevisionId;
        RepositorySpec mRepSpec;

        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;
        readonly HistoryListViewMenu mMenu;
        readonly List<string> mColumnNames;
        readonly Action mDelayedSelectionChangedAction;
        readonly Action mDoubleClickAction;
        readonly Action<IEnumerable<RepObjectInfo>> mAfterItemsChangedAction;
        readonly string mWkPath;
    }
}
