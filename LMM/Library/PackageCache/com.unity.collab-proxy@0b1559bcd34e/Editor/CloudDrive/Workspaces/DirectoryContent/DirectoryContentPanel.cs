using System.Collections.Generic;
using System.IO;

using Codice.CM.Common;
using Codice.Utils;
using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal interface ICloudWorkspacesOperations
    {
        void ImportInProject(string[] srcPaths, string dstProjectPath);
        void DeleteItems(List<string> paths);
    }

    internal class DirectoryContentPanel : IDirectoryContentMenuOperations
    {
        internal Rect Rect { get { return mLastValidRect; } }
        internal ItemsGridView ItemsGridView { get { return mItemsGridView; } }

        internal interface ICloudWorkspacesTreeView
        {
            void SelectNode(string wkPath, string fullPath);
        }

        internal DirectoryContentPanel(
            ICloudWorkspacesTreeView treeView,
            IDragAndDrop dragAndDrop,
            ICloudWorkspacesOperations cloudWorkspacesOperations,
            EditorWindow parentWindow)
        {
            mTreeView = treeView;
            mCloudWorkspacesOperations = cloudWorkspacesOperations;
            mParentWindow = parentWindow;

            BuildComponents(
                new DirectoryContentPanelMenu(this),
                dragAndDrop,
                parentWindow);
        }

        internal void OnGUI(Rect rect, bool hasFocus)
        {
            if (Event.current.type == EventType.Repaint && rect != mLastValidRect)
            {
                mLastValidRect = rect;
                mParentWindow.Repaint();
            }

            mItemsGridView.OnGUI(
                GetGridAreaRect(mLastValidRect),
                hasFocus);

            EditorGUI.DrawRect(
                GetSeparatorRect(mLastValidRect),
                UnityStyles.Colors.BarBorder);

            DrawItemNameBar.Draw(
                GetItemNameBarRect(mLastValidRect),
                mItemsGridView.GetSelectedItem());
        }

        internal void UpdateItemsForDirectory(ExpandedTreeNode node)
        {
            mItemsDirNode = node;

            string itemsDirPath = node.GetFullPath();

            bool isItemsDirPathChanging =
                 itemsDirPath != mItemsGridView.GetItemsDirPath();

            List<string> itemsPathsToSelect = ItemsGridSelection.
                GetItemsPathsToSelect(isItemsDirPathChanging, mItemsGridView);

            mItemsGridView.UpdateItems(itemsDirPath, node.GetChildren());

            ItemsGridSelection.SelectItems(
                isItemsDirPathChanging,
                itemsPathsToSelect,
                mItemsGridView);

            mParentWindow.Repaint();
        }

        internal void CleanItems()
        {
            mItemsDirNode = null;

            mItemsGridView.CleanItems();

            mParentWindow.Repaint();
        }

        int IDirectoryContentMenuOperations.GetSelectedItemsCount()
        {
            return mItemsGridView.GetSelectedItemsCount();
        }

        bool IDirectoryContentMenuOperations.IsAnyFileSelected()
        {
            List<string> selectedPaths = mItemsGridView.GetSelectedItemsPaths();

            foreach (string path in selectedPaths)
            {
                if (File.Exists(path))
                    return true;
            }

            return false;
        }

        bool IDirectoryContentMenuOperations.IsPathSelected()
        {
            return mItemsDirNode != null;
        }

        void IDirectoryContentMenuOperations.CreateFolder()
        {
            List<string> selectedPaths = mItemsGridView.GetSelectedItemsPaths();

            string parentPath;
            if (selectedPaths.Count == 0 && mItemsDirNode != null)
                parentPath = mItemsDirNode.GetFullPath();
            else if (selectedPaths.Count == 1)
                parentPath = selectedPaths[0];
            else
                return;

            string newName = NewNameDialog.GetNewNameForCreate(
                parentPath, true, mParentWindow);

            if (string.IsNullOrEmpty(newName))
                return;

            Directory.CreateDirectory(
                Path.Combine(parentPath, newName));
        }

        void IDirectoryContentMenuOperations.OpenInExplorer()
        {
            if (mItemsDirNode == null)
                return;

            List<string> selectedPaths = mItemsGridView.GetSelectedItemsPaths();

            if (selectedPaths.Count == 0)
            {
                FileSystemOperation.OpenInExplorer(mItemsDirNode.GetFullPath());
                return;
            }

            FileSystemOperation.OpenInExplorer(selectedPaths);
        }

        void IDirectoryContentMenuOperations.OpenUnityCloud()
        {
            ExpandedTreeNode selectedNode = mItemsGridView.GetSelectedItem();

            if (selectedNode == null)
                selectedNode = mItemsDirNode;

            WorkspaceInfo workspaceInfo = selectedNode.WkInfo;

            RepositorySpec repSpec = PlasticGui.Plastic.API.GetRepositorySpec(workspaceInfo);
            string cloudServer = CloudServer.GetOrganizationName(repSpec.Server);
            RepositoryInfo repInfo = PlasticGui.Plastic.API.GetRepositoryInfo(repSpec);

            if (ExpandedTreeNode.IsDirectory(selectedNode))
            {
                OpenBrowser.TryOpen(UnityUrl.UnityDashboard.CloudDrive.GetWorkspaceDirPath(
                    cloudServer,
                    repInfo.GUID.ToString(),
                    selectedNode.RelativePath));
                return;
            }

            OpenBrowser.TryOpen(UnityUrl.UnityDashboard.CloudDrive.GetWorkspaceItemPath(
                cloudServer,
                repInfo.GUID.ToString(),
                selectedNode.RelativePath));
        }

        void IDirectoryContentMenuOperations.Open()
        {
            ExpandedTreeNode node = mItemsGridView.GetSelectedItem();

            if (node == null)
                return;

            if (!ExpandedTreeNode.IsDirectory(node))
            {
                OpenOperation.OpenFile(node.GetFullPath());
                return;
            }

            mTreeView.SelectNode(node.WkInfo.ClientPath, node.GetFullPath());
        }

        void IDirectoryContentMenuOperations.Delete()
        {
            List<string> selectedPaths = mItemsGridView.GetSelectedItemsPaths();

            if (selectedPaths.Count == 0)
            {
                mCloudWorkspacesOperations.DeleteItems(
                    new List<string> { mItemsDirNode.GetFullPath() });
                return;
            }

            mCloudWorkspacesOperations.DeleteItems(selectedPaths);
        }

        void IDirectoryContentMenuOperations.Rename()
        {
            ExpandedTreeNode node = mItemsGridView.GetSelectedItem();

            string currentPath = node == null ?
                mItemsDirNode.GetFullPath() : node.GetFullPath();

            string parentPath = Path.GetDirectoryName(currentPath);
            string currentName = Path.GetFileName(currentPath);
            bool isDirectory = Directory.Exists(currentPath);

            string newName = NewNameDialog.GetNewNameForRename(
                parentPath,
                currentName,
                isDirectory,
                mParentWindow);

            if (string.IsNullOrEmpty(newName) || newName == currentName)
                return;

            string newPath = Path.Combine(parentPath, newName);

            if (File.Exists(currentPath))
                File.Move(currentPath, newPath);
            else
                Directory.Move(currentPath, newPath);
        }

        void IDirectoryContentMenuOperations.ImportInProject()
        {
            if (mItemsDirNode == null)
                return;

            bool hasItemsSelected = mItemsGridView.GetSelectedItemsCount() > 0;

            string projectPath = ImportInProjectDialog.GetProjectPathToImport(
                hasItemsSelected ?
                    mItemsDirNode.GetFullPath() :
                    Path.GetDirectoryName(mItemsDirNode.GetFullPath()),
                mItemsDirNode.WkInfo.ClientPath,
                mParentWindow);

            if (string.IsNullOrEmpty(projectPath))
                return;

            mCloudWorkspacesOperations.ImportInProject(
                hasItemsSelected ?
                    mItemsGridView.GetSelectedItemsPaths().ToArray() :
                    new string[] { mItemsDirNode.GetFullPath() },
                projectPath);
        }

        void NavigateBackAction()
        {
            if (mItemsDirNode == null)
                return;

            ExpandedTreeNode parentNode = (ExpandedTreeNode)
                ((IPlasticTreeNode)mItemsDirNode).GetParent();

            if (parentNode == null)
                return;

            mTreeView.SelectNode(parentNode.WkInfo.ClientPath, parentNode.GetFullPath());
        }

        void OnItemDoubleClickAction()
        {
            if (mItemsGridView.GetSelectedItemsCount() != 1)
                return;

            ((IDirectoryContentMenuOperations)this).Open();
        }

        static Rect GetGridAreaRect(Rect rect)
        {
            return new Rect(
                rect.x, rect.y,
                rect.width, rect.height - ITEM_NAME_BAR_HEIGHT - SEPARATOR_HEIGHT);
        }

        static Rect GetSeparatorRect(Rect rect)
        {
            return new Rect(
                rect.x, GetItemNameBarRect(rect).y - SEPARATOR_HEIGHT,
                rect.width, SEPARATOR_HEIGHT);
        }

        static Rect GetItemNameBarRect(Rect rect)
        {
            return new Rect(
                rect.x, rect.yMax - ITEM_NAME_BAR_HEIGHT,
                rect.width, ITEM_NAME_BAR_HEIGHT);
        }

        void BuildComponents(
            DirectoryContentPanelMenu menu,
            IDragAndDrop dragAndDrop,
            EditorWindow parentWindow)
        {
            mItemsGridView = new ItemsGridView(
                menu,
                dragAndDrop,
                parentWindow,
                OnItemDoubleClickAction,
                NavigateBackAction);
        }

        ItemsGridView mItemsGridView;
        Rect mLastValidRect;

        ExpandedTreeNode mItemsDirNode;

        readonly ICloudWorkspacesTreeView mTreeView;
        readonly ICloudWorkspacesOperations mCloudWorkspacesOperations;
        readonly EditorWindow mParentWindow;

        const int SEPARATOR_HEIGHT = 1;
        const int ITEM_NAME_BAR_HEIGHT = 20;
    }
}
