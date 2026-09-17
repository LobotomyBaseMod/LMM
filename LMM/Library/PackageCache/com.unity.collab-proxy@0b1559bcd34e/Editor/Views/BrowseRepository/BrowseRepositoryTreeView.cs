using System;
using System.Collections.Generic;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

using PlasticGui.WorkspaceWindow.BrowseRepository;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.BrowseRepository
{
    internal class BrowseRepositoryTreeView : PlasticTreeView
    {
        internal BrowseRepositoryTreeView(
            Action afterRowsBuiltAction,
            BrowseRepositoryHeaderState headerState)
        {
            mAfterRowsBuiltAction = afterRowsBuiltAction;

            multiColumnHeader = new MultiColumnHeader(headerState);
            multiColumnHeader.canSort = true;
            multiColumnHeader.sortingChanged += SortingChanged;

            mColumnComparers = BrowseRepositoryHeaderState.BuildColumnComparers();

            customFoldoutYOffset = UnityConstants.TREEVIEW_FOLDOUT_Y_OFFSET;
        }

        protected override bool CanChangeExpandedState(TreeViewItem item)
        {
            if (item is BrowseRepositoryViewItem)
            {
                BrowseRepositoryViewItem browseItem = (BrowseRepositoryViewItem)item;
                return BrowseRepositoryInfoView.IsDirectory(browseItem.TreeNode.Node);
            }
            return false;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem rootItem)
        {
            int sortedColumnIdx = multiColumnHeader.state.sortedColumnIndex;
            bool sortAscending = multiColumnHeader.IsSortedAscending(sortedColumnIdx);

            BrowseRepositoryColumn column = (BrowseRepositoryColumn)sortedColumnIdx;
            IComparer<BrowseRepositoryTreeNode> comparer = mColumnComparers[column];

            RegenerateRows(
                mBrowseRepositoryTree,
                mTreeViewItemIds,
                this,
                rootItem,
                mRows,
                comparer,
                sortAscending);

            mAfterRowsBuiltAction();

            return mRows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is BrowseRepositoryViewItem)
            {
                BrowseRepositoryTreeItemGUI(
                    rowHeight,
                    (BrowseRepositoryViewItem)args.item,
                    args,
                    false,
                    Repaint);
                return;
            }

            base.RowGUI(args);
        }

        internal void Refilter()
        {
            // TODO
        }

        void SortingChanged(MultiColumnHeader multiColumnHeader)
        {
            Reload();
        }

        internal void ClearModel()
        {
            mTreeViewItemIds.Clear();
            state.expandedIDs.Clear();

            mBrowseRepositoryTree = null;
        }

        internal void BuildModel(BrowseRepositoryTree browseRepositoryTree)
        {
            mBrowseRepositoryTree = browseRepositoryTree;
        }

        void BrowseRepositoryTreeItemGUI(
            float rowHeight,
            BrowseRepositoryViewItem item,
            RowGUIArgs args,
            bool isBoldText,
            Action avatarLoadedAction)
        {
            for (int visibleColumnIdx = 0; visibleColumnIdx < args.GetNumVisibleColumns(); visibleColumnIdx++)
            {
                Rect cellRect = args.GetCellRect(visibleColumnIdx);

                BrowseRepositoryColumn column =
                    (BrowseRepositoryColumn)args.GetColumn(visibleColumnIdx);

                BrowseRepositoryTreeItemCellGUI(
                    cellRect,
                    rowHeight,
                    item,
                    column,
                    avatarLoadedAction,
                    args.selected,
                    args.focused,
                    isBoldText);
            }
        }

        static void BrowseRepositoryTreeItemCellGUI(
            Rect rect,
            float rowHeight,
            BrowseRepositoryViewItem item,
            BrowseRepositoryColumn column,
            Action avatarLoadedAction,
            bool isSelected,
            bool isFocused,
            bool isBoldText)
        {
            string columnText = BrowseRepositoryInfoView.GetColumnText(
                item.TreeNode.Node,
                item.TreeNode.RepSpec,
                BrowseRepositoryHeaderState.GetColumnName(column));

            if (column == BrowseRepositoryColumn.CreatedBy)
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

            if (column == BrowseRepositoryColumn.Branch ||
                column == BrowseRepositoryColumn.Repository)
            {
                DrawTreeViewItem.ForSecondaryLabel(
                    rect, columnText, isSelected, isFocused, isBoldText);
                return;
            }

            bool isItemColumn = column == BrowseRepositoryColumn.Item;

            Texture icon = null;
            if (isItemColumn)
            {
                icon = BrowseRepositoryInfoView.IsDirectory(item.TreeNode.Node) ?
                    Images.GetFolderIcon() : Images.GetFileIcon();
            }

            DrawTreeViewItem.ForItemCell(
                rect,
                rowHeight,
                isItemColumn ? item.depth : -1,
                icon,
                null,
                null,
                columnText,
                isSelected,
                isFocused,
                isBoldText,
                false);
        }

        static void RegenerateRows(
            BrowseRepositoryTree repositoryTree,
            TreeViewItemIds<string, BrowseRepositoryTreeNode> treeViewItemIds,
            BrowseRepositoryTreeView treeView,
            TreeViewItem rootItem,
            List<TreeViewItem> rows,
            IComparer<BrowseRepositoryTreeNode> comparer,
            bool sortAscending)
        {
            ClearRows(rootItem, rows);

            if (repositoryTree == null)
                return;

            BrowseRepositoryTreeNode rootNode = repositoryTree.GetRootNode();

            if (rootNode == null)
                return;

            AddNode(
                rootNode,
                treeViewItemIds,
                treeView,
                rootItem,
                rows,
                comparer,
                sortAscending);
        }

        static void ClearRows(
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            if (rootItem.hasChildren)
                rootItem.children.Clear();

            rows.Clear();
        }

        static void AddNode(
            BrowseRepositoryTreeNode node,
            TreeViewItemIds<string, BrowseRepositoryTreeNode> treeViewItemIds,
            BrowseRepositoryTreeView treeView,
            TreeViewItem parentItem,
            List<TreeViewItem> rows,
            IComparer<BrowseRepositoryTreeNode> comparer,
            bool sortAscending)
        {
            BrowseRepositoryViewItem viewItem = CreateViewItem(
                node,
                treeViewItemIds,
                parentItem);

            parentItem.AddChild(viewItem);
            rows.Add(viewItem);

            if (!treeView.IsExpanded(viewItem.id))
                return;

            if (comparer != null)
            {
                node.Children.Sort(
                    delegate (BrowseRepositoryTreeNode child1, BrowseRepositoryTreeNode child2)
                    {
                        int comparisonResult = comparer.Compare(child1, child2);
                        return sortAscending ? comparisonResult : -comparisonResult;
                    });
            }

            foreach (BrowseRepositoryTreeNode childNode in node.Children)
            {
                if (BrowseRepositoryInfoView.IsDirectory(childNode.Node))
                {
                    AddNode(
                        childNode,
                        treeViewItemIds,
                        treeView,
                        viewItem,
                        rows,
                        comparer,
                        sortAscending);

                    continue;
                }

                AddChildNode(
                    childNode,
                    treeViewItemIds,
                    viewItem,
                    rows);
            }
        }

        static void AddChildNode(
            BrowseRepositoryTreeNode childNode,
            TreeViewItemIds<string, BrowseRepositoryTreeNode> treeViewItemIds,
            BrowseRepositoryViewItem parentItem,
            List<TreeViewItem> rows)
        {
            BrowseRepositoryViewItem viewItem = CreateViewItem(
                childNode,
                treeViewItemIds,
                parentItem);

            rows.Add(viewItem);
        }

        static BrowseRepositoryViewItem CreateViewItem(
            BrowseRepositoryTreeNode node,
            TreeViewItemIds<string, BrowseRepositoryTreeNode> treeViewItemIds,
            TreeViewItem parentItem)
        {
            int nodeId;
            if (!treeViewItemIds.TryGetInfoItemId(node, out nodeId))
                nodeId = treeViewItemIds.AddInfoItem(node);

            return new BrowseRepositoryViewItem(nodeId, node, parentItem.depth + 1);
        }

        BrowseRepositoryTree mBrowseRepositoryTree;

        readonly TreeViewItemIds<string, BrowseRepositoryTreeNode> mTreeViewItemIds =
            new TreeViewItemIds<string, BrowseRepositoryTreeNode>();
        readonly Dictionary<BrowseRepositoryColumn,
            IComparer<BrowseRepositoryTreeNode>> mColumnComparers;
        readonly Action mAfterRowsBuiltAction;
    }
}
