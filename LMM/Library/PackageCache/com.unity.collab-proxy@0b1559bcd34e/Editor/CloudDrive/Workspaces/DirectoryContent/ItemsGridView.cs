using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal interface IItemsGridView
    {
        void ClearSelectedItems();

        void SetSelection(List<int> itemIndexesToSelect);

        void UpdateScrollPosition(float scrollPosition);

        void ClearDragTargetIndex();

        void SetDragTargetIndexFromPath(string path);

        string GetDragTargetPathFromIndex(int index);
    }

    internal class ItemsGridView : IItemsGridView
    {
        internal ItemsGridView(
            DirectoryContentPanelMenu menu,
            IDragAndDrop dragAndDrop,
            EditorWindow parentWindow,
            Action doubleClickAction,
            Action navigateBackAction)
        {
            mMenu = menu;
            mDragAndDrop = dragAndDrop;
            mParentWindow = parentWindow;
            mDoubleClickAction = doubleClickAction;
            mNavigateBackAction = navigateBackAction;

            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);
            mEmptyStatePanel.UpdateContent(GetEmptyStateText(nodes: null));
        }

        internal string GetItemsDirPath()
        {
            return mItemsDirPath;
        }

        internal int GetSelectedItemsCount()
        {
            return mSelectedItemsIndexes.Count;
        }

        internal ExpandedTreeNode GetSelectedItem()
        {
            if (mSelectedItemsIndexes.Count == 0)
                return null;

            return mItems[mSelectedItemsIndexes[0]];
        }

        internal List<string> GetSelectedItemsPaths()
        {
            List<string> result = new List<string>(mSelectedItemsIndexes.Count);

            foreach (int selectedItemIndex in mSelectedItemsIndexes)
                result.Add(mItems[selectedItemIndex].GetFullPath());

            return result;
        }

        internal void CleanItems()
        {
            mItemsDirPath = null;
            mItems = null;

            mEmptyStatePanel.UpdateContent(GetEmptyStateText(nodes: null));

            ClearSelection();
        }

        internal void ClearSelection()
        {
            mSelectedItemsIndexes.Clear();

            ClearScrollPosition();
        }

        internal void UpdateItems(string itemsDirPath, List<ExpandedTreeNode> items)
        {
            mItems = new List<ExpandedTreeNode>();
            foreach (ExpandedTreeNode item in items)
            {
                if (ExpandedTreeNode.IsOnDisk(item))
                    mItems.Add(item);
            }

            mItemsDirPath = itemsDirPath;

            mEmptyStatePanel.UpdateContent(GetEmptyStateText(mItems));
        }

        internal void SelectItemsFromPaths(List<string> itemsPathsToSelect)
        {
            mSelectedItemsIndexes.Clear();

            Dictionary<string, int> indexesByPath = new Dictionary<string, int>();
            for (int i = 0; i < mItems.Count; i++)
                indexesByPath[mItems[i].GetFullPath()] = i;

            foreach (string itemPathToSelect in itemsPathsToSelect)
            {
                int index;
                if (!indexesByPath.TryGetValue(itemPathToSelect, out index))
                    continue;

                mSelectedItemsIndexes.Add(index);
            }
        }

        internal void OnGUI(Rect rect, bool hasFocus)
        {
            int itemsCount = GetItemsCount();

            UpdateSize(rect, itemsCount);

            bool shouldScrollbarBeVisible =
                mRows * mItemSize.y + 2 * DrawItemsGridView.GRID_AREA_MARGIN > rect.height;

            if (!shouldScrollbarBeVisible)
                ClearScrollPosition();

            DrawItemsGridView.Draw(
                rect, mEmptyStatePanel, this, mItems, mSelectedItemsIndexes, mItemSize,
                mScrollPosition.y, mRows, mColumns, shouldScrollbarBeVisible, hasFocus,
                mDragTargetIndex, ref mShouldShowEmptyState);

            Event e = Event.current;

            if (ProcessItemsGridViewEvent.IfNeeded(
                    e,
                    mMenu,
                    this,
                    mDragAndDrop,
                    mParentWindow,
                    mDoubleClickAction,
                    mNavigateBackAction,
                    mSelectedItemsIndexes,
                    rect,
                    mItemSize,
                    mScrollPosition.y,
                    mColumns,
                    itemsCount,
                    hasFocus,
                    shouldScrollbarBeVisible,
                    mItemsDirPath != null))
                e.Use();
        }

        void IItemsGridView.ClearSelectedItems()
        {
            mSelectedItemsIndexes.Clear();
        }

        void IItemsGridView.SetSelection(List<int> itemIndexesToSelect)
        {
            if (itemIndexesToSelect.Count == 0)
            {
                ClearSelection();
                return;
            }

            mSelectedItemsIndexes.Clear();
            mSelectedItemsIndexes.AddRange(itemIndexesToSelect);

            mScrollPosition.y = ItemsPosition.GetScrollPosition(
                mRows,
                mColumns,
                mSelectedItemsIndexes[mSelectedItemsIndexes.Count - 1],
                mItemSize.y,
                mAvailableHeight,
                mScrollPosition.y);
        }

        void IItemsGridView.UpdateScrollPosition(float scrollPosition)
        {
            mScrollPosition.y = scrollPosition;
        }

        void IItemsGridView.ClearDragTargetIndex()
        {
            mDragTargetIndex = -1;
        }

        void IItemsGridView.SetDragTargetIndexFromPath(string path)
        {
            if (path == mItemsDirPath)
            {
                mDragTargetIndex = -1;
                return;
            }

            for (int i = 0; i < mItems.Count; i++)
            {
                if (mItems[i].GetFullPath() == path)
                {
                    mDragTargetIndex = i;
                    return;
                }
            }

            mDragTargetIndex = -1;
        }

        string IItemsGridView.GetDragTargetPathFromIndex(int index)
        {
            if (index < 0 || index >= GetItemsCount())
                return mItemsDirPath;

            ExpandedTreeNode dragTargetNode = mItems[index];

            if (!ExpandedTreeNode.IsDirectory(dragTargetNode))
                return mItemsDirPath;

            return dragTargetNode.GetFullPath();
        }

        void ClearScrollPosition()
        {
            mScrollPosition.y = 0;
        }

        void UpdateSize(Rect rect, int itemsCount)
        {
            mAvailableHeight = rect.height;

            if (itemsCount == 0)
            {
                mColumns = 0;
                mRows = 0;
                mItemSize = Vector2.zero;
                return;
            }

            float availableItemsWidth = rect.width - DrawItemsGridView.GRID_AREA_MARGIN * 2;

            mColumns = Math.Max(
                1, (int)(availableItemsWidth / (DrawExpandedTreeNode.ICON_SIZE + 2 * DrawExpandedTreeNode.PADDING)));

            mRows = Mathf.CeilToInt((float)itemsCount / mColumns);

            float itemWidth = availableItemsWidth / mColumns;

            float itemHeight = DrawExpandedTreeNode.ICON_SIZE
                + ((GUIStyle)UnityStyles.CloudDrive.ItemsListLabel).lineHeight
                + 2 * DrawExpandedTreeNode.PADDING;

            mItemSize = new Vector2(itemWidth, itemHeight);
        }

        int GetItemsCount()
        {
            if (mItems == null)
                return 0;

            return mItems.Count;
        }

        static string GetEmptyStateText(List<ExpandedTreeNode> nodes)
        {
            if (nodes == null)
                return PlasticLocalization.Name.NoDirectorySelectedEmptyState.GetString();

            return nodes.Count == 0 ?
                PlasticLocalization.Name.EmptyDirectoryEmptyState.GetString() :
                string.Empty;
        }

        bool mShouldShowEmptyState;
        int mRows;
        int mColumns;
        float mAvailableHeight;
        Vector2 mItemSize;
        Vector2 mScrollPosition;

        int mDragTargetIndex = -1;
        string mItemsDirPath;
        List<ExpandedTreeNode> mItems;

        readonly List<int> mSelectedItemsIndexes = new List<int>();
        readonly EmptyStatePanel mEmptyStatePanel;
        readonly DirectoryContentPanelMenu mMenu;
        readonly IDragAndDrop mDragAndDrop;
        readonly EditorWindow mParentWindow;
        readonly Action mDoubleClickAction;
        readonly Action mNavigateBackAction;
    }
}
