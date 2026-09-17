using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Common.FsNodeReaders;
using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.Gluon.WorkspaceWindow.Views.IncomingChanges;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Diff;
using PlasticGui.WorkspaceWindow.Merge;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.StatusBar;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Errors;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Merge;
using UnityEditor.IMGUI.Controls;

using CheckIncomingChanges = PlasticGui.Gluon.WorkspaceWindow.CheckIncomingChanges;
using IncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
#if !UNITY_6000_3_OR_NEWER
using SplitterGUILayout = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterGUILayout;
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.Views.IncomingChanges.Gluon
{
    internal class IncomingChangesTab :
        IIncomingChangesTab,
        IRefreshableView,
        IncomingChangesViewLogic.IIncomingChangesView,
        IIncomingChangesViewMenuOperations,
        IncomingChangesViewMenu.IMetaMenuOperations
    {
        internal IncomingChangesTab(
            WorkspaceInfo wkInfo,
            ViewHost viewHost,
            WorkspaceWindow workspaceWindow,
            CheckIncomingChanges.IUpdateIncomingChanges updateIncomingChanges,
            IAssetStatusCache assetStatusCache,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            PendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater incomingChangesUpdater,
            WindowStatusBar windowStatusBar,
            EditorWindow parentWindow)
        {
            mWkInfo = wkInfo;
            mAssetStatusCache = assetStatusCache;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mIncomingChangesUpdater = incomingChangesUpdater;
            mWindowStatusBar = windowStatusBar;
            mParentWindow = parentWindow;

            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);

            BuildComponents();

            mProgressControls = new ProgressControlsForViews();

            mDelayedClearUpdateSuccessAction = new DelayedActionBySecondsRunner(
                DelayedClearUpdateSuccess,
                UnityConstants.NOTIFICATION_CLEAR_INTERVAL);

            mErrorsSplitterState = new SplitterState(
                SplitterSettings.Load(
                    UnityConstants.GLUON_INCOMING_CHANGES_SPLITTER_SETTINGS_NAME,
                    new float[] { 0.75f, 0.25f }),
                new int[] { 100, 100 },
                new int[] { 100000, 100000 }
            );

            mIncomingChangesViewLogic = new IncomingChangesViewLogic(
                wkInfo,
                viewHost,
                this,
                new UnityPlasticGuiMessage(),
                mProgressControls,
                pendingChangesUpdater,
                mIncomingChangesUpdater,
                updateIncomingChanges,
                workspaceWindow.GluonProgressOperationHandler, workspaceWindow,
                new IncomingChangesViewLogic.ApplyGluonWorkspaceLocalChanges(),
                new IncomingChangesViewLogic.OutOfDateItemsOperations(),
                new IncomingChangesViewLogic.GetWorkingBranch(),
                new IncomingChangesViewLogic.ResolveUserName(),
                new ResolveChangeset(),
                NewChangesInWk.Build(wkInfo, new BuildWorkspacekIsRelevantNewChange()),
                null);

            mIncomingChangesViewLogic.Refresh();
        }

        bool IIncomingChangesTab.IsVisible
        {
            get { return mIsVisible; }
            set { mIsVisible = value; }
        }

        void IIncomingChangesTab.OnEnable()
        {
            mSearchField.downOrUpArrowKeyPressed +=
                SearchField_OnDownOrUpArrowKeyPressed;
        }

        void IIncomingChangesTab.OnDisable()
        {
            mSearchField.downOrUpArrowKeyPressed -=
                SearchField_OnDownOrUpArrowKeyPressed;

            TreeHeaderSettings.Save(
                mIncomingChangesTreeView.multiColumnHeader.state,
                UnityConstants.GLUON_INCOMING_CHANGES_TABLE_SETTINGS_NAME);

            SplitterSettings.Save(
                mErrorsSplitterState,
                UnityConstants.GLUON_INCOMING_CHANGES_SPLITTER_SETTINGS_NAME);

            mErrorsPanel.OnDisable();
        }

        void IIncomingChangesTab.Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        void IIncomingChangesTab.OnGUI()
        {
            DoActionsToolbar(
                mSearchField,
                mIncomingChangesTreeView,
                this);

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            if (mErrorsPanel.IsVisible)
                SplitterGUILayout.BeginVerticalSplit(mErrorsSplitterState);

            DoIncomingChangesArea(
                mIncomingChangesTreeView,
                mEmptyStatePanel,
                mDelayedClearUpdateSuccessAction,
                mProgressControls.IsOperationRunning(),
                mIsUpdateSuccessful);

            if (mErrorsPanel.IsVisible)
            {
                mErrorsPanel.OnGUI();
                SplitterGUILayout.EndVerticalSplit();
            }

            DrawActionToolbar.Begin();

            DoActionToolbarMessage(
                mIsMessageLabelVisible,
                mMessageLabelText,
                mIsErrorMessageLabelVisible,
                mErrorMessageLabelText,
                mFileConflictCount,
                mChangesSummary);

            if (mIsProcessMergesButtonVisible)
            {
                DoProcessMergesButton(
                    mIsProcessMergesButtonEnabled,
                    mProcessMergesButtonText,
                    mShowDownloadPlasticExeWindow,
                    mIncomingChangesViewLogic,
                    mIncomingChangesTreeView,
                    mWkInfo,
                    RefreshAsset.BeforeLongAssetOperation,
                    AfterProcessMerges,
                    MergeSuccessfullyFinished);
            }

            if (mIsCancelMergesButtonVisible)
            {
                mIsCancelMergesButtonEnabled = DoCancelMergesButton(
                    mIsCancelMergesButtonEnabled,
                    mIncomingChangesViewLogic);
            }

            DrawActionToolbar.End();

            if (mProgressControls.HasNotification())
            {
                DrawProgressForViews.ForNotificationArea(
                    mProgressControls.ProgressData);
            }

            if (mProgressControls.IsOperationRunning())
            {
                OverlayProgress.DoOverlayProgress(
                    viewRect,
                    mProgressControls.ProgressData.ProgressPercent,
                    mProgressControls.ProgressData.ProgressMessage);
            }

            ExecuteAfterOnGUIAction();
        }

        void IIncomingChangesTab.AutoRefresh()
        {
            mIncomingChangesViewLogic.AutoRefresh(DateTime.Now);
        }

        void IRefreshableView.Refresh()
        {
            if (mIncomingChangesUpdater != null)
                mIncomingChangesUpdater.Update(DateTime.Now);

            mIncomingChangesViewLogic.Refresh();
        }

        void IncomingChangesViewLogic.IIncomingChangesView.UpdateData(
            IncomingChangesTree tree,
            List<ErrorMessage> errorMessages,
            string processMergesButtonText,
            PendingConflictsLabelData conflictsLabelData,
            string changesToApplySummaryText)
        {
            ShowProcessMergesButton(processMergesButtonText);

            ((IncomingChangesViewLogic.IIncomingChangesView)this).
                UpdatePendingConflictsLabel(conflictsLabelData);

            UpdateIncomingChangesTree(mIncomingChangesTreeView, tree);

            mErrorsPanel.UpdateErrorsList(errorMessages);

            UpdateOverview(tree, conflictsLabelData);
        }

        void IncomingChangesViewLogic.IIncomingChangesView.UpdatePendingConflictsLabel(
            PendingConflictsLabelData data)
        {
        }

        void IncomingChangesViewLogic.IIncomingChangesView.UpdateSolvedFileConflicts(
            List<IncomingChangeInfo> solvedConflicts,
            IncomingChangeInfo currentConflict)
        {
            mIncomingChangesTreeView.UpdateSolvedFileConflicts(
                solvedConflicts, currentConflict);
        }

        void IncomingChangesViewLogic.IIncomingChangesView.ShowMessage(
            string message, bool isErrorMessage)
        {
            if (isErrorMessage)
            {
                mErrorMessageLabelText = message;
                mIsErrorMessageLabelVisible = true;
                return;
            }

            mMessageLabelText = message;
            mIsMessageLabelVisible = true;
        }

        void IncomingChangesViewLogic.IIncomingChangesView.HideMessage()
        {
            mMessageLabelText = string.Empty;
            mIsMessageLabelVisible = false;

            mErrorMessageLabelText = string.Empty;
            mIsErrorMessageLabelVisible = false;
        }

        void IncomingChangesViewLogic.IIncomingChangesView.DisableProcessMergesButton()
        {
            mIsProcessMergesButtonEnabled = false;
        }

        void IncomingChangesViewLogic.IIncomingChangesView.ShowCancelButton()
        {
            mIsCancelMergesButtonEnabled = true;
            mIsCancelMergesButtonVisible = true;
        }

        void IncomingChangesViewLogic.IIncomingChangesView.HideCancelButton()
        {
            mIsCancelMergesButtonEnabled = false;
            mIsCancelMergesButtonVisible = false;
        }

        SelectedIncomingChangesGroupInfo IIncomingChangesViewMenuOperations.GetSelectedIncomingChangesGroupInfo()
        {
            return IncomingChangesSelection.GetSelectedGroupInfo(mIncomingChangesTreeView);
        }

        void IIncomingChangesViewMenuOperations.MergeContributors()
        {
            List<IncomingChangeInfo> fileConflicts = IncomingChangesSelection.
                GetSelectedFileConflictsIncludingMeta(mIncomingChangesTreeView);

            mIncomingChangesViewLogic.ProcessMergesForConflicts(
                MergeContributorType.MergeContributors,
                PlasticExeLauncher.BuildForMergeSelectedFiles(mWkInfo, true, mShowDownloadPlasticExeWindow),
                fileConflicts,
                RefreshAsset.BeforeLongAssetOperation,
                AfterProcessMerges,
                MergeSuccessfullyFinished);
        }

        void IIncomingChangesViewMenuOperations.MergeKeepingSourceChanges()
        {
            List<IncomingChangeInfo> fileConflicts = IncomingChangesSelection.
                GetSelectedFileConflictsIncludingMeta(mIncomingChangesTreeView);

            mIncomingChangesViewLogic.ProcessMergesForConflicts(
                MergeContributorType.KeepSource,
                null,
                fileConflicts,
                RefreshAsset.BeforeLongAssetOperation,
                AfterProcessMerges,
                MergeSuccessfullyFinished);
        }

        void IIncomingChangesViewMenuOperations.MergeKeepingWorkspaceChanges()
        {
            List<IncomingChangeInfo> fileConflicts = IncomingChangesSelection.
                GetSelectedFileConflictsIncludingMeta(mIncomingChangesTreeView);

            mIncomingChangesViewLogic.ProcessMergesForConflicts(
                MergeContributorType.KeepDestination,
                null,
                fileConflicts,
                RefreshAsset.BeforeLongAssetOperation,
                AfterProcessMerges,
                MergeSuccessfullyFinished);
        }

        void IIncomingChangesViewMenuOperations.DiffIncomingChanges()
        {
            IncomingChangeInfo incomingChange = IncomingChangesSelection.
                GetSingleSelectedIncomingChange(mIncomingChangesTreeView);

            if (incomingChange == null)
                return;

            DiffIncomingChanges(
                mShowDownloadPlasticExeWindow,
                incomingChange,
                mWkInfo);
        }

        void IIncomingChangesViewMenuOperations.DiffYoursWithIncoming()
        {
            IncomingChangeInfo incomingChange = IncomingChangesSelection.
                GetSingleSelectedIncomingChange(mIncomingChangesTreeView);

            if (incomingChange == null)
                return;

            DiffYoursWithIncoming(
                mShowDownloadPlasticExeWindow,
                incomingChange,
                mWkInfo);
        }

        void IIncomingChangesViewMenuOperations.CopyFilePath(bool relativePath)
        {
            EditorGUIUtility.systemCopyBuffer = GetFilePathList.FromIncomingChangeInfos(
                IncomingChangesSelection.GetSelectedFileConflictsIncludingMeta(
                    mIncomingChangesTreeView),
                relativePath,
                mWkInfo.ClientPath);
        }

        void IncomingChangesViewMenu.IMetaMenuOperations.DiffIncomingChanges()
        {
            IncomingChangeInfo incomingChange = IncomingChangesSelection.
                GetSingleSelectedIncomingChange(mIncomingChangesTreeView);

            if (incomingChange == null)
                return;

            DiffIncomingChanges(
                mShowDownloadPlasticExeWindow,
                mIncomingChangesTreeView.GetMetaChange(incomingChange),
                mWkInfo);
        }

        void IncomingChangesViewMenu.IMetaMenuOperations.DiffYoursWithIncoming()
        {
            IncomingChangeInfo incomingChange = IncomingChangesSelection.
                GetSingleSelectedIncomingChange(mIncomingChangesTreeView);

            if (incomingChange == null)
                return;

            DiffYoursWithIncoming(
                mShowDownloadPlasticExeWindow,
                mIncomingChangesTreeView.GetMetaChange(incomingChange),
                mWkInfo);
        }

        bool IncomingChangesViewMenu.IMetaMenuOperations.SelectionHasMeta()
        {
            return mIncomingChangesTreeView.SelectionHasMeta();
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mIncomingChangesTreeView.SetFocusAndEnsureSelectedItem();
        }

        static void DiffIncomingChanges(
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            IncomingChangeInfo incomingChange,
            WorkspaceInfo wkInfo)
        {
            DiffOperation.DiffRevisions(
                wkInfo,
                incomingChange.GetMount().RepSpec,
                incomingChange.GetBaseRevision(),
                incomingChange.GetRevision(),
                incomingChange.GetPath(),
                incomingChange.GetPath(),
                true,
                PlasticExeLauncher.BuildForDiffContributors(wkInfo, true, showDownloadPlasticExeWindow),
                imageDiffLauncher: null);
        }

        static void DiffYoursWithIncoming(
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            IncomingChangeInfo incomingChange,
            WorkspaceInfo wkInfo)
        {
            DiffOperation.DiffYoursWithIncoming(
                wkInfo,
                incomingChange.GetMount(),
                incomingChange.GetRevision(),
                incomingChange.GetPath(),
                PlasticExeLauncher.BuildForDiffContributors(wkInfo, true, showDownloadPlasticExeWindow),
                imageDiffLauncher: null);
        }

        void UpdateProcessMergesButtonText()
        {
            mProcessMergesButtonText =
                mIncomingChangesViewLogic.GetProcessMergesButtonText();
        }

        void ShowProcessMergesButton(string processMergesButtonText)
        {
            mProcessMergesButtonText = processMergesButtonText;
            mIsProcessMergesButtonEnabled = true;
            mIsProcessMergesButtonVisible = true;
        }

        static void DoActionToolbarMessage(
            bool isMessageLabelVisible,
            string messageLabelText,
            bool isErrorMessageLabelVisible,
            string errorMessageLabelText,
            int fileConflictCount,
            MergeViewTexts.ChangesToApplySummary changesSummary)
        {
            if (isMessageLabelVisible)
                DoInfoMessage(messageLabelText);

            if (isErrorMessageLabelVisible)
            {
                DoErrorMessage(errorMessageLabelText);
            }

            if (!isMessageLabelVisible && !isErrorMessageLabelVisible)
            {
                DrawMergeOverview.For(
                    0,
                    fileConflictCount,
                    changesSummary);
            }
        }

        static void DoInfoMessage(string message)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(message, UnityStyles.MergeTab.InfoLabel);

            EditorGUILayout.EndHorizontal();
        }

        static void DoErrorMessage(string message)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(message, UnityStyles.MergeTab.RedPendingConflictsOfTotalLabel);

            EditorGUILayout.EndHorizontal();
        }

        static void DoActionsToolbar(
            SearchField searchField,
            IncomingChangesTreeView incomingChangesTreeView,
            IRefreshableView view)
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

            GUILayout.FlexibleSpace();

            // We don't have the filtering implemented at the plasticgui level: IncomingChangesTree.Filter
            // When it's implemented, just uncomment this method to show the filter textbox for partial workspaces
            // VCS-1006158 [Desktop] gluon: add filter in incoming changes view

            /*
            DrawSearchField.For(
                searchField,
                incomingChangesTreeView,
                UnityConstants.SEARCH_FIELD_WIDTH);
            */

            EditorGUILayout.EndHorizontal();
        }

        void DoIncomingChangesArea(
            IncomingChangesTreeView incomingChangesTreeView,
            EmptyStatePanel emptyStatePanel,
            DelayedActionBySecondsRunner delayedClearOperationSuccessAction,
            bool isOperationRunning,
            bool isUpdateSuccessful)
        {
            EditorGUILayout.BeginVertical();

            DoIncomingChangesTreeViewArea(
                incomingChangesTreeView,
                emptyStatePanel,
                delayedClearOperationSuccessAction,
                isOperationRunning,
                isUpdateSuccessful);

            EditorGUILayout.EndVertical();
        }

        static void DoProcessMergesButton(
            bool isEnabled,
            string processMergesButtonText,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            IncomingChangesViewLogic incomingChangesViewLogic,
            IncomingChangesTreeView incomingChangesTreeView,
            WorkspaceInfo wkInfo,
            Action beforeProcessMergesAction,
            EndOperationDelegateForUpdateProgress afterProcessMergesAction,
            Action successProcessMergesAction)
        {
            GUI.enabled = isEnabled;

            GUILayout.Space(3);

            if (DrawActionButton.For(processMergesButtonText))
            {
                List<IncomingChangeInfo> incomingChanges =
                    incomingChangesViewLogic.GetCheckedChanges();

                incomingChangesTreeView.FillWithMeta(incomingChanges);

                if (incomingChanges.Count == 0)
                    return;

                incomingChangesViewLogic.ProcessMergesForItems(
                    incomingChanges,
                    PlasticExeLauncher.BuildForResolveConflicts(wkInfo, true, showDownloadPlasticExeWindow),
                    beforeProcessMergesAction,
                    afterProcessMergesAction,
                    successProcessMergesAction);
            }

            GUI.enabled = true;
        }

        static bool DoCancelMergesButton(
            bool isEnabled,
            IncomingChangesViewLogic incomingChangesViewLogic)
        {
            bool shouldCancelMergesButtonEnabled = true;

            GUI.enabled = isEnabled;

            GUILayout.Space(3);

            if (DrawActionButton.For(PlasticLocalization.GetString(
                    PlasticLocalization.Name.CancelButton)))
            {
                incomingChangesViewLogic.Cancel();

                shouldCancelMergesButtonEnabled = false;
            }

            GUI.enabled = true;

            return shouldCancelMergesButtonEnabled;
        }

        static void UpdateIncomingChangesTree(
            IncomingChangesTreeView incomingChangesTreeView,
            IncomingChangesTree tree)
        {
            incomingChangesTreeView.BuildModel(
                UnityIncomingChangesTree.BuildIncomingChangeCategories(tree));
            incomingChangesTreeView.Refilter();
            incomingChangesTreeView.Sort();
            incomingChangesTreeView.Reload();
        }

        void DoIncomingChangesTreeViewArea(
            IncomingChangesTreeView incomingChangesTreeView,
            EmptyStatePanel emptyStatePanel,
            DelayedActionBySecondsRunner delayedClearOperationSuccessAction,
            bool isOperationRunning,
            bool isUpdateSuccessful)
        {
            using (new EditorGUI.DisabledScope(isOperationRunning))
            {
                Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

                incomingChangesTreeView.OnGUI(rect);

                if (isOperationRunning)
                    return;

                if (Event.current.type == EventType.Layout)
                    mShouldShowEmptyState = incomingChangesTreeView.GetTotalItemCount() == 0;

                if (mShouldShowEmptyState)
                {
                    DrawEmptyState(
                        rect,
                        emptyStatePanel,
                        delayedClearOperationSuccessAction,
                        isUpdateSuccessful);
                    return;
                }

                if (isUpdateSuccessful)
                    NotifySuccessInStatusBar();
            }
        }

        static void DrawEmptyState(
            Rect rect,
            EmptyStatePanel emptyStatePanel,
            DelayedActionBySecondsRunner delayedClearOperationSuccessAction,
            bool isUpdateSuccessful)
        {
            if (isUpdateSuccessful && !delayedClearOperationSuccessAction.IsRunning)
                delayedClearOperationSuccessAction.Run();

            emptyStatePanel.OnGUI(rect);
        }

        static string GetEmptyStateMessage(
            bool isUpdateSuccessful,
            string searchString)
        {
            if (isUpdateSuccessful)
                return PlasticLocalization.Name.WorkspaceUpdateCompleted.GetString();

            if (!string.IsNullOrEmpty(searchString))
                return PlasticLocalization.Name.NoIncomingChangesMatchingFilters.GetString();

            return PlasticLocalization.Name.NoIncomingChangesFound.GetString();
        }

        void AfterProcessMerges(UpdateProgress progress)
        {
            mAfterOnGUIAction = () =>
            {
                RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateProgress(mWkInfo, progress));
            };
            }

        void ExecuteAfterOnGUIAction()
        {
            if (mProgressControls.IsOperationRunning())
                return;

            if (mAfterOnGUIAction == null)
                return;

            mAfterOnGUIAction();
            mAfterOnGUIAction = null;
        }

        void NotifySuccessInStatusBar()
        {
            mWindowStatusBar.Notify(
                new GUIContentNotification(
                    PlasticLocalization.Name.WorkspaceUpdateCompleted.GetString()),
                MessageType.None,
                Images.GetStepOkIcon());
            mIsUpdateSuccessful = false;
        }

        void MergeSuccessfullyFinished()
        {
            mIsUpdateSuccessful = true;
            UpdateEmptyStateMessage();
        }

        void UpdateOverview(
            IncomingChangesTree incomingChangesTree,
            PendingConflictsLabelData conflictsLabelData)
        {
            mChangesSummary = BuildFilesSummaryData.
                GetChangesToApplySummary(incomingChangesTree);

            mFileConflictCount = conflictsLabelData.PendingConflictsCount;
        }

        void DelayedClearUpdateSuccess()
        {
            mIsUpdateSuccessful = false;

            UpdateEmptyStateMessage();
        }

        void UpdateEmptyStateMessage()
        {
            string searchString = mIncomingChangesTreeView.searchString;
            string message = GetEmptyStateMessage(mIsUpdateSuccessful, searchString);
            mEmptyStatePanel.UpdateContent(message, bDrawOkIcon: mIsUpdateSuccessful);
        }

        void BuildComponents()
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed +=
                SearchField_OnDownOrUpArrowKeyPressed;

            IncomingChangesTreeHeaderState incomingChangesHeaderState =
                IncomingChangesTreeHeaderState.GetDefault();
            TreeHeaderSettings.Load(incomingChangesHeaderState,
                UnityConstants.GLUON_INCOMING_CHANGES_TABLE_SETTINGS_NAME,
                (int)IncomingChangesTreeColumn.Path, true);

            mIncomingChangesTreeView = new IncomingChangesTreeView(
                mWkInfo, incomingChangesHeaderState,
                IncomingChangesTreeHeaderState.GetColumnNames(),
                new IncomingChangesViewMenu(this, this),
                UpdateProcessMergesButtonText,
                UpdateEmptyStateMessage);
            mIncomingChangesTreeView.Reload();

            UpdateEmptyStateMessage();

            mErrorsPanel = new ErrorsPanel(
                PlasticLocalization.Name.IncomingChangesCannotBeApplied.GetString(),
                UnityConstants.GLUON_INCOMING_ERRORS_TABLE_SETTINGS_NAME,
                UnityConstants.GLUON_INCOMING_ERRORS_PANEL_SPLITTER_SETTINGS_NAME);
        }

        bool mShouldShowEmptyState;
        bool mIsVisible;
        bool mIsProcessMergesButtonVisible;
        bool mIsCancelMergesButtonVisible;
        bool mIsMessageLabelVisible;
        bool mIsErrorMessageLabelVisible;
        bool mIsProcessMergesButtonEnabled;
        bool mIsCancelMergesButtonEnabled;
        bool mIsUpdateSuccessful;
        string mProcessMergesButtonText;
        string mMessageLabelText;
        string mErrorMessageLabelText;
        Action mAfterOnGUIAction;

        int mFileConflictCount;
        MergeViewTexts.ChangesToApplySummary mChangesSummary;

        SearchField mSearchField;
        IncomingChangesTreeView mIncomingChangesTreeView;
        ErrorsPanel mErrorsPanel;

        readonly SplitterState mErrorsSplitterState;

        readonly IncomingChangesViewLogic mIncomingChangesViewLogic;
        readonly DelayedActionBySecondsRunner mDelayedClearUpdateSuccessAction;
        readonly ProgressControlsForViews mProgressControls;
        readonly EmptyStatePanel mEmptyStatePanel;
        readonly EditorWindow mParentWindow;
        readonly WindowStatusBar mWindowStatusBar;
        readonly IncomingChangesUpdater mIncomingChangesUpdater;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly WorkspaceInfo mWkInfo;
    }
}
