using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Common;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Locks;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Locks
{
    internal sealed class LocksListView :
        PlasticTreeView,
        FillLocksTable.IShowContentView,
        FillLocksTable.ILocksList
    {
        internal GenericMenu Menu { get { return mMenu.Menu; } }
        internal string EmptyStateMessage { get { return mEmptyStatePanel.Text; } }

        internal LocksListView(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            LocksListHeaderState headerState,
            List<string> columnNames,
            LocksViewMenu menu,
            Action delayedSelectionChangedAction,
            Action<IEnumerable<LockInfo>> afterItemsChangedAction,
            Action repaintAction)
        {
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mColumnNames = columnNames;
            mMenu = menu;
            mDelayedSelectionChangedAction = delayedSelectionChangedAction;
            mAfterItemsChangedAction = afterItemsChangedAction;
            mEmptyStatePanel = new EmptyStatePanel(repaintAction);
            mMultiLinkLabelData =  new MultiLinkLabelData(
                PlasticLocalization.Name.LocksTutorialLabel.GetString(),
                new List<string> { PlasticLocalization.Name.LocksTutorialButton.GetString() },
                new List<Action> { () => { Codice.Utils.OpenBrowser.TryOpen(LOCKS_TUTORIAL_LINK); } });

            mLocksSelector = new LocksSelector(this, mListViewItemIds);

            mDelayedFilterAction = new DelayedActionBySecondsRunner(
                DelayedSearchChanged, UnityConstants.SEARCH_DELAYED_INPUT_ACTION_INTERVAL);

            mDelayedSelectionRunner = new DelayedActionBySecondsRunner(
                DelayedSelectionChanged, UnityConstants.SELECTION_DELAYED_INPUT_ACTION_INTERVAL);

            SetupTreeView(headerState);
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            if (Event.current.type == EventType.Layout)
                mShouldShowEmptyState = mRows.Count == 0 && !mEmptyStatePanel.IsEmpty();

            if (mShouldShowEmptyState)
                mEmptyStatePanel.OnGUI(rect);

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
            if (mLocksList == null)
            {
                ClearRows(rootItem, mRows);

                return mRows;
            }

            RegenerateRows(
                mListViewItemIds,
                mLocksList,
                rootItem,
                mRows);

            return mRows;
        }

        protected override void SearchChanged(string newSearch)
        {
            mDelayedFilterAction.Run();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            mDelayedSelectionRunner.Run();
        }

        protected override void ContextClickedItem(int id)
        {
            mMenu.Popup();
            Repaint();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is LocksListViewItem)
            {
                LocksListViewItemGUI(
                    mRepSpec,
                    mWkInfo.ClientPath,
                    rowHeight,
                    ((LocksListViewItem)args.item).LockInfo,
                    args,
                    Repaint);
                return;
            }

            base.RowGUI(args);
        }

        internal void OnDisable()
        {
            TreeHeaderSettings.Save(
                multiColumnHeader.state,
                UnityConstants.LOCKS_TABLE_SETTINGS_NAME);
        }

        internal List<LockInfo> GetSelectedLocks()
        {
            return mLocksSelector.GetSelectedLocks();
        }

        void FillLocksTable.IShowContentView.ShowContentPanel()
        {
            mEmptyStatePanel.UpdateContent(
                string.Empty,
                multiLinkLabelData: mMultiLinkLabelData);

            Reload();

            mLocksSelector.RestoreSelectedLocks();
        }

        void FillLocksTable.IShowContentView.ShowEmptyStatePanel(string explanationText)
        {
            mEmptyStatePanel.UpdateContent(
                explanationText,
                multiLinkLabelData: string.IsNullOrEmpty(searchString) ? mMultiLinkLabelData : null);

            Reload();
        }

        void FillLocksTable.IShowContentView.ShowErrorPanel(string errorText)
        {
            Debug.LogErrorFormat(
                PlasticLocalization.Name.LoadLocksErrorExplanation.GetString(),
                errorText);

            mEmptyStatePanel.UpdateContent(
                PlasticLocalization.Name.LoadLocksError.GetString(),
                multiLinkLabelData: mMultiLinkLabelData);

            mLocksList = null;
            mListViewItemIds.Clear();

            Reload();
        }

        void FillLocksTable.ILocksList.Fill(LockInfoList lockInfoList, Filter filter)
        {
            mLocksSelector.SaveSelectedLocks();

            mListViewItemIds.Clear();

            mLocksList = lockInfoList;

            Filter();
            Sort();
        }

        void Filter()
        {
            if (mLocksList == null)
                return;

            mLocksList.Filter(new Filter(searchString));
            mAfterItemsChangedAction(mLocksList.GetLocks());
        }

        void Sort()
        {
            if (mLocksList == null)
                return;

            int sortedColumnIdx = multiColumnHeader.state.sortedColumnIndex;
            bool sortAscending = multiColumnHeader.IsSortedAscending(sortedColumnIdx);

            mLocksList.Sort(mColumnNames[sortedColumnIdx], sortAscending);
        }

        void DelayedSearchChanged()
        {
            Filter();

            Reload();

            TableViewOperations.ScrollToSelection(this);
        }

        void DelayedSelectionChanged()
        {
            mDelayedSelectionChangedAction();
        }

        void SortingChanged(MultiColumnHeader header)
        {
            Sort();

            Reload();
        }

        void SetupTreeView(LocksListHeaderState headerState)
        {
            TreeHeaderSettings.Load(
                headerState,
                UnityConstants.LOCKS_TABLE_SETTINGS_NAME,
                (int)LocksListColumn.ModificationDate,
                false);

            multiColumnHeader = new MultiColumnHeader(headerState);
            multiColumnHeader.canSort = true;
            multiColumnHeader.sortingChanged += SortingChanged;
        }

        static void RegenerateRows(
            ListViewItemIds<LockInfo> listViewItemIds,
            LockInfoList locksList,
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            ClearRows(rootItem, rows);

            if (locksList == null)
                return;

            foreach (LockInfo lockInfo in locksList.GetLocks())
            {
                int objectId;
                if (!listViewItemIds.TryGetInfoItemId(lockInfo, out objectId))
                    objectId = listViewItemIds.AddInfoItem(lockInfo);

                LocksListViewItem lockListViewItem =
                    new LocksListViewItem(objectId, lockInfo);

                rootItem.AddChild(lockListViewItem);
                rows.Add(lockListViewItem);
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

        static void LocksListViewItemGUI(
            RepositorySpec repSpec,
            string wkPath,
            float rowHeight,
            LockInfo item,
            RowGUIArgs args,
            Action avatarLoadedAction)
        {
            for (var visibleColumnIdx = 0; visibleColumnIdx < args.GetNumVisibleColumns(); visibleColumnIdx++)
            {
                var cellRect = args.GetCellRect(visibleColumnIdx);

                if (visibleColumnIdx == 0)
                {
                    cellRect.x += UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;
                    cellRect.width -= UnityConstants.FIRST_COLUMN_WITHOUT_ICON_INDENT;
                }

                var column = (LocksListColumn) args.GetColumn(visibleColumnIdx);

                DrawLocksListViewItem.ForCell(
                    repSpec,
                    wkPath,
                    cellRect,
                    rowHeight,
                    item,
                    column,
                    avatarLoadedAction,
                    args.selected,
                    args.focused);
            }
        }

        bool mShouldShowEmptyState;
        ListViewItemIds<LockInfo> mListViewItemIds = new ListViewItemIds<LockInfo>();

        LockInfoList mLocksList;

        readonly EmptyStatePanel mEmptyStatePanel;
        readonly MultiLinkLabelData mMultiLinkLabelData;
        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;
        readonly LocksSelector mLocksSelector;
        readonly Action mDelayedSelectionChangedAction;
        readonly Action<IEnumerable<LockInfo>> mAfterItemsChangedAction;
        readonly LocksViewMenu mMenu;
        readonly List<string> mColumnNames;
        readonly RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;

        const string LOCKS_TUTORIAL_LINK = "https://learn.unity.com/tutorial/6650a6abedbc2a2ccccb05a4";
    }
}
