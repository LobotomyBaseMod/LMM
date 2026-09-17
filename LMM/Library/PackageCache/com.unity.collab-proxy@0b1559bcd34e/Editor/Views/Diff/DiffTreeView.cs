using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Commands;
using Codice.Client.Common;
using Codice.CM.Common;
using Codice.Utils;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Diff;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;
#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal class DiffTreeView : PlasticTreeView
    {
        internal GenericMenu Menu { get { return mMenu.Menu; } }

        internal DiffTreeView(
            DiffTreeViewMenu menu,
            Action delayedSelectionChangedAction,
            Action doubleClickAction,
            Action afterItemsChangedAction) : base(showCustomBackground: false)
        {
            mMenu = menu;
            mDelayedSelectionChangedAction = delayedSelectionChangedAction;
            mDoubleClickAction = doubleClickAction;
            mAfterItemsChangedAction = afterItemsChangedAction;

            customFoldoutYOffset = UnityConstants.TREEVIEW_FOLDOUT_Y_OFFSET;

            mDelayedFilterAction = new DelayedActionBySecondsRunner(
                DelayedSearchChanged, UnityConstants.SEARCH_DELAYED_INPUT_ACTION_INTERVAL);

            mDelayedSelectionRunner = new DelayedActionBySecondsRunner(
                DelayedSelectionChanged, UnityConstants.SELECTION_DELAYED_INPUT_ACTION_INTERVAL);
        }

        internal bool ProcessKeyActionForTesting(Event e)
        {
            return mMenu.ProcessKeyActionIfNeeded(e);
        }

        internal void ProcessDoubleClickForTesting()
        {
            mDoubleClickAction();
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

        protected override bool CanChangeExpandedState(TreeViewItem item)
        {
            return item is ChangeCategoryTreeViewItem
                || item is MergeCategoryTreeViewItem;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem rootItem)
        {
            try
            {
                RegenerateRows(
                    mDiffTree,
                    mTreeViewItemIds,
                    this,
                    rootItem,
                    mRows,
                    mExpandCategories);
            }
            finally
            {
                mExpandCategories = false;
            }

            return mRows;
        }

        protected override void CommandEventHandling()
        {
            // NOTE - empty override to prevent crash when pressing ctrl-a in the treeview
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
            args.rowRect.width = Mathf.Max(0f, args.rowRect.width - ROW_RIGHT_MARGIN);

            TreeViewItemGUI(
                args.item, args.rowRect, rowHeight, mDiffTree, args.selected, args.focused);
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

        internal void ClearModel()
        {
            mTreeViewItemIds.Clear();

            mDiffTree = new UnityDiffTree();
        }

        internal void BuildModel(
            WorkspaceInfo wkInfo,
            List<ClientDiff> diffs,
            BranchResolver brResolver,
            bool skipMergeTracking)
        {
            mTreeViewItemIds.Clear();

            mDiffTree.BuildCategories(wkInfo, diffs, brResolver, skipMergeTracking);
        }

        internal void Refilter()
        {
            Filter filter = new Filter(searchString);
            mDiffTree.Filter(filter, ColumnsNames);

            mExpandCategories = true;
        }

        internal void Sort()
        {
            mDiffTree.Sort(
                PlasticLocalization.GetString(PlasticLocalization.Name.PathColumn),
                sortAscending: true);
        }

        internal ClientDiffInfo GetMetaDiff(ClientDiffInfo diff)
        {
            return mDiffTree.GetMetaDiff(diff);
        }

        internal bool SelectionHasMeta()
        {
            if (!HasSelection())
                return false;

            ClientDiffInfo selectedDiff = GetSelectedDiffs(false)[0];

            if (selectedDiff == null)
                return false;

            return mDiffTree.HasMeta(selectedDiff);
        }

        internal List<ClientDiffInfo> GetSelectedDiffs(bool includeMetaFiles)
        {
            List<ClientDiffInfo> result = new List<ClientDiffInfo>();

            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count == 0)
                return result;

            foreach (KeyValuePair<ITreeViewNode, int> item
                in mTreeViewItemIds.GetInfoItems())
            {
                if (!selectedIds.Contains(item.Value))
                    continue;

                if (!(item.Key is ClientDiffInfo))
                    continue;

                result.Add((ClientDiffInfo)item.Key);
            }

            if (includeMetaFiles)
                mDiffTree.FillWithMeta(result);

            return result;
        }

        void DelayedSearchChanged()
        {
            Refilter();

            Sort();

            Reload();

            mAfterItemsChangedAction();

            TableViewOperations.ScrollToSelection(this);
        }

        static void RegenerateRows(
            UnityDiffTree diffTree,
            TreeViewItemIds<IDiffCategory, ITreeViewNode> treeViewItemIds,
            TreeView treeView,
            TreeViewItem rootItem,
            List<TreeViewItem> rows,
            bool expandCategories)
        {
            ClearRows(rootItem, rows);

            List<IDiffCategory> categories = diffTree.GetNodes();

            if (categories == null)
                return;

            foreach (IDiffCategory category in categories)
            {
                if (category is CategoryGroup &&
                    ((CategoryGroup)category).CategoryType == CategoryGroup.Type.MergeCategory)
                {
                    AddMergeCategory(
                        rootItem,
                        category,
                        rows,
                        treeViewItemIds,
                        treeView,
                        expandCategories);
                }

                if (category is ChangeCategory)
                {
                    AddChangeCategory(
                        rootItem,
                        category,
                        rows,
                        treeViewItemIds,
                        treeView,
                        expandCategories);
                }
            }

            if (!expandCategories)
                return;

            treeView.state.expandedIDs = treeViewItemIds.GetCategoryIds();
        }

        static void ClearRows(
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            if (rootItem.hasChildren)
                rootItem.children.Clear();

            rows.Clear();
        }

        static void AddMergeCategory(
            TreeViewItem rootItem,
            IDiffCategory category,
            List<TreeViewItem> rows,
            TreeViewItemIds<IDiffCategory, ITreeViewNode> treeViewItemIds,
            TreeView treeView,
            bool expandCategories)
        {
            int categoryId;
            if (!treeViewItemIds.TryGetCategoryItemId(category, out categoryId))
                categoryId = treeViewItemIds.AddCategoryItem(category);

            MergeCategoryTreeViewItem mergeCategoryTreeViewItem =
                new MergeCategoryTreeViewItem(
                    categoryId,
                    rootItem.depth + 1,
                    (CategoryGroup)category);

            rootItem.AddChild(mergeCategoryTreeViewItem);
            rows.Add(mergeCategoryTreeViewItem);

            if (!expandCategories &&
                !treeView.IsExpanded(mergeCategoryTreeViewItem.id))
                return;

            for (int i = 0; i < category.GetChildrenCount(); i++)
            {
                IDiffCategory child = (IDiffCategory)((ITreeViewNode)category)
                    .GetChild(i);

                AddChangeCategory(
                    mergeCategoryTreeViewItem,
                    child,
                    rows,
                    treeViewItemIds,
                    treeView,
                    expandCategories);
            }
        }

        static void AddChangeCategory(
            TreeViewItem parentItem,
            IDiffCategory category,
            List<TreeViewItem> rows,
            TreeViewItemIds<IDiffCategory, ITreeViewNode> treeViewItemIds,
            TreeView treeView,
            bool expandCategories)
        {
            int categoryId;
            if (!treeViewItemIds.TryGetCategoryItemId(category, out categoryId))
                categoryId = treeViewItemIds.AddCategoryItem(category);

            ChangeCategoryTreeViewItem changeCategoryTreeViewItem =
                new ChangeCategoryTreeViewItem(
                    categoryId,
                    parentItem.depth + 1,
                    (ChangeCategory)category);

            parentItem.AddChild(changeCategoryTreeViewItem);
            rows.Add(changeCategoryTreeViewItem);

            if (!expandCategories &&
                !treeView.IsExpanded(changeCategoryTreeViewItem.id))
                return;

            AddClientDiffs(
                changeCategoryTreeViewItem,
                (ITreeViewNode)category,
                rows,
                treeViewItemIds);
        }

        static void AddClientDiffs(
            TreeViewItem parentItem,
            ITreeViewNode parentNode,
            List<TreeViewItem> rows,
            TreeViewItemIds<IDiffCategory, ITreeViewNode> treeViewItemIds)
        {
            for (int i = 0; i < parentNode.GetChildrenCount(); i++)
            {
                ITreeViewNode child = parentNode.GetChild(i);

                int nodeId;
                if (!treeViewItemIds.TryGetInfoItemId(child, out nodeId))
                    nodeId = treeViewItemIds.AddInfoItem(child);

                TreeViewItem changeTreeViewItem =
                    new ClientDiffTreeViewItem(
                        nodeId,
                        parentItem.depth + 1,
                        (ClientDiffInfo)child);

                parentItem.AddChild(changeTreeViewItem);
                rows.Add(changeTreeViewItem);
            }
        }

        static void TreeViewItemGUI(
            TreeViewItem item,
            Rect rowRect,
            float rowHeight,
            UnityDiffTree diffTree,
            bool isSelected,
            bool isFocused)
        {
            if (item is MergeCategoryTreeViewItem)
            {
                MergeCategoryTreeViewItemGUI(
                    rowRect,
                    (MergeCategoryTreeViewItem)item,
                    isSelected,
                    isFocused);
                return;
            }

            if (item is ChangeCategoryTreeViewItem)
            {
                ChangeCategoryTreeViewItemGUI(
                    rowRect,
                    (ChangeCategoryTreeViewItem)item,
                    isSelected,
                    isFocused);
                return;
            }

            if (item is ClientDiffTreeViewItem)
            {
                ClientDiffTreeViewItemGUI(
                    rowRect,
                    rowHeight,
                    diffTree,
                    (ClientDiffTreeViewItem)item,
                    isSelected,
                    isFocused);
            }
        }

        static void MergeCategoryTreeViewItemGUI(
            Rect rowRect,
            MergeCategoryTreeViewItem item,
            bool isSelected,
            bool isFocused)
        {
            string label = item.Category.CategoryName;
            string infoLabel = PlasticLocalization.Name.ItemsCount.GetString(
                item.Category.GetChildrenCount());

            DrawTreeViewItem.ForIndentedItem(
                rowRect,
                item.depth,
                label,
                infoLabel,
                isSelected,
                isFocused);
        }

        static void ChangeCategoryTreeViewItemGUI(
            Rect rowRect,
            ChangeCategoryTreeViewItem item,
            bool isSelected,
            bool isFocused)
        {
            string label = item.Category.CategoryName;
            string infoLabel = PlasticLocalization.Name.ItemsCount.GetString(
                item.Category.GetChildrenCount());

            DrawTreeViewItem.ForIndentedItem(
                rowRect,
                item.depth,
                label,
                infoLabel,
                isSelected,
                isFocused);
        }

        static void ClientDiffTreeViewItemGUI(
            Rect rowRect,
            float rowHeight,
            UnityDiffTree diffTree,
            ClientDiffTreeViewItem item,
            bool isSelected,
            bool isFocused)
        {
            string label = ClientDiffView.GetColumnText(
                item.Difference.DiffWithMount.Mount.RepSpec,
                item.Difference.DiffWithMount.Difference,
                PlasticLocalization.GetString(PlasticLocalization.Name.PathColumn));

            if (diffTree.HasMeta(item.Difference))
                label = string.Concat(label, UnityConstants.TREEVIEW_META_LABEL);

            Texture icon = GetClientDiffIcon(
                item.Difference.DiffWithMount.Difference.IsDirectory,
                label);

            DrawTreeViewItem.ForItemCell(
                rowRect,
                rowHeight,
                item.depth,
                icon,
                null,
                null,
                label,
                isSelected,
                isFocused,
                false,
                false,
                DrawTreeViewItem.TextTrimming.Path);
        }

        static Texture GetClientDiffIcon(bool isDirectory, string path)
        {
            if (isDirectory)
                return Images.GetFolderIcon();

            return Images.GetFileIconFromCmPath(path);
        }

        bool mExpandCategories;

        TreeViewItemIds<IDiffCategory, ITreeViewNode> mTreeViewItemIds =
            new TreeViewItemIds<IDiffCategory, ITreeViewNode>();

        UnityDiffTree mDiffTree = new UnityDiffTree();

        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;

        static readonly List<string> ColumnsNames = new List<string> {
            PlasticLocalization.GetString(PlasticLocalization.Name.PathColumn)};
        readonly DiffTreeViewMenu mMenu;
        readonly Action mAfterItemsChangedAction;
        readonly Action mDelayedSelectionChangedAction;
        readonly Action mDoubleClickAction;

        const float ROW_RIGHT_MARGIN = 8f;
    }
}
