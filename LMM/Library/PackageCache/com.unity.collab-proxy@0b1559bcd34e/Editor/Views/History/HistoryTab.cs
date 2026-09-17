using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Utils;
using GluonGui;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Diff;
using PlasticGui.WorkspaceWindow.History;
using PlasticGui.WorkspaceWindow.Open;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
using GluonRevertOperation = GluonGui.WorkspaceWindow.Views.Details.History.RevertOperation;
using HistoryDescriptor = GluonGui.WorkspaceWindow.Views.Details.History.HistoryDescriptor;
using OpenRevisionOperation = PlasticGui.WorkspaceWindow.History.OpenRevisionOperation;

using DiffWindow = Plugins.PlasticSCM.Editor.Diff.DiffWindow;

namespace Unity.PlasticSCM.Editor.Views.History
{
    internal class HistoryTab :
        IRefreshableView,
        HistoryViewLogic.IHistoryView,
        HistoryListViewMenu.IMenuOperations,
        IOpenMenuOperations,
        IHistoryViewMenuOperations
    {
        internal HistoryListView Table { get { return mHistoryListView; } }
        internal string EmptyStateMessage { get { return mEmptyStatePanel.Text; } }

        internal HistoryTab(
            WorkspaceInfo wkInfo,
            ViewHost viewHost,
            IWorkspaceWindow workspaceWindow,
            IAssetStatusCache assetStatusCache,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            EditorWindow parentWindow,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mViewHost = viewHost;
            mWorkspaceWindow = workspaceWindow;
            mAssetStatusCache = assetStatusCache;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
            mPendingChangesUpdater = pendingChangesUpdater;
            mDeveloperIncomingChangesUpdater = developerIncomingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mParentWindow = parentWindow;
            mIsGluonMode = isGluonMode;

            BuildComponents(wkInfo);

            mProgressControls = new ProgressControlsForViews();

            mHistoryViewLogic = new HistoryViewLogic(
                wkInfo, this, mProgressControls);
            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);
        }

        internal void RefreshForItem(
            RepositorySpec repSpec,
            long itemId,
            string path,
            bool isDirectory)
        {
            mRepSpec = repSpec;
            mItemId = itemId;
            mPath = path;
            mIsDirectory = isDirectory;

            ((IRefreshableView)this).Refresh();
        }

        internal void OnEnable()
        {
            mSearchField.downOrUpArrowKeyPressed +=
                SearchField_OnDownOrUpArrowKeyPressed;
        }

        internal void OnDisable()
        {
            mSearchField.downOrUpArrowKeyPressed -=
                SearchField_OnDownOrUpArrowKeyPressed;

            TreeHeaderSettings.Save(
                mHistoryListView.multiColumnHeader.state,
                UnityConstants.HISTORY_TABLE_SETTINGS_NAME);
        }

        internal SerializableHistoryTabState GetSerializableState()
        {
            return new SerializableHistoryTabState(mRepSpec, mItemId, mPath, mIsDirectory);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI(Action closeViewAction)
        {
            DoActionsToolbar(
                mProgressControls,
                mSearchField,
                mHistoryListView,
                GetViewTitle(mPath),
                this,
                closeViewAction);

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            DoHistoryArea(
                mHistoryListView,
                mEmptyStatePanel,
                mProgressControls.IsOperationRunning());

            if (mProgressControls.IsOperationRunning())
            {
                OverlayProgress.DoOverlayProgress(
                    viewRect,
                    mProgressControls.ProgressData.ProgressPercent,
                    mProgressControls.ProgressData.ProgressMessage);
            }
        }

        internal void SetLaunchToolForTesting(
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor)
        {
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
        }

        void IRefreshableView.Refresh()
        {
            mHistoryViewLogic.RefreshForItem(mRepSpec, mItemId, new CancelToken());
        }

        List<RepObjectInfo> HistoryViewLogic.IHistoryView.GetSelectedRevisions()
        {
            return HistorySelection.GetSelectedRepObjectInfos(mHistoryListView);
        }

        void HistoryViewLogic.IHistoryView.SelectRevisions(
            List<RepObjectInfo> revisionsToSelect)
        {
            HistorySelection.SelectRevisions(
                mHistoryListView, revisionsToSelect);
        }

        void HistoryViewLogic.IHistoryView.UpdateData(
            List<ResolvedUser> resolvedUsers,
            ResolvedUser currentUser,
            Dictionary<BranchInfo, ChangesetInfo> branchesAndChangesets,
            BranchInfo workingBranch,
            HistoryRevisionList list,
            long loadedRevisionId,
            WorkspaceUIConfiguration config)
        {
            mHistoryListView.BuildModel(mRepSpec, list, loadedRevisionId);

            mHistoryListView.Refilter();

            mHistoryListView.Sort();

            mHistoryListView.Reload();
        }

        long HistoryListViewMenu.IMenuOperations.GetSelectedChangesetId()
        {
            return HistorySelection.GetSelectedChangesetId(mHistoryListView);
        }

        SelectedHistoryGroupInfo IHistoryViewMenuOperations.GetSelectedHistoryGroupInfo()
        {
            return SelectedHistoryGroupInfo.BuildFromSelection(
                HistorySelection.GetSelectedRepObjectInfos(mHistoryListView),
                HistorySelection.GetSelectedHistoryRevisions(mHistoryListView),
                mHistoryListView.GetLoadedRevisionId(),
                mIsDirectory);
        }

        void IOpenMenuOperations.Open()
        {
            OpenRevisionOperation.Open(
                mRepSpec,
                Path.GetFileName(mPath),
                HistorySelection.GetSelectedHistoryRevisions(
                    mHistoryListView));
        }

        void IOpenMenuOperations.OpenWith()
        {
            List<HistoryRevision> revisions = HistorySelection.
                GetSelectedHistoryRevisions(mHistoryListView);

            OpenRevisionOperation.OpenWith(
                mRepSpec,
                FileSystemOperation.GetExePath(),
                Path.GetFileName(mPath),
                revisions);
        }

        void IOpenMenuOperations.OpenWithCustom(string exePath, string args)
        {
        }

        void IOpenMenuOperations.OpenInExplorer()
        {
        }

        void IHistoryViewMenuOperations.SaveRevisionAs()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.UnityPackage.SaveRevisionFromFileHistory);

