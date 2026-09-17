using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Commands;
using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.CM.Common.Mount;
using PlasticGui;
using PlasticGui.Diff;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BrowseRepository;
using PlasticGui.WorkspaceWindow.Diff;
using PlasticGui.WorkspaceWindow.Diff.Type;
using Plugins.PlasticSCM.Editor.Diff;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Diff.Dialogs;
using Unity.PlasticSCM.Editor.Views.History;

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal class DiffPanel :
        IDiffTreeViewMenuOperations,
        DiffTreeViewMenu.IMetaMenuOperations,
        UndeleteClientDiffsOperation.IGetRestorePathDialog,
        IShowDiffs
    {
        internal DiffTreeView Table { get { return mDiffTreeView; } }
        internal string EmptyStateMessage { get { return mEmptyStatePanel.Text; } }

        internal DiffPanel(
            Action repaint,
            WorkspaceInfo wkInfo,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IHistoryViewLauncher historyViewLauncher,
            IRefreshView refreshView,
            IAssetStatusCache assetStatusCache,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            IPendingChangesUpdater pendingChangesUpdater,
            IIncomingChangesUpdater developerIncomingChangesUpdater,
            IIncomingChangesUpdater gluonIncomingChangesUpdater,
            EditorWindow parentWindow,
            bool isGluonMode)
        {
            mRepaint = repaint;
            mWkInfo = wkInfo;
            mWorkspaceWindow = workspaceWindow;
            mViewSwitcher = viewSwitcher;
            mHistoryViewLauncher = historyViewLauncher;
            mRefreshView = refreshView;
            mAssetStatusCache = assetStatusCache;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mPendingChangesUpdater = pendingChangesUpdater;
            mDeveloperIncomingChangesUpdater = developerIncomingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mParentWindow = parentWindow;
            mIsGluonMode = isGluonMode;

            mGuiMessage = new UnityPlasticGuiMessage();

            mEmptyStatePanel = new EmptyStatePanel(repaint);

            BuildComponents();

            mProgressControls = new ProgressControlsForViews();
        }

        internal void ClearInfo()
        {
            ClearData();

            mRepaint();
        }

        internal void UpdateInfo(
            MountPointWithPath mountWithPath,
            RepObjectInfo repObjectInfo)
        {
            if (mSelectedRepObjectInfo != null &&
                mSelectedRepObjectInfo.Equals(repObjectInfo))
                return;

            mSelectedMountWithPath = mountWithPath;
            mSelectedRepObjectInfo = repObjectInfo;

            FillData(mountWithPath, repObjectInfo);

            mRepaint();
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
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mRepaint);
        }

        internal void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DoActionsToolbar(
                mDiffs,
                mDiffsBranchResolver,
                mProgressControls,
                mIsSkipMergeTrackingButtonVisible,
                mIsSkipMergeTrackingButtonChecked,
                mSearchField,
                mDiffTreeView);

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            if (mIsDiffDeferred)
            {
                DoDeferredDiffsPanel();
            }
            else
            {
                DoDiffTreeViewArea(
                    mDiffTreeView,
                    mEmptyStatePanel,
                    mProgressControls.IsOperationRunning());
            }

            if (mProgressControls.HasNotification())
            {
                DrawProgressForViews.ForNotificationArea(
                    mProgressControls.ProgressData);
            }

            EditorGUILayout.EndVertical();

            if (mProgressControls.IsOperationRunning())
            {
                OverlayProgress.DoOverlayProgress(
                    viewRect,
                    mProgressControls.ProgressData.ProgressPercent,
                    mProgressControls.ProgressData.ProgressMessage);
            }
        }

        void DoDeferredDiffsPanel()
        {
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIStyle wrappedLabelStyle = new GUIStyle(
                UnityStyles.EmptyState.Label);
            wrappedLabelStyle.wordWrap = true;

            GUILayout.Label(
                mIsDiffDeferredExplanation,
                wrappedLabelStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                PlasticLocalization.Name.CalculateDiffsButton.GetString(),
                GUILayout.Width(150)))
            {
                CalculateDiffsDeferred();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
        }

        void IDiffTreeViewMenuOperations.SaveRevisionAs()
        {
            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                TrackFeatureUseEvent.Features.UnityPackage.SaveRevisionFromDiff);

            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);
            RepositorySpec repSpec = clientDiffInfo.DiffWithMount.Mount.RepSpec;
            RevisionInfo revision = clientDiffInfo.DiffWithMount.Difference.RevInfo;

            string defaultFileName = DefaultRevisionName.Get(
                Path.GetFileName(clientDiffInfo.DiffWithMount.Difference.Path), revision.Changeset);
            string destinationPath = SaveAction.GetDestinationPath(
                mWkInfo.ClientPath,
                clientDiffInfo.DiffWithMount.Difference.Path,
                defaultFileName);

            if (string.IsNullOrEmpty(destinationPath))
                return;

            SaveRevisionOperation.SaveRevision(
                repSpec,
                destinationPath,
                revision,
                mProgressControls);
        }

        SelectedDiffsGroupInfo IDiffTreeViewMenuOperations.GetSelectedDiffsGroupInfo()
        {
            return SelectedDiffsGroupInfo.BuildFromSelectedNodes(
                DiffSelection.GetSelectedDiffsWithoutMeta(mDiffTreeView),
                mWkInfo != null);
        }

        void IDiffTreeViewMenuOperations.Diff()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            if (UseBuiltinDiffWindowPreference.IsEnabled())
            {
                DiffWindow diffWindow = ShowWindow.Diff();
                diffWindow.ShowDiffFromDiff(
                    clientDiffInfo.DiffWithMount.Mount.Mount,
                    clientDiffInfo.DiffWithMount.Difference);
                return;
            }

            DiffOperation.DiffClientDiff(
                mWkInfo,
                clientDiffInfo.DiffWithMount.Mount.Mount,
                clientDiffInfo.DiffWithMount.Difference,
                PlasticExeLauncher.BuildForDiffRevision(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                imageDiffLauncher: null);
        }

        void IDiffTreeViewMenuOperations.History()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            mHistoryViewLauncher.ShowHistoryView(
                clientDiffInfo.DiffWithMount.Mount.RepSpec,
                clientDiffInfo.DiffWithMount.Difference.RevInfo.ItemId,
                clientDiffInfo.DiffWithMount.Difference.Path,
                clientDiffInfo.DiffWithMount.Difference.IsDirectory);
        }

        void IDiffTreeViewMenuOperations.RevertChanges()
        {
            RevertClientDiffsOperation.RevertChanges(
                mWkInfo,
                DiffSelection.GetSelectedDiffs(mDiffTreeView),
                mWorkspaceWindow,
                mProgressControls,
                mGuiMessage,
                mPendingChangesUpdater,
                mIsGluonMode ?
                    mGluonIncomingChangesUpdater :
                    mDeveloperIncomingChangesUpdater,
                AfterRevertOrUndeleteOperation);
        }

        void IDiffTreeViewMenuOperations.Undelete()
        {
            UndeleteClientDiffsOperation.Undelete(
                mWkInfo,
                DiffSelection.GetSelectedDiffs(mDiffTreeView),
                mRefreshView,
                mProgressControls,
                this,
                mGuiMessage,
                mPendingChangesUpdater,
                mIsGluonMode ?
                    mGluonIncomingChangesUpdater :
                    mDeveloperIncomingChangesUpdater,
                AfterRevertOrUndeleteOperation);
        }

        void IDiffTreeViewMenuOperations.UndeleteToSpecifiedPaths()
        {
            UndeleteClientDiffsOperation.UndeleteToSpecifiedPaths(
                mWkInfo,
                DiffSelection.GetSelectedDiffs(mDiffTreeView),
                mRefreshView,
                mProgressControls,
                this,
                mGuiMessage,
                mPendingChangesUpdater,
                mIsGluonMode ?
                    mGluonIncomingChangesUpdater :
                    mDeveloperIncomingChangesUpdater,
                AfterRevertOrUndeleteOperation);
        }

        void IDiffTreeViewMenuOperations.Annotate()
        {
        }

        void IDiffTreeViewMenuOperations.CopyFilePath(bool relativePath)
        {
            EditorGUIUtility.systemCopyBuffer = GetFilePathList.FromClientDiffInfos(
                DiffSelection.GetSelectedDiffsWithoutMeta(mDiffTreeView),
                relativePath,
                mWkInfo.ClientPath);
        }

        bool DiffTreeViewMenu.IMetaMenuOperations.SelectionHasMeta()
        {
            return mDiffTreeView.SelectionHasMeta();
        }

        void DiffTreeViewMenu.IMetaMenuOperations.DiffMeta()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            ClientDiffInfo clientDiffInfoMeta =
                mDiffTreeView.GetMetaDiff(clientDiffInfo);

            if (UseBuiltinDiffWindowPreference.IsEnabled())
            {
                DiffWindow diffWindow = ShowWindow.Diff();
                diffWindow.ShowDiffFromDiff(
                    clientDiffInfoMeta.DiffWithMount.Mount.Mount,
                    clientDiffInfoMeta.DiffWithMount.Difference);
                return;
            }

            DiffOperation.DiffClientDiff(
                mWkInfo,
                clientDiffInfoMeta.DiffWithMount.Mount.Mount,
                clientDiffInfoMeta.DiffWithMount.Difference,
                PlasticExeLauncher.BuildForDiffRevision(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                imageDiffLauncher: null);
        }

        GetRestorePathData
            UndeleteClientDiffsOperation.IGetRestorePathDialog.GetRestorePath(
                string wkPath, string restorePath, string explanation,
                bool isDirectory, bool showSkipButton)
        {
            return GetRestorePathDialog.GetRestorePath(
                wkPath, restorePath, explanation, isDirectory,
                showSkipButton, mParentWindow);
        }

        void DiffTreeViewMenu.IMetaMenuOperations.HistoryMeta()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            ClientDiffInfo clientDiffInfoMeta =
                mDiffTreeView.GetMetaDiff(clientDiffInfo);

            mHistoryViewLauncher.ShowHistoryView(
                clientDiffInfoMeta.DiffWithMount.Mount.RepSpec,
                clientDiffInfoMeta.DiffWithMount.Difference.RevInfo.ItemId,
                clientDiffInfoMeta.DiffWithMount.Difference.Path,
                clientDiffInfoMeta.DiffWithMount.Difference.IsDirectory);
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mDiffTreeView.SetFocusAndEnsureSelectedItem();
        }

        void AfterRevertOrUndeleteOperation()
        {
            RefreshAsset.UnityAssetDatabase(mAssetStatusCache);

            mViewSwitcher.ShowPendingChanges();
        }

        void ClearData()
        {
            mSelectedMountWithPath = null;
            mSelectedRepObjectInfo = null;

            mDiffs = null;
            mIsDiffDeferred = false;

            mDiffTreeView.searchString = null;

            ClearDiffs();
        }

        void IShowDiffs.ShowWaitingAnimation()
        {
            ((IProgressControls)mProgressControls).ShowProgress(
                PlasticLocalization.Name.Loading.GetString());
        }

        void IShowDiffs.HideWaitingAnimation()
        {
            ((IProgressControls)mProgressControls).HideProgress();
        }

        void IShowDiffs.For(
            ShowDiffsData showDiffsData,
            DiffViewEntryDescriptor selectedItem,
            bool selectItem,
            Action afterUpdateDiffs)
        {
            mDiffs = showDiffsData.Diffs;
            mDiffsBranchResolver = showDiffsData.BranchResolver;

            ShowDiffsData();
        }

        void FillData(
            MountPointWithPath mountWithPath,
            RepObjectInfo repObjectInfo)
        {
            if (repObjectInfo.Id == -1)
            {
                ClearDiffs();
                UpdateEmptyState();
                return;
            }

            if (ShouldDeferDiffCalculation(repObjectInfo, out mIsDiffDeferredExplanation))
            {
                mIsDiffDeferred = true;
                ClearDiffs();
                UpdateEmptyState();
                return;
            }

            mIsDiffDeferred = false;
            CalculateDiffs(mountWithPath, repObjectInfo);
        }

        internal void CalculateDiffsDeferred()
        {
            if (!mIsDiffDeferred)
                return;

            mIsDiffDeferred = false;
            CalculateDiffs(mSelectedMountWithPath, mSelectedRepObjectInfo);
        }

        void CalculateDiffs(
            MountPointWithPath mountWithPath,
            RepObjectInfo repObjectInfo)
        {
            ((IProgressControls)mProgressControls).ShowProgress(
                PlasticLocalization.GetString(PlasticLocalization.Name.Loading));

            mIsSkipMergeTrackingButtonVisible = false;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(100);
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    if (repObjectInfo is BranchInfo)
                    {
                        mDiffs = PlasticGui.Plastic.API.GetBranchDifferencesForMountPoint(
                            mountWithPath,
                            (BranchInfo)repObjectInfo);
                    }

                    if (repObjectInfo is ChangesetInfo)
                    {
                        mDiffs = PlasticGui.Plastic.API.GetChangesetDifferences(
                            mountWithPath,
                            (ChangesetInfo)repObjectInfo);
                    }

                    mDiffsBranchResolver = BuildBranchResolver.ForDiffs(mDiffs);
                },
                afterOperationDelegate: delegate
                {
                    ((IProgressControls)mProgressControls).HideProgress();

                    if (mSelectedMountWithPath != mountWithPath ||
                        mSelectedRepObjectInfo != repObjectInfo)
                        return;

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.LogException("DiffPanel", waiter.Exception);

                        ((IProgressControls)mProgressControls).ShowError(waiter.Exception.Message);

                        ClearDiffs();
                        return;
                    }

                    ShowDiffsData();
                });
        }

        void ShowDiffsData()
        {
            mDiffTreeView.searchString = null;

            if (mDiffs == null || mDiffs.Count == 0)
            {
                ClearDiffs();
                UpdateEmptyState();
                return;
            }

            mIsSkipMergeTrackingButtonVisible =
                ClientDiffList.HasMerges(mDiffs);

            bool skipMergeTracking =
                mIsSkipMergeTrackingButtonVisible &&
                mIsSkipMergeTrackingButtonChecked;

            UpdateDiffTreeView(
                mWkInfo,
                mDiffs,
                mDiffsBranchResolver,
                skipMergeTracking,
                mDiffTreeView);

            UpdateEmptyState();
        }

        void ClearDiffs()
        {
            mIsSkipMergeTrackingButtonVisible = false;

            ClearDiffTreeView(mDiffTreeView);
        }

        static void ClearDiffTreeView(
            DiffTreeView diffTreeView)
        {
            diffTreeView.ClearModel();

            diffTreeView.Reload();
        }

        static void UpdateDiffTreeView(
            WorkspaceInfo wkInfo,
            List<ClientDiff> diffs,
            BranchResolver brResolver,
            bool skipMergeTracking,
            DiffTreeView diffTreeView)
        {
            diffTreeView.BuildModel(
                wkInfo, diffs, brResolver, skipMergeTracking);

            diffTreeView.Refilter();

            diffTreeView.Sort();

            diffTreeView.Reload();
        }

        void DoActionsToolbar(
            List<ClientDiff> diffs,
            BranchResolver brResolver,
            ProgressControlsForViews progressControls,
            bool isSkipMergeTrackingButtonVisible,
            bool isSkipMergeTrackingButtonChecked,
            SearchField searchField,
            DiffTreeView diffTreeView)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();

            if (isSkipMergeTrackingButtonVisible)
            {
                DoSkipMergeTrackingButton(
                    diffs, brResolver,
                    isSkipMergeTrackingButtonChecked,
                    diffTreeView);
            }

            DrawSearchField.For(
                searchField,
                diffTreeView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            EditorGUILayout.EndHorizontal();
        }

        void DoSkipMergeTrackingButton(
            List<ClientDiff> diffs,
            BranchResolver brResolver,
            bool isSkipMergeTrackingButtonChecked,
            DiffTreeView diffTreeView)
        {
            bool wasChecked = isSkipMergeTrackingButtonChecked;

            GUIContent buttonContent = new GUIContent(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.SkipDiffMergeTracking));

            GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

            float buttonWidth = buttonStyle.CalcSize(buttonContent).x + 10;

            Rect toggleRect = GUILayoutUtility.GetRect(
                buttonContent, buttonStyle, GUILayout.Width(buttonWidth));

            bool isChecked = GUI.Toggle(
                toggleRect, wasChecked, buttonContent, buttonStyle);

            if (wasChecked == isChecked)
                return;

            // if user just checked the skip merge tracking button
            if (isChecked)
            {
                TrackFeatureUseEvent.For(
                    PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                    TrackFeatureUseEvent.Features.UnityPackage.ChangesetViewSkipMergeTrackingButton);
            }

            UpdateDiffTreeView(mWkInfo, diffs, brResolver, isChecked, diffTreeView);

            mIsSkipMergeTrackingButtonChecked = isChecked;
        }

        void OnDelayedSelectionChanged()
        {
            if (!UseBuiltinDiffWindowPreference.IsEnabled())
                return;

            if (mDiffTreeView.GetSelection().Count != 1)
                return;

            if (!DiffSelection.IsApplicableDiffClientDiff(mDiffTreeView))
                return;

            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            DiffWindow diffWindow = GetWindowIfOpened.Diff();

            diffWindow?.ShowDiffFromDiff(
                clientDiffInfo.DiffWithMount.Mount.Mount,
                clientDiffInfo.DiffWithMount.Difference);
        }

        void OnRowDoubleClickAction()
        {
            if (mDiffTreeView.GetSelection().Count != 1)
                return;

            if (DiffSelection.IsApplicableDiffClientDiff(mDiffTreeView))
            {
                if (UseBuiltinDiffWindowPreference.IsEnabled())
                {
                    ClientDiffInfo clientDiffInfo =
                        DiffSelection.GetSelectedDiff(mDiffTreeView);

                    DiffWindow diffWindow = ShowWindow.Diff();
                    diffWindow.ShowDiffFromDiff(
                        clientDiffInfo.DiffWithMount.Mount.Mount,
                        clientDiffInfo.DiffWithMount.Difference);
                    return;
                }

                ((IDiffTreeViewMenuOperations)this).Diff();
                return;
            }

            int selectedNode = mDiffTreeView.GetSelection()[0];

            if (mDiffTreeView.IsExpanded(selectedNode))
            {
                mDiffTreeView.SetExpanded(selectedNode, expanded: false);
                return;
            }

            mDiffTreeView.SetExpanded(selectedNode, expanded: true);
        }

        void UpdateEmptyState()
        {
            mEmptyStatePanel.UpdateContent(GetEmptyStateMessage(mDiffTreeView));
        }

        void DoDiffTreeViewArea(
            DiffTreeView diffTreeView,
            EmptyStatePanel emptyStatePanel,
            bool isOperationRunning)
        {
            GUI.enabled = !isOperationRunning;

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            if (Event.current.type == EventType.Layout)
                UpdateEmptyState();

            if (emptyStatePanel.IsEmpty())
            {
                diffTreeView.OnGUI(rect);
            }
            else
            {
                emptyStatePanel.OnGUI(rect);
            }

            GUI.enabled = true;
        }

        static string GetEmptyStateMessage(DiffTreeView diffTreeView)
        {
            if (diffTreeView.GetRows().Count > 0)
                return string.Empty;

            return string.IsNullOrEmpty(diffTreeView.searchString) ?
                PlasticLocalization.Name.NoContentToCompareExplanation.GetString() :
                PlasticLocalization.Name.NoDiffsMatchingFilters.GetString();
        }

        void BuildComponents()
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            DiffTreeViewMenu diffTreeViewMenu = new DiffTreeViewMenu(this, this);
            mDiffTreeView = new DiffTreeView(
                diffTreeViewMenu,
                OnDelayedSelectionChanged,
                OnRowDoubleClickAction,
                UpdateEmptyState);

            mDiffTreeView.Reload();
        }

        bool ShouldDeferDiffCalculation(RepObjectInfo repObjectInfo, out string message)
        {
            if (repObjectInfo is BranchInfo branchInfo && branchInfo.IsMainBranch())
            {
                message = PlasticLocalization.Name.MainBranchDiffDeferredExplanation.GetString();
                return true;
            }

            message = string.Empty;
            return false;
        }

        bool mIsSkipMergeTrackingButtonVisible;
        bool mIsSkipMergeTrackingButtonChecked;
        bool mIsDiffDeferred;
        string mIsDiffDeferredExplanation;

        RepObjectInfo mSelectedRepObjectInfo;
        MountPointWithPath mSelectedMountWithPath;

        volatile List<ClientDiff> mDiffs;
        volatile BranchResolver mDiffsBranchResolver;

        SearchField mSearchField;
        DiffTreeView mDiffTreeView;

        readonly Action mRepaint;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IIncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        readonly ProgressControlsForViews mProgressControls;
        readonly IIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly EmptyStatePanel mEmptyStatePanel;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly bool mIsGluonMode;
        readonly EditorWindow mParentWindow;
        readonly IRefreshView mRefreshView;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IHistoryViewLauncher mHistoryViewLauncher;
        readonly IViewSwitcher mViewSwitcher;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly WorkspaceInfo mWkInfo;
    }
}
