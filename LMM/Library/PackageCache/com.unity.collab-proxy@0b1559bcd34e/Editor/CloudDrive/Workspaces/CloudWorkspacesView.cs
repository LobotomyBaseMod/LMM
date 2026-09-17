using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.CloudDrive;
using PlasticGui.CloudDrive.Workspaces;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace;
using Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent;
using Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DragAndDrop;
using Unity.PlasticSCM.Editor.CloudDrive.Workspaces.Tree;
using Unity.PlasticSCM.Editor.UI.Errors;
using Unity.PlasticSCM.Editor.UI.Progress;

#if !UNITY_6000_3_OR_NEWER
using SplitterGUILayout = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterGUILayout;
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces
{
    internal interface IDragAndDrop
    {
        List<string> GetDragSourcePaths();

        void ClearDragTargetPath();

        void SetDragTargetPath(string path);

        void ExecuteDropAction(string[] paths);
    }

    internal class CloudWorkspacesView :
        IRefreshableView,
        INotifyUpdatePaths,
        FillOrganizationsAndProjects.INotify,
        ICloudWorkspacesOperations,
        IDragAndDrop
    {
        internal const float MIN_WIDTH =
            TREE_VIEW_MIN_SIZE +
            SPLITTER_WIDTH +
            DIRECTORY_CONTENT_MIN_SIZE;

        internal CloudWorkspacesView(
            string proposedCloudServer,
            string proposedProject,
            WorkspaceInfo workspaceToSelect,
            IPlasticWebRestApi restApi,
            IPlasticAPI plasticApi,
            IProgressControls progressControls,
            EditorWindow parentWindow)
        {
            mRestApi = restApi;
            mPlasticApi = plasticApi;
            mProgressControls = progressControls;
            mParentWindow = parentWindow;

            mFillCloudWorkspacesView = new FillCloudWorkspaces();

            InitializeProposedOrganizationProject(proposedCloudServer, proposedProject);
            BuildComponents(workspaceToSelect, parentWindow);

            mSplitterState = new SplitterState(
                SplitterSettings.Load(
                    UnityConstants.CLOUD_WORKSPACES_SPLITTER_SETTINGS_NAME,
                    new float[] { 0.25f, 0.75f }),
                new int[] { TREE_VIEW_MIN_SIZE, DIRECTORY_CONTENT_MIN_SIZE },
                new int[] { 100000, 100000 }
            );

#if UNITY_6000_3_OR_NEWER
            UnityEditor.DragAndDrop.AddDropHandlerV2(ProjectBrowserDropHandler);
#else
            UnityEditor.DragAndDrop.AddDropHandler(ProjectBrowserDropHandler);
#endif

            Refresh();
        }

        internal void SelectWorkspaceAndCopyPaths(
            string organization,
            string project,
            WorkspaceInfo wkInfo,
            string[] assetPaths,
            string dstRelativePath)
        {
            OnOrganizationSelected(organization);
            OnProjectSelected(project);
            mCloudWorkspacesTreeView.WorkspaceToSelect = wkInfo;
            mCloudWorkspacesTreeView.Reload();

            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                TrackFeatureUseEvent.Features.UnityPackage.AddAssetsToCloudDrive);

            CopyPaths(
                assetPaths,
                AssetsPath.GetFullPath.ForPath(string.Concat(wkInfo.ClientPath, dstRelativePath)),
                mCloudWorkspacesTreeView,
                wkInfo,
                mParentWindow,
                this,
                beforeStartingOperationDelegate: null,
                endOperationDelegate: null,
                shouldRefreshView: true);
        }

        internal bool IsOperationRunning()
        {
            return mCloudWorkspacesTreeView.IsOperationRunning();
        }

        internal void AutoRefresh()
        {
            Refresh();
        }

        internal void OnGUI(Rect rect, bool hasFocus)
        {
            Event e = Event.current;

            if (ProcessTabKeyActionIfNeeded(e, mCloudWorkspacesTreeView))
                e.Use();

            if (ProcessF5KeyActionIfNeeded(e, this))
                e.Use();

            UpdateDirectoryContentFocusIfNeeded(
                e, mCloudWorkspacesTreeView, mDirectoryContentPanel.Rect);

            DoContentArea(
                mCloudWorkspacesTreeView,
                mDirectoryContentPanel,
                mSplitterState,
                rect.width,
                rect.height - EditorStyles.toolbar.fixedHeight,
                hasFocus);

            DoToolbar(
                GetToolbarRect(rect),
                mCloudWorkspacesTreeView.Width,
                mOrganizations,
                mProjects,
                this);
        }

        internal void OnEnable()
        {
#if UNITY_6000_3_OR_NEWER
            UnityEditor.DragAndDrop.AddDropHandlerV2(ProjectBrowserDropHandler);
#else
            UnityEditor.DragAndDrop.AddDropHandler(ProjectBrowserDropHandler);
#endif
        }

        internal void OnDisable()
        {
#if UNITY_6000_3_OR_NEWER
            UnityEditor.DragAndDrop.RemoveDropHandlerV2(ProjectBrowserDropHandler);
#else
            UnityEditor.DragAndDrop.RemoveDropHandler(ProjectBrowserDropHandler);
#endif

            SplitterSettings.Save(
                mSplitterState,
                UnityConstants.CLOUD_WORKSPACES_SPLITTER_SETTINGS_NAME);
        }

        internal void Update(EditorWindow parentWindow)
        {
            mCloudWorkspacesTreeView.Update(parentWindow);
        }

        void IRefreshableView.Refresh()
        {
            Refresh();
        }

        void INotifyUpdatePaths.RefreshViewSilently()
        {
            mFillCloudWorkspacesView.LoadExpandedTrees(
                mSelectedOrganization,
                mSelectedProject,
                mCloudWorkspacesTreeView,
                progressControls: null);
        }

        void INotifyUpdatePaths.ShowErrors(string wkName, List<ErrorMessage> errors)
        {
            ErrorsDialog.ShowDialog(
                PlasticLocalization.Name.CopyFileErrorsTitle.GetString(),
                PlasticLocalization.Name.CopyFileErrorsMessage.GetString(wkName),
                errors,
                mParentWindow);
        }

        void FillOrganizationsAndProjects.INotify.OrganizationsRetrieved(List<string> organizations)
        {
            mOrganizations = organizations;

            mSelectedOrganization = GetDefaultValue(mProposedOrganization, mOrganizations);

            if (mSelectedOrganization == null)
                return;

            OnOrganizationSelected(mSelectedOrganization);
        }

        void FillOrganizationsAndProjects.INotify.ProjectsRetrieved(List<string> projects)
        {
            mProjects = projects;
            mProjects.Insert(0, PlasticLocalization.Name.AllProjects.GetString());
            mProjects.Insert(1, string.Empty);

            mSelectedProject = GetDefaultValue(mProposedProject, mProjects);

            if (mSelectedProject == null)
                return;

            OnProjectSelected(mSelectedProject);
        }

        List<string> IDragAndDrop.GetDragSourcePaths()
        {
            return mDirectoryContentPanel.ItemsGridView.GetSelectedItemsPaths();
        }

        void IDragAndDrop.ClearDragTargetPath()
        {
            mDragTargetPath = null;
        }

        void IDragAndDrop.SetDragTargetPath(string path)
        {
            mDragTargetPath = path;
        }

        void IDragAndDrop.ExecuteDropAction(string[] paths)
        {
            WorkspaceInfo wkInfo = mCloudWorkspacesTreeView.GetSelectedWorkspaceInfo();

            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                TrackFeatureUseEvent.Features.UnityPackage.CloudDriveDragToWindow);

            CopyPaths(
                paths,
                mDragTargetPath,
                mCloudWorkspacesTreeView,
                wkInfo,
                mParentWindow,
                this,
                beforeStartingOperationDelegate: null,
                endOperationDelegate: null,
                shouldRefreshView: true);
        }

        void ICloudWorkspacesOperations.DeleteItems(List<string> paths)
        {
            WorkspaceInfo wkInfo = mCloudWorkspacesTreeView.GetSelectedWorkspaceInfo();

            ProgressControlsForViews workspaceProgressControls =
                mCloudWorkspacesTreeView.GetWorkspaceProgressControls(wkInfo);

            if (workspaceProgressControls.IsOperationRunning())
            {
                GuiMessage.ShowInformation(PlasticLocalization.Name.OperationInProgress.GetString());
                return;
            }

            DeletePathsOperation.DeleteRecursively(
                paths.ToArray(),
                Path.GetFileName(wkInfo.ClientPath),
                this,
                workspaceProgressControls);
        }

        void ICloudWorkspacesOperations.ImportInProject(string[] paths, string projectPath)
        {
            WorkspaceInfo wkInfo = mCloudWorkspacesTreeView.GetSelectedWorkspaceInfo();

            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                TrackFeatureUseEvent.Features.UnityPackage.CloudDriveImportToProject);

            CopyPaths(
                paths,
                AssetsPath.GetFullPath.ForPath(string.Join(ProjectPath.Get(), projectPath)),
                mCloudWorkspacesTreeView,
                wkInfo,
                mParentWindow,
                this,
                BeforeCopyPathsForProjectBrowserDropAction,
                AfterCopyPathsForProjectBrowserDropAction,
                shouldRefreshView: false);
        }

        void Refresh()
        {
            FillOrganizationsAndProjects.LoadOrganizations(
                mRestApi, mPlasticApi, mProgressControls, this);

            RefreshTree();
        }

        void RefreshTree()
        {
            mFillCloudWorkspacesView.LoadExpandedTrees(
                mSelectedOrganization,
                mSelectedProject,
                mCloudWorkspacesTreeView,
                mProgressControls);
        }

        void OnTreeViewSelectionChanged()
        {
            List<ExpandedTreeNode> selectedNodes = CloudWorkspacesSelection.
                GetSelectedNodes(mCloudWorkspacesTreeView);

            if (selectedNodes.Count != 1)
            {
                mDirectoryContentPanel.CleanItems();
                return;
            }

            mDirectoryContentPanel.UpdateItemsForDirectory(selectedNodes[0]);
        }

        DragAndDropVisualMode ProjectBrowserDropHandler(
#if UNITY_6000_3_OR_NEWER
            EntityId dragEntityId, string dropUponPath, bool perform)