            HistoryRevision revision = HistorySelection.
                GetSelectedHistoryRevision(mHistoryListView);

            string defaultFileName = DefaultRevisionName.Get(
                Path.GetFileName(mPath), revision.ChangeSet);

            string destinationPath = SaveAction.GetDestinationPath(
                mWkInfo.ClientPath, mPath, defaultFileName);

            if (string.IsNullOrEmpty(destinationPath))
                return;

            SaveRevisionOperation.SaveRevision(
                mRepSpec,
                destinationPath,
                revision,
                mProgressControls);
        }

        void IHistoryViewMenuOperations.DiffWithPrevious()
        {
            HistoryRevision revision = HistorySelection.
                GetSelectedHistoryRevision(mHistoryListView);

            if (UseBuiltinDiffWindowPreference.IsEnabled())
            {
                DiffWindow diffWindow = ShowWindow.Diff();
                diffWindow.ShowDiffFromHistory(
                    null,
                    revision,
                    mRepSpec,
                    mPath,
                    mItemId);
                return;
            }

            DiffOperation.DiffWithPrevious(
                mWkInfo,
                mRepSpec,
                Path.GetFileName(mPath),
                string.Empty,
                revision.Id,
                mItemId,
                revision.ChangeSet,
                mProgressControls,
                PlasticExeLauncher.BuildForDiffRevision(mRepSpec, mIsGluonMode, mShowDownloadPlasticExeWindow),
                null);
        }

        void IHistoryViewMenuOperations.DiffSelectedRevisions()
        {
            List<HistoryRevision> revisions = HistorySelection.
                GetSelectedHistoryRevisions(mHistoryListView);

            bool areReversed = revisions[0].Id > revisions[1].Id;

            HistoryRevision leftRevision = revisions[(areReversed) ? 1 : 0];
            HistoryRevision rightRevision = revisions[(areReversed) ? 0 : 1];

            if (UseBuiltinDiffWindowPreference.IsEnabled())
            {
                DiffWindow diffWindow = ShowWindow.Diff();
                diffWindow.ShowDiffFromHistory(
                    leftRevision,
                    rightRevision,
                    mRepSpec,
                    mPath,
                    mItemId);
                return;
            }

            DiffOperation.DiffRevisions(
                mWkInfo,
                mRepSpec,
                Path.GetFileName(mPath),
                string.Empty,
                mItemId,
                leftRevision,
                rightRevision,
                mProgressControls,
                PlasticExeLauncher.BuildForDiffSelectedRevisions(mRepSpec, mIsGluonMode, mShowDownloadPlasticExeWindow),
                null);
        }

        void IHistoryViewMenuOperations.DiffChangeset()
        {
            long changeset = HistorySelection.GetSelectedChangesetId(mHistoryListView);

            LaunchDiffOperations.DiffChangeset(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                mRepSpec,
                changeset,
                mIsGluonMode);
        }

        void IHistoryViewMenuOperations.RevertToThisRevision()
        {
            HistoryRevision revision = HistorySelection.
                GetSelectedHistoryRevision(mHistoryListView);

            string fullPath = GetFullPath(mWkInfo.ClientPath, mPath);

            if (mIsGluonMode)
            {
                HistoryDescriptor historyDescriptor = new HistoryDescriptor(
                    mRepSpec, fullPath, mItemId, revision.Id, mIsDirectory);

                GluonRevertOperation.RevertToThisRevision(
                    mWkInfo,
                    mViewHost,
                    mProgressControls,
                    historyDescriptor,
                    revision,
                    mPendingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    () => RefreshAsset.UnityAssetDatabase(mAssetStatusCache));
                return;
            }

            RevertOperation.RevertToThisRevision(
                mWkInfo,
                mProgressControls,
                mWorkspaceWindow,
                mRepSpec,
                revision,
                fullPath,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                null,
                () => RefreshAsset.UnityAssetDatabase(mAssetStatusCache));
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mHistoryListView.SetFocusAndEnsureSelectedItem();
        }

        static string GetFullPath(string wkPath, string path)
        {
            if (PathHelper.IsContainedOn(path, wkPath))
                return path;

            return WorkspacePath.GetWorkspacePathFromCmPath(
                wkPath, path, Path.DirectorySeparatorChar);
        }

        static void DoActionsToolbar(
            ProgressControlsForViews progressControls,
            SearchField searchField,
            HistoryListView historyListView,
            string viewTitle,
            IRefreshableView view,
            Action closeViewAction)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(
                    new GUIContent(Images.GetRefreshIcon(),
                        PlasticLocalization.Name.RefreshButton.GetString()),
                    UnityStyles.ToolbarButtonLeft,
                    GUILayout.Width(UnityConstants.TOOLBAR_ICON_BUTTON_WIDTH)))
            {
                view.Refresh();
            }

            GUILayout.Label(
                viewTitle,
                UnityStyles.HistoryTab.HeaderLabel);

           GUILayout.FlexibleSpace();

            DrawSearchField.For(
                searchField,
                historyListView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            if (GUILayout.Button(
                Images.GetCloseIcon(),
                UnityStyles.CloseViewIconButtonStyle))
            {
                closeViewAction();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DoHistoryArea(
            HistoryListView historyListView,
            EmptyStatePanel emptyStatePanel,
            bool isOperationRunning)
        {
            GUI.enabled = !isOperationRunning;

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            historyListView.OnGUI(rect);

            if (Event.current.type == EventType.Layout)
                mShouldShowEmptyState = !emptyStatePanel.IsEmpty();

            if (mShouldShowEmptyState)
                emptyStatePanel.OnGUI(rect);

            GUI.enabled = true;
        }

        void OnDelayedSelectionChanged()
        {
            if (!UseBuiltinDiffWindowPreference.IsEnabled())
                return;

            if (mHistoryListView.GetSelection().Count != 1)
                return;

            HistoryRevision selectedRevision = HistorySelection.GetSelectedHistoryRevision(mHistoryListView);

            if (selectedRevision == null)
                return;

            DiffWindow diffWindow = GetWindowIfOpened.Diff();

            diffWindow?.ShowDiffFromHistory(
                null,
                selectedRevision,
                mRepSpec,
                mPath,
                mItemId);
        }

        void OnRowDoubleClickAction()
        {
            if (mHistoryListView.GetSelection().Count != 1)
                return;

            HistoryRevision selectedRevision = HistorySelection.GetSelectedHistoryRevision(mHistoryListView);

            if (selectedRevision == null)
                return;

            if (UseBuiltinDiffWindowPreference.IsEnabled())
            {
                DiffWindow diffWindow = ShowWindow.Diff();
                diffWindow.ShowDiffFromHistory(
                    null,
                    selectedRevision,
                    mRepSpec,
                    mPath,
                    mItemId);
                return;
            }

            ((IOpenMenuOperations)this).Open();
        }

        void OnItemsChangedAction(IEnumerable<RepObjectInfo> items)
        {
            if (items.Count() > 0)
            {
                mEmptyStatePanel.UpdateContent(string.Empty);
                return;
            }

            mEmptyStatePanel.UpdateContent(
                PlasticLocalization.Name.NoRevisionsMatchingFilters.GetString());
        }

        static string GetViewTitle(string path)
        {
            path = PathHelper.RemoveLastSlash(
                path, Path.DirectorySeparatorChar);

            return PlasticLocalization.GetString(
                PlasticLocalization.Name.HistoryViewTitle,
                Path.GetFileName(path));
        }

        void BuildComponents(WorkspaceInfo wkInfo)
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            HistoryListHeaderState headerState =
                HistoryListHeaderState.GetDefault();
            TreeHeaderSettings.Load(headerState,
                UnityConstants.HISTORY_TABLE_SETTINGS_NAME,
                (int)HistoryListColumn.CreationDate,
                false);

            mHistoryListView = new HistoryListView(
                wkInfo.ClientPath,
                headerState,
                new HistoryListViewMenu(this, this, this),
                HistoryListHeaderState.GetColumnNames(),
                OnDelayedSelectionChanged,
                OnRowDoubleClickAction,
                afterItemsChangedAction: OnItemsChangedAction);

            mHistoryListView.Reload();
        }

        bool mShouldShowEmptyState;
        bool mIsDirectory;
        long mItemId;
        string mPath;
        RepositorySpec mRepSpec;

        SearchField mSearchField;
        HistoryListView mHistoryListView;

        LaunchTool.IProcessExecutor mProcessExecutor;
        LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;

        readonly EmptyStatePanel mEmptyStatePanel;
        readonly HistoryViewLogic mHistoryViewLogic;
        readonly ProgressControlsForViews mProgressControls;
        readonly bool mIsGluonMode;
        readonly EditorWindow mParentWindow;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        readonly GluonIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly ViewHost mViewHost;
        readonly WorkspaceInfo mWkInfo;
    }
}
