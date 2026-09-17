using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Common;
using Codice.CM.Common;
using Codice.Utils;
using PlasticGui;
using PlasticGui.CloudDrive.Workspaces;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.CloudDrive.ShareWorkspace;
using Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.Tree
{
    internal class CloudWorkspacesTreeView :
        PlasticTreeView,
        FillCloudWorkspaces.ICloudWorkspacesTree,
        DirectoryContentPanel.ICloudWorkspacesTreeView,
        ICloudWorkspacesTreeMenuOperations
    {
        internal float Width { get { return mLastValidWidth; } }
        internal WorkspaceInfo WorkspaceToSelect { set { mWorkspaceToSelect = value; } }

        internal CloudWorkspacesTreeView(
            Action delayedSelectionChangedAction,
            IProgressControls progressControls,
            EditorWindow parentWindow) : base(showCustomBackground: false)
        {
            mDelayedSelectionChangedAction = delayedSelectionChangedAction;
            mProgressControls = progressControls;
            mParentWindow = parentWindow;

            mDelayedSelectionRunner = new DelayedActionBySecondsRunner(
                DelayedSelectionChanged, UnityConstants.SELECTION_DELAYED_INPUT_ACTION_INTERVAL);

            mMenu = new CloudWorkspacesTreeViewMenu(this);

            rowHeight = 16;
        }

        protected override bool CanChangeExpandedState(TreeViewItem item)
        {
            if (item is CloudWorkspacesLabelTreeViewItem)
                return false;

            return ((CloudWorkspacesTreeViewItem)item).IsExpandable;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem rootItem)
        {
            RegenerateRows(
                mMyDrivesByWorkspace.Values.ToList(),
                mDrivesSharedWithMeByWorkspace.Values.ToList(),
                mTreeViewItemIds,
                this,
                rootItem,
                mRows);

            SelectWorkspaceIfNeeded();

            if (GetSelectedNode() != null)
                DelayedSelectionChanged();

            return mRows;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            if (item is CloudWorkspacesLabelTreeViewItem)
                return 30f;

            return base.GetCustomRowHeight(row, item);
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint && mLastValidWidth != rect.width)
                mLastValidWidth = rect.width;

            base.OnGUI(rect);

            if (!HasFocus())
                return;

            Event e = Event.current;

            if (e.type != EventType.KeyDown)
                return;

            bool isProcessed = mMenu.ProcessKeyActionIfNeeded(e);

            if (isProcessed)
                e.Use();
        }

        protected override void ContextClickedItem(int id)
        {
            mMenu.Popup();
            Repaint();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is CloudWorkspacesLabelTreeViewItem)
            {
                EditorGUI.LabelField(
                    args.rowRect,
                    ((CloudWorkspacesLabelTreeViewItem)args.item).Label,
                    UnityStyles.Tree.BoldLabelWithMargin);
                return;
            }

            if (args.item is CloudWorkspacesTreeViewItem)
            {
                CloudWorkspacesTreeViewItem item = (CloudWorkspacesTreeViewItem)args.item;

                args.rowRect.width = mLastValidWidth;

                WorkspaceContentTreeViewItemGUI(
                    args.rowRect,
                    this,
                    item,
                    GetProgressControlsDataForNode(
                        item.ExpandedTreeNode,
                        mProgressControlsByWorkspace),
                    args.selected,
                    args.focused);
                return;
            }

            base.RowGUI(args);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            List<int> selectedIdsToSet = new List<int>();

            foreach (int selectedId in selectedIds)
            {
                ExpandedTreeNode selectedNode;
                if (mTreeViewItemIds.TryGetItemById(selectedId, out selectedNode))
                    selectedIdsToSet.Add(selectedId);
            }

            SetSelection(selectedIdsToSet);
            mDelayedSelectionRunner.Run();
        }

        protected override void ExpandedStateChanged()
        {
            IList<int> expandedIds = GetExpanded();

            ReloadExpandedTreeNodes(
                expandedIds.Except(mLastExpandedIds),
                mTreeViewItemIds,
                mMyDrivesByWorkspace,
                mDrivesSharedWithMeByWorkspace);

            mLastExpandedIds = expandedIds;
        }

        internal bool IsOperationRunning()
        {
            foreach (ProgressControlsForViews progressControls
                in mProgressControlsByWorkspace.Values)
            {
                if (progressControls.IsOperationRunning())
                    return true;
            }

            return false;
        }

        internal ProgressControlsForViews GetWorkspaceProgressControls(WorkspaceInfo wkInfo)
        {
            ProgressControlsForViews result;
            if (mProgressControlsByWorkspace.TryGetValue(wkInfo, out result))
                return result;

            mProgressControlsByWorkspace[wkInfo] = new ProgressControlsForViews();
            return mProgressControlsByWorkspace[wkInfo];
        }

        internal WorkspaceInfo GetSelectedWorkspaceInfo()
        {
            ExpandedTreeNode selectedNode = GetSelectedNode();

            if (selectedNode == null)
                return null;

            return selectedNode.WkInfo;
        }

        internal ExpandedTreeNode GetSelectedNode()
        {
            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count != 1)
                return null;

            int selectedId = selectedIds[0];

            ExpandedTreeNode result;
            if (!mTreeViewItemIds.TryGetItemById(selectedId, out result))
                return null;

            return result;
        }

        internal List<ExpandedTreeNode> GetSelectedNodes()
        {
            List<ExpandedTreeNode> result = new List<ExpandedTreeNode>();

            IList<int> selectedIds = GetSelection();

            foreach (int selectedId in selectedIds)
            {
                ExpandedTreeNode selectedNode;
                if (!mTreeViewItemIds.TryGetItemById(selectedId, out selectedNode))
                    continue;

                result.Add(selectedNode);
            }

            return result;
        }

        internal void Update(EditorWindow parentWindow)
        {
            foreach (ProgressControlsForViews progressControls
                in mProgressControlsByWorkspace.Values)
            {
                progressControls.UpdateProgress(parentWindow);
            }
        }

        void FillCloudWorkspaces.ICloudWorkspacesTree.ExpandedTreesRetrieved(
            List<ExpandedTree> myDrives,
            List<ExpandedTree> drivesSharedWithMe)
        {
            mMyDrivesByWorkspace = BuildExpandedTrees(myDrives);
            mDrivesSharedWithMeByWorkspace = BuildExpandedTrees(drivesSharedWithMe);

            BuildProgressControls(
                myDrives.Concat(drivesSharedWithMe),
                mProgressControlsByWorkspace);

            Reload();
        }

        void DirectoryContentPanel.ICloudWorkspacesTreeView.SelectNode(
            string wkPath, string fullPath)
        {
            SelectNode(wkPath, fullPath);
        }

        int ICloudWorkspacesTreeMenuOperations.GetSelectedItemsCount()
        {
            return GetSelection().Count;
        }

        bool ICloudWorkspacesTreeMenuOperations.IsAnyNonRootItemSelected()
        {
            List<ExpandedTreeNode> selectedNodes = GetSelectedNodes();

            foreach (ExpandedTreeNode node in selectedNodes)
            {
                if (!ExpandedTreeNode.IsRootNode(node))
                    return true;
            }

            return false;
        }

        bool ICloudWorkspacesTreeMenuOperations.IsAnySharedDriveSelected()
        {
            List<ExpandedTreeNode> selectedNodes = GetSelectedNodes();

            foreach (ExpandedTreeNode node in selectedNodes)
            {
                if (mDrivesSharedWithMeByWorkspace.ContainsKey(node.WkInfo))
                    return true;
            }

            return false;
        }

        void ICloudWorkspacesTreeMenuOperations.OpenInExplorer()
        {
            List<ExpandedTreeNode> selectedNodes = GetSelectedNodes();

            if (selectedNodes.Count != 1)
                return;

            FileSystemOperation.OpenInExplorer(selectedNodes[0].GetFullPath());
        }

        void ICloudWorkspacesTreeMenuOperations.OpenUnityCloud()
        {
            List<ExpandedTreeNode> selectedNodes = GetSelectedNodes();

            if (selectedNodes.Count != 1)
                return;

            WorkspaceInfo workspaceInfo = selectedNodes[0].WkInfo;

            RepositorySpec repSpec = PlasticGui.Plastic.API.GetRepositorySpec(workspaceInfo);
            string cloudServer = CloudServer.GetOrganizationName(repSpec.Server);
            RepositoryInfo repInfo = PlasticGui.Plastic.API.GetRepositoryInfo(repSpec);

            OpenBrowser.TryOpen(UnityUrl.UnityDashboard.
                CloudDrive.GetWorkspace(cloudServer, repInfo.GUID.ToString()));
        }

        void ICloudWorkspacesTreeMenuOperations.Delete()
        {
            List<ExpandedTreeNode> selectedNodes = GetSelectedNodes();

            if (selectedNodes.Count != 1)
                return;

            WorkspaceInfo workspaceInfo = selectedNodes[0].WkInfo;

            ProgressControlsForViews workspaceProgressControls =
                GetWorkspaceProgressControls(workspaceInfo);

            if (workspaceProgressControls.IsOperationRunning())
            {
                GuiMessage.ShowInformation(
                    PlasticLocalization.Name.OperationInProgress.GetString());
                return;
            }

            string workspaceName = Path.GetFileName(workspaceInfo.ClientPath);
            if (!DeleteWorkspaceDialog.UserConfirmsDelete(workspaceName, mParentWindow))
                return;

            DeleteWorkspaceOperation.DeleteWorkspace(
                workspaceInfo,
                mProgressControls,
                Reload);
        }

        void ICloudWorkspacesTreeMenuOperations.Share()
        {
            List<ExpandedTreeNode> selectedNodes = GetSelectedNodes();

            if (selectedNodes.Count != 1)
                return;

            WorkspaceInfo workspaceInfo = selectedNodes[0].WkInfo;

            ProgressControlsForViews workspaceProgressControls =
                GetWorkspaceProgressControls(workspaceInfo);

            if (workspaceProgressControls.IsOperationRunning())
            {
                GuiMessage.ShowInformation(
                    PlasticLocalization.Name.OperationInProgress.GetString());
                return;
            }

            List<SecurityMember> collaboratorsToAdd;
            List<SecurityMember> collaboratorsToRemove;
            if (!ShareWorkspaceDialog.ShareWorkspace(
                    workspaceInfo,
                    mParentWindow,
                    out collaboratorsToAdd,
                    out collaboratorsToRemove))
                return;

            WorkspaceShareOperations.ShareWorkspace(
                workspaceInfo, collaboratorsToAdd, mProgressControls);
            WorkspaceShareOperations.UnshareWorkspace(
                workspaceInfo, collaboratorsToRemove, mProgressControls);
        }

        void ICloudWorkspacesTreeMenuOperations.UnshareWithMe()
        {
            List<ExpandedTreeNode> selectedNodes = GetSelectedNodes();

            if (selectedNodes.Count != 1)
                return;

            WorkspaceInfo workspaceInfo = selectedNodes[0].WkInfo;

            ProgressControlsForViews workspaceProgressControls =
                GetWorkspaceProgressControls(workspaceInfo);

            if (workspaceProgressControls.IsOperationRunning())
            {
                GuiMessage.ShowInformation(
                    PlasticLocalization.Name.OperationInProgress.GetString());
                return;
            }

            if (!GuiMessage.ShowQuestion(
                PlasticLocalization.Name.UnshareWithMeMenuItem.GetString(),
                PlasticLocalization.Name.UnshareConfirmationMessage.GetString(),
                PlasticLocalization.Name.UnshareButton.GetString()))
            {
                return;
            }

            WorkspaceShareOperations.UnshareWorkspaceWithMe(
                workspaceInfo,
                mProgressControls,
                Reload);
        }

        void SelectWorkspaceIfNeeded()
        {
            if (mWorkspaceToSelect == null)
                return;

            int idToSelect;
            if (!mTreeViewItemIds.TryGetItemIdByKey(mWorkspaceToSelect.ClientPath, out idToSelect))
                return;

            SetSelection(new List<int> { idToSelect });
            mWorkspaceToSelect = null;
        }

        void DelayedSelectionChanged()
        {
            if (mIsSelectionChangedEventDisabled)
                return;

            IList<int> selectedIds = GetSelection();

            if (selectedIds.Count == 1)
                ReloadExpandedTreeNodeIfChildrenNotLoaded(
                    selectedIds[0],
                    mTreeViewItemIds,
                    mMyDrivesByWorkspace,
                    mDrivesSharedWithMeByWorkspace);

            mDelayedSelectionChangedAction();
        }

        void SelectNode(string wkPath, string fullPath)
        {
            if (ExpandAncestors(wkPath, fullPath, mTreeViewItemIds, this))
                Reload();

            int idToSelect;
            if (!mTreeViewItemIds.TryGetItemIdByKey(fullPath, out idToSelect))
                return;

            mIsSelectionChangedEventDisabled = true;

            try
            {
                TableViewOperations.SetSelectionAndScroll(
                    this, new List<int> { idToSelect });
            }
            finally
            {
                mIsSelectionChangedEventDisabled = false;
            }

            DelayedSelectionChanged();
        }

        static void WorkspaceContentTreeViewItemGUI(
            Rect rowRect,
            CloudWorkspacesTreeView tree,
            CloudWorkspacesTreeViewItem item,
            ProgressControlsForViews.Data progressControlsData,
            bool isSelected,
            bool isFocused)
        {
            DrawTreeViewItem.ForIndentedItemWithIcon(
                rowRect,
                tree.rowHeight,
                item.depth,
                Path.GetFileName(item.ExpandedTreeNode.GetFullPath()),
                string.Empty,
                GetItemIcon(tree, item),
                isSelected,
                isFocused);

            if (progressControlsData == null || !progressControlsData.IsOperationRunning)
                return;

            DoProgress(rowRect, progressControlsData);
        }

        static void DoProgress(
            Rect rowRect,
            ProgressControlsForViews.Data progressControlsData)
        {
            GUILayout.BeginArea(rowRect);

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            LoadingSpinner.OnGUI(
                progressControlsData.ProgressPercent,
                progressControlsData.ProgressMessage);

            GUILayout.Space(2);

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        static void ReloadExpandedTreeNodes(
            IEnumerable<int> idsToReload,
            TreeViewItemIds<ExpandedTreeNode> treeViewItemIds,
            Dictionary<WorkspaceInfo, ExpandedTree> myDrivesByWorkspace,
            Dictionary<WorkspaceInfo, ExpandedTree> drivesSharedWithMeByWorkspace)
        {
            foreach (int id in idsToReload)
            {
                ExpandedTreeNode node;
                if (!treeViewItemIds.TryGetItemById(id, out node))
                    continue;

                ExpandedTree expandedTree;
                if (!myDrivesByWorkspace.TryGetValue(node.WkInfo, out expandedTree) &&
                    !drivesSharedWithMeByWorkspace.TryGetValue(node.WkInfo, out expandedTree))
                    continue;

                expandedTree.ReloadNode(node);
            }
        }

        static void ReloadExpandedTreeNodeIfChildrenNotLoaded(
            int idToReload,
            TreeViewItemIds<ExpandedTreeNode> treeViewItemIds,
            Dictionary<WorkspaceInfo, ExpandedTree> myDrivesByWorkspace,
            Dictionary<WorkspaceInfo, ExpandedTree> drivesSharedWithMeByWorkspace)
        {
            ExpandedTreeNode node;
            if (!treeViewItemIds.TryGetItemById(idToReload, out node))
                return;

            if (node.GetChildren() != null)
                return;

            ExpandedTree expandedTree;
            if (!myDrivesByWorkspace.TryGetValue(node.WkInfo, out expandedTree) &&
                !drivesSharedWithMeByWorkspace.TryGetValue(node.WkInfo, out expandedTree))
                return;

            expandedTree.ReloadNode(node);
        }

        static bool ExpandAncestors(
            string wkPath,
            string nodePath,
            TreeViewItemIds<ExpandedTreeNode> treeViewItemIds,
            CloudWorkspacesTreeView treeView)
        {
            List<string> ancestors = GetAncestorsPaths(wkPath, nodePath);
            bool result = false;

            foreach (string ancestor in ancestors)
            {
                int ancestorId;
                if (!treeViewItemIds.TryGetItemIdByKey(ancestor, out ancestorId))
                    continue;

                if (treeView.IsExpanded(ancestorId))
                    continue;

                treeView.SetExpanded(ancestorId, true);
                result = true;
            }

            return result;
        }

        static void RegenerateRows(
            List<ExpandedTree> myDrives,
            List<ExpandedTree> drivesSharedWithMe,
            TreeViewItemIds<ExpandedTreeNode> treeViewItemIds,
            CloudWorkspacesTreeView treeView,
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            ClearRows(rootItem, rows);

            treeViewItemIds.ClearItems();

            AddLabelRow(PlasticLocalization.Name.MyDrivesLabel.GetString(), treeViewItemIds, rows);

            AddExpandedTrees(myDrives, treeViewItemIds, treeView, rootItem, rows);

            AddLabelRow(
                PlasticLocalization.Name.DrivesSharedWithMeLabel.GetString(), treeViewItemIds, rows);

            AddExpandedTrees(drivesSharedWithMe, treeViewItemIds, treeView, rootItem, rows);
        }

        static void AddExpandedTrees(
            List<ExpandedTree> expandedTrees,
            TreeViewItemIds<ExpandedTreeNode> treeViewItemIds,
            CloudWorkspacesTreeView treeView,
            TreeViewItem rootItem,
            List<TreeViewItem> rows)
        {
            foreach (ExpandedTree expandedTree in expandedTrees)
            {
                ExpandedTreeNode rootNode = expandedTree.GetRootNode();

                if (rootNode == null)
                    continue;

                AddNode(
                    rootNode,
                    expandedTree,
                    treeViewItemIds,
                    treeView,
                    rootItem,
                    rows);
            }
        }

        static void AddLabelRow(
            string label,
            TreeViewItemIds<ExpandedTreeNode> treeViewItemIds,
            List<TreeViewItem> rows)
        {
            int nodeId;
            if (!treeViewItemIds.TryGetItemIdByKey(label, out nodeId))
                nodeId = treeViewItemIds.AddItemIdByKey(label);

            rows.Add(new CloudWorkspacesLabelTreeViewItem(nodeId, label));
        }

        static void AddNode(
            ExpandedTreeNode node,
            ExpandedTree expandedTree,
            TreeViewItemIds<ExpandedTreeNode> treeViewItemIds,
            CloudWorkspacesTreeView treeView,
            TreeViewItem parentItem,
            List<TreeViewItem> rows)
        {
            if (!ExpandedTreeNode.IsOnDisk(node))
                return;

            int nodeId;
            if (!treeViewItemIds.TryGetItemIdByKey(node.GetFullPath(), out nodeId))
                nodeId = treeViewItemIds.AddItemIdByKey(node.GetFullPath());

            treeViewItemIds.AddItemById(nodeId, node);

            CloudWorkspacesTreeViewItem viewItem =
                new CloudWorkspacesTreeViewItem(
                    nodeId,
                    node,
                    Directory.GetDirectories(node.GetFullPath()).Length > 0,
                    parentItem.depth + 1);

            rows.Add(viewItem);
            parentItem.AddChild(viewItem);

            if (!treeView.IsExpanded(nodeId))
                return;

            if (node.GetChildren() == null)
                expandedTree.ReloadNode(node);

            foreach (ExpandedTreeNode childNode in node.GetChildren())
            {
                if (!ExpandedTreeNode.IsDirectory(childNode))
                    continue;

                AddNode(
                    childNode,
                    expandedTree,
                    treeViewItemIds,
                    treeView,
                    viewItem,
                    rows);
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

        static Texture GetItemIcon(
            CloudWorkspacesTreeView tree,
            CloudWorkspacesTreeViewItem item)
        {
            if (item.parent.id == 0)
                return Images.GetCloudWorkspaceIcon();

            if (tree.IsExpanded(item.id))
                return Images.GetFolderOpenedIcon();

            return Images.GetFolderIcon();
        }

        static Dictionary<WorkspaceInfo, ExpandedTree> BuildExpandedTrees(
            List<ExpandedTree> expandedTrees)
        {
            Dictionary<WorkspaceInfo, ExpandedTree> result =
                new Dictionary<WorkspaceInfo, ExpandedTree>();

            foreach (ExpandedTree expandedTree in expandedTrees)
                result[expandedTree.WkInfo] = expandedTree;

            return result;
        }

        static void BuildProgressControls(
            IEnumerable<ExpandedTree> expandedTrees,
            Dictionary<WorkspaceInfo, ProgressControlsForViews> progressControlsByWorkspace)
        {
            List<WorkspaceInfo> workspaceInfos = new List<WorkspaceInfo>();

            foreach (ExpandedTree expandedTree in expandedTrees)
                workspaceInfos.Add(expandedTree.GetRootNode().WkInfo);

            for (int i = progressControlsByWorkspace.Keys.Count - 1; i >= 0; i--)
            {
                WorkspaceInfo wkInfo = progressControlsByWorkspace.Keys.ElementAt(i);

                if (!workspaceInfos.Contains(wkInfo))
                    progressControlsByWorkspace.Remove(wkInfo);
            }

            foreach (WorkspaceInfo wkInfo in workspaceInfos)
            {
                if (progressControlsByWorkspace.ContainsKey(wkInfo))
                    continue;

                progressControlsByWorkspace[wkInfo] = new ProgressControlsForViews();
            }
        }

        static List<string> GetAncestorsPaths(
            string wkPath,
            string nodePath)
        {
            List<string> ancestors = new List<string>();

            while (!PathHelper.IsSamePath(wkPath, nodePath))
            {
                nodePath = Directory.GetParent(nodePath).FullName;
                ancestors.Add(nodePath);
            }

            ancestors.Reverse();
            return ancestors;
        }

        static ProgressControlsForViews.Data GetProgressControlsDataForNode(
            ExpandedTreeNode node,
            Dictionary<WorkspaceInfo, ProgressControlsForViews> progressControlsByWorkspace)
        {
            if (!ExpandedTreeNode.IsRootNode(node))
                return null;

            ProgressControlsForViews progressControls;
            if (!progressControlsByWorkspace.TryGetValue(
                    node.WkInfo,
                    out progressControls))
                return null;

            return progressControls.ProgressData;
        }

        float mLastValidWidth;
        bool mIsSelectionChangedEventDisabled = false;
        IEnumerable<int> mLastExpandedIds = new List<int>();
        Dictionary<WorkspaceInfo, ExpandedTree> mMyDrivesByWorkspace =
            new Dictionary<WorkspaceInfo, ExpandedTree>();
        Dictionary<WorkspaceInfo, ExpandedTree> mDrivesSharedWithMeByWorkspace =
            new Dictionary<WorkspaceInfo, ExpandedTree>();
        WorkspaceInfo mWorkspaceToSelect;

        readonly CloudWorkspacesTreeViewMenu mMenu;
        readonly DelayedActionBySecondsRunner mDelayedSelectionRunner;

        readonly Action mDelayedSelectionChangedAction;
        readonly IProgressControls mProgressControls;
        readonly EditorWindow mParentWindow;

        readonly TreeViewItemIds<ExpandedTreeNode> mTreeViewItemIds =
            new TreeViewItemIds<ExpandedTreeNode>();
        readonly Dictionary<WorkspaceInfo, ProgressControlsForViews> mProgressControlsByWorkspace =
            new Dictionary<WorkspaceInfo, ProgressControlsForViews>();
    }
}