#else
            int dragInstanceId, string dropUponPath, bool perform)
#endif
        {
            if (perform == false)
                return DragAndDropVisualMode.None;

            if (UnityEditor.DragAndDrop.objectReferences.Length == 0 ||
                UnityEditor.DragAndDrop.objectReferences[0] != mParentWindow)
                return DragAndDropVisualMode.None;

            WorkspaceInfo wkInfo = mCloudWorkspacesTreeView.GetSelectedWorkspaceInfo();

            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                TrackFeatureUseEvent.Features.UnityPackage.CloudDriveDragToProjectView);

            CopyPaths(
                UnityEditor.DragAndDrop.paths,
                AssetsPath.GetFullPath.ForPath(dropUponPath),
                mCloudWorkspacesTreeView,
                wkInfo,
                mParentWindow,
                this,
                BeforeCopyPathsForProjectBrowserDropAction,
                AfterCopyPathsForProjectBrowserDropAction,
                shouldRefreshView: false);

            return DragAndDropVisualMode.Copy;
        }

        void BeforeCopyPathsForProjectBrowserDropAction()
        {
            if (mCopyPathsForProjectBrowserDropCount == 0)
                RefreshAsset.BeforeLongAssetOperation();

            mCopyPathsForProjectBrowserDropCount++;
        }

        void AfterCopyPathsForProjectBrowserDropAction()
        {
            if (mCopyPathsForProjectBrowserDropCount == 1)
                RefreshAsset.AfterLongAssetOperation();

            mCopyPathsForProjectBrowserDropCount--;
        }

        void OnOrganizationSelected(object organization)
        {
            mSelectedOrganization = organization.ToString();
            mProposedOrganization = mSelectedOrganization;
            mSelectedProject = null;

            PlasticGuiConfig.Get().Configuration.LastUsedCloudDriveServer = mSelectedOrganization;
            PlasticGuiConfig.Get().Save();

            mProjects.Clear();

            if (!CloudServer.IsUnityOrganization(mSelectedOrganization))
            {
                OnProjectSelected(string.Empty);
                return;
            }

            FillOrganizationsAndProjects.LoadProjects(
                mSelectedOrganization, mPlasticApi, mProgressControls, this);
        }

        void OnProjectSelected(object project)
        {
            mSelectedProject = project.ToString();
            mProposedProject = mSelectedProject;

            PlasticGuiConfig.Get().Configuration.LastUsedCloudDriveProject = mSelectedProject;
            PlasticGuiConfig.Get().Save();

            RefreshTree();
        }

        static void CopyPaths(
            string[] paths,
            string targetPath,
            CloudWorkspacesTreeView cloudWorkspacesTreeView,
            WorkspaceInfo wkInfo,
            EditorWindow parentWindow,
            INotifyUpdatePaths notify,
            Action beforeStartingOperationDelegate,
            Action endOperationDelegate,
            bool shouldRefreshView)
        {
            ProgressControlsForViews workspaceProgressControls =
                cloudWorkspacesTreeView.GetWorkspaceProgressControls(wkInfo);

            if (workspaceProgressControls.IsOperationRunning())
            {
                GuiMessage.ShowInformation(PlasticLocalization.Name.OperationInProgress.GetString());
                return;
            }

            CopyPathsOperation.CopyRecursivelyTo(
                paths,
                targetPath,
                Path.GetFileName(wkInfo.ClientPath),
                new AskUserForExistingFile(parentWindow),
                notify,
                workspaceProgressControls,
                beforeStartingOperationDelegate,
                endOperationDelegate,
                shouldRefreshView);
        }

        static bool ProcessTabKeyActionIfNeeded(
            Event e,
            CloudWorkspacesTreeView treeView)
        {
            if (!Keyboard.IsTabPressed(e))
                return false;

            if (!treeView.HasFocus())
            {
                treeView.SetFocus();
                return true;
            }

            GUI.FocusControl(null);
            return true;
        }

        static bool ProcessF5KeyActionIfNeeded(
            Event e,
            IRefreshableView view)
        {
            if (!Keyboard.IsKeyPressed(e, KeyCode.F5))
                return false;

            view.Refresh();
            return true;
        }

        static void UpdateDirectoryContentFocusIfNeeded(
            Event e,
            CloudWorkspacesTreeView treeView,
            Rect directoryContentRect)
        {
            if (e.type != EventType.MouseDown)
                return;

            if (!directoryContentRect.Contains(e.mousePosition))
                return;

            if (!treeView.HasFocus())
                return;

            GUI.FocusControl(null);
        }

        void DoContentArea(
            CloudWorkspacesTreeView cloudWorkspacesTreeView,
            DirectoryContentPanel directoryContentPanel,
            SplitterState splitterState,
            float width,
            float height,
            bool hasFocus)
        {
            SplitterGUILayout.BeginHorizontalSplit(splitterState);
            Rect treeAreaRect = GetTreeRect(width, height);
            Rect directoryContentPanelRect = GetDirectoryContentPanelRect(width, height);
            SplitterGUILayout.EndHorizontalSplit();

            DoTreeArea(cloudWorkspacesTreeView, treeAreaRect);

            directoryContentPanel.OnGUI(
                directoryContentPanelRect,
                hasFocus && !cloudWorkspacesTreeView.HasFocus());

            EditorGUI.DrawRect(
                GetSplitterRect(directoryContentPanel.Rect),
                UnityStyles.Colors.BarBorder);
        }

        void DoTreeArea(
            CloudWorkspacesTreeView cloudWorkspacesTreeView,
            Rect treeAreaRect)
        {
            Rect newDriveButtonRect = new Rect(
                treeAreaRect.x + NEW_DRIVE_BUTTON_MARGIN,
                treeAreaRect.y + NEW_DRIVE_BUTTON_MARGIN,
                treeAreaRect.width - 2 * NEW_DRIVE_BUTTON_MARGIN,
                EditorStyles.miniButton.fixedHeight + NEW_DRIVE_BUTTON_MARGIN);

            if (GUI.Button(
                newDriveButtonRect,
                PlasticLocalization.Name.NewDriveButton.GetString(),
                EditorStyles.miniButton))
            {
                WorkspaceCreationData wkCreationData = CreateWorkspaceDialog.CreateWorkspace(
                    mSelectedOrganization,
                    mSelectedProject,
                    mRestApi,
                    mPlasticApi,
                    mParentWindow);

                CreateWorkspaceOperation.CreateWorkspace(
                    wkCreationData, mPlasticApi, mProgressControls,
                    (createdWorkspace) =>
                    {
                        TrackFeatureUseEvent.For(
                            PlasticGui.Plastic.API.GetRepositorySpec(createdWorkspace),
                            TrackFeatureUseEvent.Features.UnityPackage.CloudDriveCreateWorkspaceFromNewDriveButton);

                        OnOrganizationSelected(wkCreationData.CloudServer);

                        mProposedProject = CloudProjectRepository.GetProjectName(
                            wkCreationData.WorkspaceName);

                        mCloudWorkspacesTreeView.WorkspaceToSelect = createdWorkspace;
                        mCloudWorkspacesTreeView.Reload();
                    });
            }

            Rect treeRect = new Rect(
                treeAreaRect.x,
                treeAreaRect.y + EditorStyles.miniButton.fixedHeight + 2 * NEW_DRIVE_BUTTON_MARGIN,
                treeAreaRect.width,
                treeAreaRect.height - EditorStyles.miniButton.fixedHeight - 2 * NEW_DRIVE_BUTTON_MARGIN);

            cloudWorkspacesTreeView.OnGUI(treeRect);
        }

        void DoToolbar(
            Rect rect,
            float combosAreaWidth,
            List<string> organizations,
            List<string> projects,
            IRefreshableView view)
        {
            GUILayout.BeginArea(rect);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DoCombosArea(combosAreaWidth, organizations, projects);

            GUILayout.FlexibleSpace();

            DoRefreshButton(view);

            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void DoCombosArea(
            float combosAreaWidth,
            List<string> organizations,
            List<string> projects)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(combosAreaWidth));

            DoOrganizationsCombo(organizations);

            DoProjectsCombo(projects);

            EditorGUILayout.EndHorizontal();
        }

        void DoOrganizationsCombo(List<string> organizations)
        {
            GUIContent content = new GUIContent(
                PlasticLocalization.Name.OrganizationsComboBoxLabel.GetString(
                    mSelectedOrganization));

            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarPopup);

            if (GUI.Button(rect, content, EditorStyles.toolbarPopup))
            {
                GenericMenu menu = new GenericMenu();
                foreach (string option in organizations)
                {
                    menu.AddItem(
                        new GUIContent(UnityMenuItem.EscapedText(option)),
                        false,
                        OnOrganizationSelected,
                        option);
                }

                menu.DropDown(rect);
            }
        }

        void DoProjectsCombo(List<string> projects)
        {
            if (projects.Count == 0)
                return;

            GUIContent content = new GUIContent(
                PlasticLocalization.Name.ProjectsComboBoxLabel.GetString(mSelectedProject));

            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarPopup);

            if (GUI.Button(rect, content, EditorStyles.toolbarPopup))
            {
                GenericMenu menu = new GenericMenu();
                foreach (string option in projects)
                {
                    menu.AddItem(
                        new GUIContent(UnityMenuItem.EscapedText(option)),
                        false,
                        OnProjectSelected,
                        option);
                }

                menu.DropDown(rect);
            }
        }

        static void DoRefreshButton(IRefreshableView view)
        {
            if (DrawToolbarButton(
                    Images.GetRefreshIcon(),
                    PlasticLocalization.Name.RefreshButton.GetString()))
                view.Refresh();
        }

        static bool DrawToolbarButton(Texture icon, string tooltip)
        {
            return GUILayout.Button(
                new GUIContent(icon, tooltip),
                EditorStyles.toolbarButton,
                GUILayout.Width(26));
        }

        static Rect GetToolbarRect(Rect rect)
        {
            return new Rect(
                rect.x,
                rect.y,
                rect.width,
                EditorStyles.toolbar.fixedHeight);
        }

        static Rect GetSplitterRect(Rect directoryContentRect)
        {
            return new Rect(
                directoryContentRect.x,
                directoryContentRect.y,
                SPLITTER_WIDTH,
                directoryContentRect.height);
        }

        static Rect GetTreeRect(float width, float height)
        {
            Rect result = GUILayoutUtility.GetRect(
                width, height,
                GUILayout.ExpandWidth(true));

            result.y += EditorStyles.toolbar.fixedHeight;

            return result;
        }

        static Rect GetDirectoryContentPanelRect(float width, float height)
        {
            Rect result = GUILayoutUtility.GetRect(
                width, height,
                GUILayout.ExpandWidth(true));

            result.y += EditorStyles.toolbar.fixedHeight;

            return result;
        }

        static string GetDefaultValue(string proposedValue, List<string> values)
        {
            if (values.Count == 0)
                return null;

            if (!string.IsNullOrEmpty(proposedValue) && values.Contains(proposedValue))
                return proposedValue;

            return values[0];
        }

        void BuildComponents(WorkspaceInfo workspaceToSelect, EditorWindow parentWindow)
        {
            mCloudWorkspacesTreeView = new CloudWorkspacesTreeView(
                OnTreeViewSelectionChanged, mProgressControls, parentWindow);
            mCloudWorkspacesTreeView.WorkspaceToSelect = workspaceToSelect;
            mCloudWorkspacesTreeView.Reload();

            mDirectoryContentPanel = new DirectoryContentPanel(
                mCloudWorkspacesTreeView, this, this, parentWindow);
        }

        void InitializeProposedOrganizationProject(
            string proposedCloudServer, string proposedProject)
        {
            if (!string.IsNullOrEmpty(proposedCloudServer) &&
                !string.IsNullOrEmpty(proposedProject))
            {
                mProposedOrganization = proposedCloudServer;
                mProposedProject = proposedProject;
                return;
            }

            string lastUsedServer = PlasticGuiConfig.Get().Configuration.LastUsedCloudDriveServer;
            string lastUsedProject = PlasticGuiConfig.Get().Configuration.LastUsedCloudDriveProject;

            if (!string.IsNullOrEmpty(lastUsedServer) && !string.IsNullOrEmpty(lastUsedProject))
            {
                mProposedOrganization = lastUsedServer;
                mProposedProject = lastUsedProject;
                return;
            }

            GetProposedOrganizationProject.Values proposedOrganizationProject =
                GetProposedOrganizationProject.FromCloudProjectSettings();

            mProposedOrganization = proposedOrganizationProject != null ?
                proposedOrganizationProject.Organization :
                GetDefaultServer.FromConfig(mRestApi);

            mProposedProject = proposedOrganizationProject != null ?
                proposedOrganizationProject.Project :
                Application.productName;
        }

        int mCopyPathsForProjectBrowserDropCount = 0;
        string mDragTargetPath;
        CloudWorkspacesTreeView mCloudWorkspacesTreeView;
        DirectoryContentPanel mDirectoryContentPanel;

        List<string> mOrganizations = new List<string>();
        List<string> mProjects = new List<string>();
        string mProposedOrganization;
        string mProposedProject;
        string mSelectedOrganization;
        string mSelectedProject;

        readonly SplitterState mSplitterState;

        readonly FillCloudWorkspaces mFillCloudWorkspacesView;
        readonly IPlasticWebRestApi mRestApi;
        readonly IPlasticAPI mPlasticApi;
        readonly IProgressControls mProgressControls;
        readonly EditorWindow mParentWindow;

        const float SPLITTER_WIDTH = 1;
        const float NEW_DRIVE_BUTTON_MARGIN = 5;
        const int TREE_VIEW_MIN_SIZE = 139;
        const int DIRECTORY_CONTENT_MIN_SIZE =
            2 * (int)DrawItemsGridView.GRID_AREA_MARGIN +
            (int)DrawExpandedTreeNode.ICON_SIZE;
    }
}
