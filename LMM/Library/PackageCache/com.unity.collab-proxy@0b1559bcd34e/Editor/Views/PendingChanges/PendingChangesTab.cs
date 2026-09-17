using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Common;
using Codice.Client.Common.FsNodeReaders;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.CM.Common.Merge;
using Codice.CM.Common.Mount;
using Codice.LogWrapper;
using GluonGui;
using GluonGui.WorkspaceWindow.Views.Checkin.Operations;
using GluonGui.WorkspaceWindow.Views.Shelves;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Diff;
using PlasticGui.WorkspaceWindow.Items;
using PlasticGui.WorkspaceWindow.Open;
using PlasticGui.WorkspaceWindow.PendingChanges;
using PlasticGui.WorkspaceWindow.PendingChanges.Changelists;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Settings;
using Unity.PlasticSCM.Editor.StatusBar;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Errors;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs;
using Unity.PlasticSCM.Editor.Views.PendingChanges.PendingMergeLinks;
using Unity.PlasticSCM.Editor.Views.Changesets;

using DiffWindow = Plugins.PlasticSCM.Editor.Diff.DiffWindow;

#if !UNITY_6000_3_OR_NEWER
using SplitterGUILayout = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterGUILayout;
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal partial class PendingChangesTab :
        IRefreshableView,
        PendingChangesOptionsFoldout.IAutoRefreshView,
        IPendingChangesView,
        CheckinUIOperation.ICheckinView,
        ShelveOperations.ICheckinView,
        PendingChangesViewPendingChangeMenu.IMetaMenuOperations,
        IPendingChangesMenuOperations,
        IChangelistMenuOperations,
        IOpenMenuOperations,
        PendingChangesViewPendingChangeMenu.IAdvancedUndoMenuOperations,
        IFilesFilterPatternsMenuOperations,
        PendingChangesViewMenu.IGetSelectedNodes,
        ChangesetsTab.IRevertToChangesetListener,
        CommentArea.IPendingChangesTabOperations
    {
        internal string EmptyStateMessage { get { return mEmptyStatePanel.Text; } }
        internal PendingChangesTreeView Table { get { return mPendingChangesTreeView; } }
        internal IProgressControls ProgressControls { get { return mProgressControls; } }
        internal bool IsVisible { get; set; }

        internal void SetChangesForTesting(List<ChangeInfo> changes)
        {
            UpdateChangesTree(changes);
        }

        internal void SetMergeLinksForTesting(
            IDictionary<MountPoint, IList<PendingMergeLink>> mergeLinks)
        {
            mPendingMergeLinks = mergeLinks;

            UpdateMergeLinksList();
        }

        internal void SetDrawOperationSuccessForTesting(IDrawOperationSuccess drawOperationSuccess)
        {
            mDrawOperationSuccess = drawOperationSuccess;
            mIsOperationSuccessPendingToDraw = true;
        }

        internal PendingChangesTab(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ViewHost viewHost,
            WorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IShowChangesetInView showChangesetInView,
            IShowShelveInView showShelveInView,
            IMergeViewLauncher mergeViewLauncher,
            IHistoryViewLauncher historyViewLauncher,
            IAssetStatusCache assetStatusCache,
            ISaveAssets saveAssets,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            INewChangesInWk newChangesInWk,
            IPendingChangesUpdater pendingChangesUpdater,
            IIncomingChangesUpdater developerIncomingChangesUpdater,
            IIncomingChangesUpdater gluonIncomingChangesUpdater,
            IShelvedChangesUpdater shelvedChangesUpdater,
            CheckPendingChanges.IUpdatePendingChanges updatePendingChanges,
            WindowStatusBar windowStatusBar,
            EditorWindow parentWindow,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mViewHost = viewHost;
            mWorkspaceWindow = workspaceWindow;
            mViewSwitcher = viewSwitcher;
            mShowChangesetInView = showChangesetInView;
            mShowShelveInView = showShelveInView;
            mHistoryViewLauncher = historyViewLauncher;
            mAssetStatusCache = assetStatusCache;
            mSaveAssets = saveAssets;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            mNewChangesInWk = newChangesInWk;
            mPendingChangesUpdater = pendingChangesUpdater;
            mDeveloperIncomingChangesUpdater = developerIncomingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mUpdatePendingChanges = updatePendingChanges;
            mWindowStatusBar = windowStatusBar;
            mParentWindow = parentWindow;
            mIsGluonMode = isGluonMode;

            mGuiMessage = new UnityPlasticGuiMessage();
            mCheckedStateManager = new PendingChangesViewCheckedStateManager();
            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);

            BuildComponents(isGluonMode, parentWindow.Repaint);

            mProgressControls = new ProgressControlsForViews();

            if (mErrorsPanel != null)
            {
                mErrorsSplitterState = new SplitterState(
                    SplitterSettings.Load(
                        UnityConstants.PENDING_CHANGES_ERRORS_SPLITTER_SETTINGS_NAME,
                        new float[] { 0.75f, 0.25f }),
                    new int[] { 100, 100 },
                    new int[] { 100000, 100000 }
                );
            }

            mCommentsSplitterState = new SplitterState(
                SplitterSettings.Load(
                    UnityConstants.PENDING_CHANGES_COMMENTS_SPLITTER_SETTINGS_NAME,
                    new float[] { 0.78f, 0.22f }),
                new int[] { 160, 160 },
                new int[] { 100000, 100000 }
            );

            workspaceWindow.RegisterPendingChangesProgressControls(mProgressControls);

            mPendingChangesOperations = new PendingChangesOperations(
                wkInfo,
                workspaceWindow,
                viewSwitcher,
                mergeViewLauncher,
                this,
                mProgressControls,
                workspaceWindow,
                pendingChangesUpdater,
                isGluonMode ?
                    gluonIncomingChangesUpdater :
                    developerIncomingChangesUpdater,
                shelvedChangesUpdater,
                null,
                null);

            Refresh();
        }

        internal void AutoRefresh()
        {
            if (mIsAutoRefreshDisabled)
                return;

            if (!PlasticGuiConfig.Get().Configuration.CommitAutoRefresh)
                return;

            if (mNewChangesInWk != null && !mNewChangesInWk.Detected())
                return;

            Refresh();
        }

        internal void Refresh(PendingChangesStatus pendingChangesStatus = null)
        {
            if (IsOperationRunning())
                return;

            if (mDeveloperIncomingChangesUpdater != null)
                mDeveloperIncomingChangesUpdater.Update(DateTime.Now);

            if (mGluonIncomingChangesUpdater != null)
                mGluonIncomingChangesUpdater.Update(DateTime.Now);

            if (mShelvedChangesUpdater != null)
                mShelvedChangesUpdater.Update(DateTime.Now);

            FillPendingChanges(mNewChangesInWk, pendingChangesStatus);
        }

        internal void ClearIsCommentWarningNeeded()
        {
            mIsEmptyCheckinCommentWarningNeeded = false;
            mIsEmptyShelveCommentWarningNeeded = false;
        }

        internal void UpdateIsCheckinCommentWarningNeeded(string comment)
        {
            mIsEmptyCheckinCommentWarningNeeded =
                string.IsNullOrEmpty(comment) &&
                PlasticGuiConfig.Get().Configuration.ShowEmptyCommentWarning;

            mNeedsToShowEmptyCheckinCommentDialog = mIsEmptyCheckinCommentWarningNeeded;
        }

        internal void UpdateIsShelveCommentWarningNeeded(string comment)
        {
            mIsEmptyShelveCommentWarningNeeded =
                string.IsNullOrEmpty(comment) &&
                PlasticGuiConfig.Get().Configuration.ShowEmptyShelveCommentWarning;

            mNeedsToShowEmptyShelveCommentDialog = mIsEmptyShelveCommentWarningNeeded;
        }

        internal void OnEnable()
        {
            mIsEnabled = true;

            mSearchField.downOrUpArrowKeyPressed +=
                SearchField_OnDownOrUpArrowKeyPressed;
        }

        internal void OnDisable()
        {
            mIsEnabled = false;

            mSearchField.downOrUpArrowKeyPressed -=
                SearchField_OnDownOrUpArrowKeyPressed;

            TreeViewSessionState.Save(
                mPendingChangesTreeView,
                UnityConstants.PENDING_CHANGES_UNCHECKED_ITEMS_KEY_NAME);

            TreeHeaderSettings.Save(
                mPendingChangesTreeView.multiColumnHeader.state,
                UnityConstants.PENDING_CHANGES_TABLE_SETTINGS_NAME);

            SplitterSettings.Save(
                mCommentsSplitterState,
                UnityConstants.PENDING_CHANGES_COMMENTS_SPLITTER_SETTINGS_NAME);

            if (mErrorsSplitterState != null)
            {
                SplitterSettings.Save(
                    mErrorsSplitterState,
                    UnityConstants.PENDING_CHANGES_ERRORS_SPLITTER_SETTINGS_NAME);
            }

            if (mErrorsPanel != null)
                mErrorsPanel.OnDisable();

            mCommentArea.OnDisable();
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);

            // Display the empty comment dialog here, otherwise it causes errors in the OnGUI method
            if (mNeedsToShowEmptyCheckinCommentDialog)
            {
                mNeedsToShowEmptyCheckinCommentDialog = false;

                mHasPendingCheckinFromPreviousUpdate =
                    EmptyCommentDialog.ShouldContinueWithCheckin(mParentWindow, mWkInfo);

                mIsEmptyCheckinCommentWarningNeeded = !mHasPendingCheckinFromPreviousUpdate;
            }

            if (mNeedsToShowEmptyShelveCommentDialog)
            {
                mNeedsToShowEmptyShelveCommentDialog = false;

                mHasPendingShelveFromPreviousUpdate =
                    EmptyCommentDialog.ShouldContinueWithShelve(mParentWindow, mWkInfo);

                mIsEmptyShelveCommentWarningNeeded = !mHasPendingShelveFromPreviousUpdate;
            }

            if (mHasPendingCheckinFromPreviousUpdate)
            {
                mHasPendingCheckinFromPreviousUpdate = false;
                CheckinForMode(mIsGluonMode, mCommentArea.KeepItemsLocked);
            }

            if (mHasPendingShelveFromPreviousUpdate)
            {
                mHasPendingShelveFromPreviousUpdate = false;
                ShelveForMode(mIsGluonMode, mCommentArea.KeepItemsLocked);
            }
        }

        internal void OnGUI(ResolvedUser currentUser)
        {
            SplitterGUILayout.BeginHorizontalSplit(mCommentsSplitterState);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    if (mErrorsPanel != null && mErrorsPanel.IsVisible)
                        SplitterGUILayout.BeginVerticalSplit(mErrorsSplitterState);

                    DoWarningMessage();
                    DoActionsToolbar();

                    Rect viewRect = OverlayProgress.CaptureViewRectangle();

                    DoContentArea();

                    if (mErrorsPanel != null && mErrorsPanel.IsVisible)
                    {
                        mErrorsPanel.OnGUI();
                        SplitterGUILayout.EndVerticalSplit();
                    }

                    if (mProgressControls.HasNotification())
                        DrawProgressForViews.ForNotificationArea(mProgressControls.ProgressData);

                    if (IsOperationRunning())
                    {
                        OverlayProgress.DoOverlayProgress(
                            viewRect,
                            mProgressControls.ProgressData.ProgressPercent,
                            mProgressControls.ProgressData.ProgressMessage);
                    }
                }

                DoVerticalSeparator();
            }

            mCommentArea.OnGUI(currentUser, IsOperationRunning());

            SplitterGUILayout.EndHorizontalSplit();

            ExecuteAfterOnGUIAction();
        }

        void DoWarningMessage()
        {
            EditorGUILayout.BeginVertical();

            if (!string.IsNullOrEmpty(mGluonWarningMessage))
                DoWarningMessage(mGluonWarningMessage);

            EditorGUILayout.EndVertical();
        }

        void DoActionsToolbar()
        {
            EditorGUILayout.BeginVertical();

            DoActionsToolbar(
                mProgressControls,
                mSearchField,
                mPendingChangesTreeView,
                this);

            EditorGUILayout.EndVertical();
        }

        void DoContentArea()
        {
            EditorGUILayout.BeginVertical();

            DoChangesArea(
                mPendingChangesTreeView,
                mEmptyStatePanel,
                IsOperationRunning(),
                mDrawOperationSuccess);

            if (HasPendingMergeLinks() && !mHasPendingMergeLinksFromRevert)
            {
                Rect availableWidthRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                DoMergeLinksArea(mMergeLinksListView, availableWidthRect.width);
            }

            EditorGUILayout.EndVertical();
        }

        void IPendingChangesView.SetDefaultComment(string defaultComment)
        {
        }

        void IPendingChangesView.ClearComments()
        {
            mCommentArea.ClearComments();
        }

        void IRefreshableView.Refresh()
        {
            Refresh();
        }

        void PendingChangesOptionsFoldout.IAutoRefreshView.DisableAutoRefresh()
        {
            mIsAutoRefreshDisabled = true;
        }

        void PendingChangesOptionsFoldout.IAutoRefreshView.EnableAutoRefresh()
        {
            mIsAutoRefreshDisabled = false;
        }

        void PendingChangesOptionsFoldout.IAutoRefreshView.ForceRefresh()
        {
            ((IRefreshableView)this).Refresh();
        }

        void IPendingChangesView.ClearChangesToCheck(List<string> changes)
        {
            mCheckedStateManager.ClearChangesToCheck(changes);

            mParentWindow.Repaint();
        }

        void IPendingChangesView.CleanCheckedElements(List<ChangeInfo> checkedChanges)
        {
            mCheckedStateManager.Clean(checkedChanges);

            mParentWindow.Repaint();
        }

        void IPendingChangesView.CheckChanges(List<string> changesToCheck)
        {
            mCheckedStateManager.SetChangesToCheck(changesToCheck);

            mParentWindow.Repaint();
        }

        bool IPendingChangesView.IncludeDependencies(
            IList<ChangeDependencies> changesDependencies,
            string operation)
        {
            return DependenciesDialog.IncludeDependencies(
                mWkInfo, changesDependencies, operation, mParentWindow);
        }

        SearchMatchesData IPendingChangesView.AskForMatches(string changePath)
        {
            throw new NotImplementedException();
        }

        void IPendingChangesView.CleanLinkedTasks()
        {
        }

        void CheckinUIOperation.ICheckinView.CollapseWarningMessagePanel()
        {
            mGluonWarningMessage = string.Empty;

            mParentWindow.Repaint();
        }

        void CheckinUIOperation.ICheckinView.ExpandWarningMessagePanel(string text)
        {
            mGluonWarningMessage = text;

            mParentWindow.Repaint();
        }

        void CheckinUIOperation.ICheckinView.ClearComments()
        {
            mCommentArea.ClearComments();
        }

        void ShelveOperations.ICheckinView.OnShelvesetApplied(List<ErrorMessage> errorMessages)
        {
            mViewSwitcher.ShowPendingChanges();
            mErrorsPanel.UpdateErrorsList(errorMessages);
        }

        bool PendingChangesViewPendingChangeMenu.IMetaMenuOperations.SelectionHasMeta()
        {
            return mPendingChangesTreeView.SelectionHasMeta();
        }

        void PendingChangesViewPendingChangeMenu.IMetaMenuOperations.DiffMeta()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);
            ChangeInfo selectedChangeMeta = mPendingChangesTreeView.GetMetaChange(
                selectedChange);

            ChangeInfo changedForMoved = mPendingChangesTreeView.GetChangedForMoved(selectedChange);
            ChangeInfo changedForMovedMeta = (changedForMoved == null) ?
                null : mPendingChangesTreeView.GetMetaChange(changedForMoved);

            if (UseBuiltinDiffWindowPreference.IsEnabled())
            {
                DiffWindow diffWindow = ShowWindow.Diff();

                diffWindow.ShowDiffFromChange(
                    selectedChangeMeta,
                    changedForMovedMeta);
                return;
            }

            DiffOperation.DiffWorkspaceContent(
                mWkInfo,
                selectedChangeMeta,
                changedForMovedMeta,
                mProgressControls,
                PlasticExeLauncher.BuildForDiffWorkspaceContent(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                null);
        }

        void PendingChangesViewPendingChangeMenu.IMetaMenuOperations.HistoryMeta()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);
            ChangeInfo selectedChangeMeta = mPendingChangesTreeView.GetMetaChange(
                selectedChange);

            mHistoryViewLauncher.ShowHistoryView(
                selectedChangeMeta.RepositorySpec,
                selectedChangeMeta.RevInfo.ItemId,
                selectedChangeMeta.Path,
                selectedChangeMeta.IsDirectory);
        }

        void PendingChangesViewPendingChangeMenu.IMetaMenuOperations.OpenMeta()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedMetaPaths(mPendingChangesTreeView);

            FileSystemOperation.Open(selectedPaths);
        }

        void PendingChangesViewPendingChangeMenu.IMetaMenuOperations.OpenMetaWith()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedMetaPaths(mPendingChangesTreeView);

            OpenOperation.OpenWith(
                FileSystemOperation.GetExePath(),
                selectedPaths);
        }

        void PendingChangesViewPendingChangeMenu.IMetaMenuOperations.OpenMetaInExplorer()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedMetaPaths(mPendingChangesTreeView);

            if (selectedPaths.Count < 1)
                return;

            FileSystemOperation.OpenInExplorer(selectedPaths[0]);
        }

        SelectedChangesGroupInfo IPendingChangesMenuOperations.GetSelectedChangesGroupInfo()
        {
            return PendingChangesSelection.GetSelectedChangesGroupInfo(
                mWkInfo.ClientPath, mPendingChangesTreeView);
        }

        void IPendingChangesMenuOperations.Diff()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);

            if (UseBuiltinDiffWindowPreference.IsEnabled())
            {
                DiffWindow diffWindow = ShowWindow.Diff();
                diffWindow.ShowDiffFromChange(
                    selectedChange,
                    mPendingChangesTreeView.GetChangedForMoved(selectedChange));
                return;
            }

            DiffOperation.DiffWorkspaceContent(
                mWkInfo,
                selectedChange,
                mPendingChangesTreeView.GetChangedForMoved(selectedChange),
                null,
                PlasticExeLauncher.BuildForDiffWorkspaceContent(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                null);
        }

        void IPendingChangesMenuOperations.UndoChanges()
        {
            List<ChangeInfo> changesToUndo = PendingChangesSelection
                .GetSelectedChanges(mPendingChangesTreeView,
                    bExcludePrivates: true);

            List<ChangeInfo> dependenciesCandidates =
                mPendingChangesTreeView.GetDependenciesCandidates(changesToUndo, true);

            UndoChangesForMode(mIsGluonMode, false, changesToUndo, dependenciesCandidates);
        }

        void IPendingChangesMenuOperations.SearchMatches()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);

            if (selectedChange == null)
                return;

            SearchMatchesOperation operation = new SearchMatchesOperation(
                mWkInfo,
                mWorkspaceWindow,
                this,
                mProgressControls,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                null);

            operation.SearchMatches(
                selectedChange,
                PendingChangesSelection.GetAllChanges(mPendingChangesTreeView),
                null);
        }

        void IPendingChangesMenuOperations.ApplyLocalChanges()
        {
            List<ChangeInfo> selectedChanges = PendingChangesSelection
                .GetSelectedChanges(mPendingChangesTreeView);

            if (selectedChanges.Count == 0)
                return;

            ApplyLocalChangesOperation operation = new ApplyLocalChangesOperation(
                mWkInfo,
                mWorkspaceWindow,
                this,
                mProgressControls,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                null);

            operation.ApplyLocalChanges(
                selectedChanges,
                PendingChangesSelection.GetAllChanges(mPendingChangesTreeView),
                null);
        }

        void IPendingChangesMenuOperations.CopyFilePath(bool relativePath)
        {
            EditorGUIUtility.systemCopyBuffer = GetFilePathList.FromSelectedPaths(
                PendingChangesSelection.GetSelectedPathsWithoutMeta(mPendingChangesTreeView),
                relativePath,
                mWkInfo.ClientPath);
        }

        void IPendingChangesMenuOperations.Delete()
        {
            List<string> privateDirectoriesToDelete;
            List<string> privateFilesToDelete;

            if (!mPendingChangesTreeView.GetSelectedPathsToDelete(
                    out privateDirectoriesToDelete,
                    out privateFilesToDelete))
                return;

            DeleteOperation.Delete(
                mWorkspaceWindow,
                mProgressControls,
                mWkInfo,
                privateDirectoriesToDelete,
                privateFilesToDelete,
                mPendingChangesUpdater,
                mIsGluonMode ?
                    mGluonIncomingChangesUpdater :
                    mDeveloperIncomingChangesUpdater,
                mShelvedChangesUpdater,
                () => RefreshAsset.UnityAssetDatabase(mAssetStatusCache));
        }

        void IPendingChangesMenuOperations.Annotate()
        {
            throw new NotImplementedException();
        }

        void IPendingChangesMenuOperations.History()
        {
            ChangeInfo selectedChange = PendingChangesSelection.
                GetSelectedChange(mPendingChangesTreeView);

            mHistoryViewLauncher.ShowHistoryView(
                selectedChange.RepositorySpec,
                selectedChange.RevInfo.ItemId,
                selectedChange.Path,
                selectedChange.IsDirectory);
        }

        SelectedChangesGroupInfo IChangelistMenuOperations.GetSelectedChangesGroupInfo()
        {
            return PendingChangesSelection.GetSelectedChangesGroupInfo(
                mWkInfo.ClientPath, mPendingChangesTreeView);
        }

        List<ChangeListInfo> IChangelistMenuOperations.GetSelectedChangelistInfos()
        {
            return PendingChangesSelection.GetSelectedChangeListInfos(
                mPendingChangesTreeView);
        }

        void IChangelistMenuOperations.Checkin()
        {
            List<ChangeInfo> changesToCheckin;
            List<ChangeInfo> dependenciesCandidates;

            mPendingChangesTreeView.GetCheckedChanges(
                PendingChangesSelection.GetSelectedChangelistNodes(mPendingChangesTreeView),
                false, out changesToCheckin, out dependenciesCandidates);

            CheckinChangesForMode(
                changesToCheckin,
                dependenciesCandidates,
                mIsGluonMode,
                mCommentArea.KeepItemsLocked);
        }

        void IChangelistMenuOperations.Shelve()
        {
            List<ChangeInfo> changesToShelve;
            List<ChangeInfo> dependenciesCandidates;

            mPendingChangesTreeView.GetCheckedChanges(
                PendingChangesSelection.GetSelectedChangelistNodes(mPendingChangesTreeView),
                false, out changesToShelve, out dependenciesCandidates);

            ShelveChangesForMode(
                changesToShelve,
                dependenciesCandidates,
                mIsGluonMode,
                mCommentArea.KeepItemsLocked);
        }

        void IChangelistMenuOperations.Undo()
        {
            List<ChangeInfo> changesToUndo;
            List<ChangeInfo> dependenciesCandidates;

            mPendingChangesTreeView.GetCheckedChanges(
                PendingChangesSelection.GetSelectedChangelistNodes(mPendingChangesTreeView),
                true, out changesToUndo, out dependenciesCandidates);

            UndoChangesForMode(mIsGluonMode, false, changesToUndo, dependenciesCandidates);
        }

        void IChangelistMenuOperations.CreateNew()
        {
            ChangelistCreationData changelistCreationData =
                CreateChangelistDialog.CreateChangelist(mWkInfo, mParentWindow);

            ChangelistOperations.CreateNew(mWkInfo, this, changelistCreationData);
        }

        void IChangelistMenuOperations.MoveToNewChangelist(List<ChangeInfo> changes)
        {
            ChangelistCreationData changelistCreationData =
                CreateChangelistDialog.CreateChangelist(mWkInfo, mParentWindow);

            if (!changelistCreationData.Result)
                return;

            ChangelistOperations.CreateNew(mWkInfo, this, changelistCreationData);

            ChangelistOperations.MoveToChangelist(
                mWkInfo, this, changes,
                changelistCreationData.ChangelistInfo.Name);
        }

        void IChangelistMenuOperations.Edit()
        {
            ChangeListInfo changelistToEdit = PendingChangesSelection.GetSelectedChangeListInfo(
                mPendingChangesTreeView);

            ChangelistCreationData changelistCreationData = CreateChangelistDialog.EditChangelist(
                mWkInfo,
                changelistToEdit,
                mParentWindow);

            ChangelistOperations.Edit(mWkInfo, this, changelistToEdit, changelistCreationData);
        }

        void IChangelistMenuOperations.Delete()
        {
            ChangelistOperations.Delete(
                mWkInfo,
                this,
                PendingChangesSelection.GetSelectedChangelistNodes(mPendingChangesTreeView));
        }

        void IChangelistMenuOperations.MoveToChangelist(
            List<ChangeInfo> changes,
            string targetChangelist)
        {
            ChangelistOperations.MoveToChangelist(
                mWkInfo,
                this,
                changes,
                targetChangelist);
        }

        void IOpenMenuOperations.Open()
        {
            List<string> selectedPaths = PendingChangesSelection.
                GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            FileSystemOperation.Open(selectedPaths);
        }

        void IOpenMenuOperations.OpenWith()
        {
            List<string> selectedPaths = PendingChangesSelection.
                GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            OpenOperation.OpenWith(
                FileSystemOperation.GetExePath(),
                selectedPaths);
        }

        void IOpenMenuOperations.OpenWithCustom(string exePath, string args)
        {
            List<string> selectedPaths = PendingChangesSelection.
                GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            OpenOperation.OpenWith(exePath, selectedPaths);
        }

        void IOpenMenuOperations.OpenInExplorer()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            if (selectedPaths.Count < 1)
                return;

            FileSystemOperation.OpenInExplorer(selectedPaths[0]);
        }

        void IFilesFilterPatternsMenuOperations.AddFilesFilterPatterns(
            FilterTypes type, FilterActions action, FilterOperationType operation)
        {
            List<string> selectedPaths = PendingChangesSelection.GetSelectedPaths(
                mPendingChangesTreeView);

            string[] rules = FilterRulesGenerator.GenerateRules(
                selectedPaths, mWkInfo.ClientPath, action, operation);

            bool isApplicableToAllWorkspaces = !mIsGluonMode;
            bool isAddOperation = operation == FilterOperationType.Add;

            FilterRulesConfirmationData filterRulesConfirmationData =
                FilterRulesConfirmationDialog.AskForConfirmation(
                    rules, isAddOperation, isApplicableToAllWorkspaces, mParentWindow);

            AddFilesFilterPatternsOperation.Run(
                mWkInfo,
                mWorkspaceWindow,
                type,
                operation,
                filterRulesConfirmationData,
                mPendingChangesUpdater);
        }

        void PendingChangesViewPendingChangeMenu.IAdvancedUndoMenuOperations.UndoUnchanged()
        {
            UndoUnchangedChanges(PendingChangesSelection.
                GetSelectedChanges(mPendingChangesTreeView, bExcludePrivates: true));
        }

        void PendingChangesViewPendingChangeMenu.IAdvancedUndoMenuOperations.UndoCheckoutsKeepingChanges()
        {
            UndoCheckoutChangesKeepingLocalChanges(PendingChangesSelection.
                GetSelectedChanges(mPendingChangesTreeView, bExcludePrivates: true));
        }

        List<IPlasticTreeNode> PendingChangesViewMenu.IGetSelectedNodes.GetSelectedNodes()
        {
            return mPendingChangesTreeView.GetSelectedNodes();
        }

        void ChangesetsTab.IRevertToChangesetListener.OnSuccessOperation()
        {
            mHasPendingMergeLinksFromRevert = true;
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mPendingChangesTreeView.SetFocusAndEnsureSelectedItem();
        }

        void ClearOperationSuccess()
        {
            if (mIsOperationSuccessPendingToDraw)
                return;

            mDrawOperationSuccess = null;
        }

        void FillPendingChanges(
            INewChangesInWk newChangesInWk, PendingChangesStatus pendingChangesStatus)
        {
            if (mIsRefreshing)
                return;

            mIsRefreshing = true;

            ClearOperationSuccess();

            List<ChangeInfo> changesToSelect =
                PendingChangesSelection.GetChangesToFocus(mPendingChangesTreeView);

            ((IProgressControls)mProgressControls).ShowProgress(PlasticLocalization.
                GetString(PlasticLocalization.Name.LoadingPendingChanges));

            bool bHasToUpdatePendingChangesNotification = pendingChangesStatus == null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    FilterManager.Get().Reload(mWkInfo);

                    if (pendingChangesStatus != null)
                        return;

                    if (newChangesInWk != null)
                        newChangesInWk.Detected();

                    pendingChangesStatus = new PendingChangesStatus(
                        GetStatus.ForWorkspace(
                            mWkInfo,
                            GetStatus.DefaultOptions(),
                            PendingChangesOptions.GetMovedMatchingOptions()),
                        PlasticGui.Plastic.API.GetPendingMergeLinks(mWkInfo));
                },
                /*afterOperationDelegate*/ delegate
                {
                    try
                    {
                        if (waiter.Exception != null)
                        {
                            if (!IsControlledException(waiter.Exception))
                                ExceptionsHandler.DisplayException(waiter.Exception);

                            return;
                        }

                        mPendingMergeLinks = pendingChangesStatus.PendingMergeLinks;

                        UpdateChangesTree(pendingChangesStatus.WorkspaceStatusResult.Changes);

                        RestoreData();

                        UpdateMergeLinksList();

                        PendingChangesSelection.SelectChanges(
                            mPendingChangesTreeView, changesToSelect);

                        RefreshAsset.VersionControlCache(mAssetStatusCache);

                        if (bHasToUpdatePendingChangesNotification)
                        {
                            CheckPendingChanges.UpdateNotification(
                                mWkInfo, mUpdatePendingChanges, pendingChangesStatus);
                        }
                    }
                    finally
                    {
                        ((IProgressControls)mProgressControls).HideProgress();

                        UpdateNotificationPanel();

                        mIsRefreshing = false;
                    }
                });
        }

        bool IsControlledException(Exception ex)
        {
            // This condition covers the scenario of a hanging refresh operation
            // that fails after the workspace has been removed
            return ex is CmException &&
                   NOT_EXISTING_WORKSPACE_MESSAGE_ID.Equals(((CmException) ex).KeyMessage);
        }

        void DoVerticalSeparator()
        {
            Rect result = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(result, UnityStyles.Colors.BarBorder);
        }

        void UpdateChangesTree(List<ChangeInfo> changes)
        {
            mPendingChangesTreeView.BuildModel(changes, mCheckedStateManager);

            mPendingChangesTreeView.Refilter();

            mPendingChangesTreeView.Sort();

            mPendingChangesTreeView.Reload();
        }

        static void DoWarningMessage(string message)
        {
            GUILayout.Label(message, UnityStyles.WarningMessage);
        }

        void UpdateMergeLinksList()
        {
            mMergeLinksListView.BuildModel(mPendingMergeLinks);

            mMergeLinksListView.Reload();

            if (!HasPendingMergeLinks())
                mHasPendingMergeLinksFromRevert = false;
        }

        void UpdateNotificationPanel()
        {
            if (PlasticGui.Plastic.API.IsFsReaderWatchLimitReached(mWkInfo))
            {
                ((IProgressControls)mProgressControls).ShowWarning(PlasticLocalization.
                    GetString(PlasticLocalization.Name.NotifyLinuxWatchLimitWarning));
                return;
            }
        }

        static void DoActionsToolbar(
            ProgressControlsForViews progressControls,
            SearchField searchField,
            PendingChangesTreeView pendingChangesTreeView,
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

            DrawSearchField.For(
                searchField,
                pendingChangesTreeView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            EditorGUILayout.EndHorizontal();
        }

        void DoChangesArea(
            PendingChangesTreeView changesTreeView,
            EmptyStatePanel emptyStatePanel,
            bool isOperationRunning,
            IDrawOperationSuccess drawOperationSuccess)
        {
            using (new EditorGUI.DisabledScope(isOperationRunning))
            {
                Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
                changesTreeView.OnGUI(rect);

                if (isOperationRunning)
                    return;

                if (Event.current.type == EventType.Layout)
                    mShouldShowEmptyState = changesTreeView.GetTotalItemCount() == 0;

                if (mShouldShowEmptyState)
                {
                    DrawEmptyState(
                        rect,
                        emptyStatePanel,
                        drawOperationSuccess);
                    return;
                }

                if (drawOperationSuccess != null)
                {
                    drawOperationSuccess.InStatusBar(mWindowStatusBar);
                    mDrawOperationSuccess = null;
                    mIsOperationSuccessPendingToDraw = false;
                }
            }
        }

        void ExecuteAfterOnGUIAction()
        {
            if (IsOperationRunning())
                return;

            if (mAfterOnGUIAction == null)
                return;

            mAfterOnGUIAction();
            mAfterOnGUIAction = null;
        }

        void DrawEmptyState(
            Rect rect,
            EmptyStatePanel emptyStatePanel,
            IDrawOperationSuccess drawOperationSuccess)
        {
            if (drawOperationSuccess == null)
            {
                emptyStatePanel.OnGUI(rect);
                return;
            }

            drawOperationSuccess.InEmptyState(rect);
            mIsOperationSuccessPendingToDraw = false;
        }

        static string GetEmptyStateMessage(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
                return PlasticLocalization.Name.NoPendingChangesMatchingFilters.GetString();

            return PlasticLocalization.Name.NoPendingChangesFoundUnityVCS.GetString();
        }

        bool HasPendingMergeLinks()
        {
            if (mPendingMergeLinks == null)
                return false;

            return mPendingMergeLinks.Count > 0;
        }

        bool IsOperationRunning()
        {
            return mProgressControls.IsOperationRunning() || mPendingChangesUpdater.IsRunning();
        }

        static void DoMergeLinksArea(
            MergeLinksListView mergeLinksListView, float width)
        {
            GUILayout.Label(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.MergeLinkDescriptionColumn),
                EditorStyles.boldLabel);

            float desiredTreeHeight = mergeLinksListView.DesiredHeight;

            Rect treeRect = GUILayoutUtility.GetRect(
                0,
                width,
                desiredTreeHeight,
                desiredTreeHeight);

            mergeLinksListView.OnGUI(treeRect);
        }

        void RestoreData()
        {
            if (!mRestoreData || !mIsEnabled)
                return;

            TreeViewSessionState.Restore(
                mPendingChangesTreeView,
                UnityConstants.PENDING_CHANGES_UNCHECKED_ITEMS_KEY_NAME);

            mRestoreData = false;
        }

        void OnDelayedSelectionChanged()
        {
            if (!IsVisible)
                return;

            if (!UseBuiltinDiffWindowPreference.IsEnabled())
                return;

            if (mPendingChangesTreeView.GetSelection().Count != 1)
                return;

            if (!PendingChangesSelection.IsApplicableDiffWorkspaceContent(mPendingChangesTreeView))
                return;

            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);

            DiffWindow diffWindow = GetWindowIfOpened.Diff();

            diffWindow?.ShowDiffFromChange(
                selectedChange,
                mPendingChangesTreeView.GetChangedForMoved(selectedChange));
        }

        void OnRowDoubleClickAction()
        {
            if (mPendingChangesTreeView.GetSelection().Count != 1)
                return;

            if (PendingChangesSelection.IsApplicableDiffWorkspaceContent(mPendingChangesTreeView))
            {
                if (UseBuiltinDiffWindowPreference.IsEnabled())
                {
                    ChangeInfo selectedChange = PendingChangesSelection
                        .GetSelectedChange(mPendingChangesTreeView);

                    DiffWindow diffWindow = ShowWindow.Diff();
                    diffWindow.ShowDiffFromChange(
                        selectedChange,
                        mPendingChangesTreeView.GetChangedForMoved(selectedChange));
                    return;
                }

                ((IPendingChangesMenuOperations)this).Diff();
                return;
            }

            int selectedNode = mPendingChangesTreeView.GetSelection()[0];

            if (mPendingChangesTreeView.IsExpanded(selectedNode))
            {
                mPendingChangesTreeView.SetExpanded(selectedNode, expanded: false);
                return;
            }

            mPendingChangesTreeView.SetExpanded(selectedNode, expanded: true);
        }

        void UpdateEmptyStateMessage()
        {
            string searchString = mPendingChangesTreeView.searchString;
            string message = GetEmptyStateMessage(searchString);
            mEmptyStatePanel.UpdateContent(message);
        }

        void BuildComponents(bool isGluonMode, Action repaintAction)
        {
            mCommentArea = new CommentArea(
                this, isGluonMode, ClearIsCommentWarningNeeded, repaintAction);

            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            PendingChangesTreeHeaderState headerState =
                PendingChangesTreeHeaderState.GetDefault(isGluonMode);
            TreeHeaderSettings.Load(headerState,
                UnityConstants.PENDING_CHANGES_TABLE_SETTINGS_NAME,
                (int)PendingChangesTreeColumn.Item, true);

            mPendingChangesTreeView = new PendingChangesTreeView(
                mWkInfo, isGluonMode, headerState,
                PendingChangesTreeHeaderState.GetColumnNames(),
                new PendingChangesViewMenu(
                    mWkInfo, this, this, this, this, this, this, this, isGluonMode),
                mAssetStatusCache,
                OnDelayedSelectionChanged,
                OnRowDoubleClickAction,
                UpdateEmptyStateMessage);
            mPendingChangesTreeView.Reload();

            UpdateEmptyStateMessage();

            mMergeLinksListView = new MergeLinksListView();
            mMergeLinksListView.Reload();

            if (isGluonMode)
                mErrorsPanel = new ErrorsPanel(
                    PlasticLocalization.Name.ChangesCannotBeApplied.GetString(),
                    UnityConstants.PENDING_CHANGES_ERRORS_TABLE_SETTINGS_NAME,
                    UnityConstants.PENDING_CHANGES_ERRORS_PANEL_SPLITTER_SETTINGS_NAME);
        }

        bool mShouldShowEmptyState;
        bool mIsRefreshing;
        bool mIsAutoRefreshDisabled;
        bool mIsEmptyCheckinCommentWarningNeeded = false;
        bool mNeedsToShowEmptyCheckinCommentDialog = false;
        bool mHasPendingCheckinFromPreviousUpdate = false;
        bool mIsEmptyShelveCommentWarningNeeded = false;
        bool mNeedsToShowEmptyShelveCommentDialog = false;
        bool mHasPendingShelveFromPreviousUpdate = false;
        bool mHasPendingMergeLinksFromRevert = false;
        bool mRestoreData = true;
        bool mIsEnabled = true;
        string mGluonWarningMessage;
        Action mAfterOnGUIAction;
        IDictionary<MountPoint, IList<PendingMergeLink>> mPendingMergeLinks;

        IDrawOperationSuccess mDrawOperationSuccess;
        bool mIsOperationSuccessPendingToDraw = false;

        SearchField mSearchField;
        PendingChangesTreeView mPendingChangesTreeView;
        MergeLinksListView mMergeLinksListView;
        ErrorsPanel mErrorsPanel;

        readonly SplitterState mErrorsSplitterState;
        CommentArea mCommentArea;
        readonly SplitterState mCommentsSplitterState;

        readonly PendingChangesOperations mPendingChangesOperations;
        readonly ProgressControlsForViews mProgressControls;
        readonly EmptyStatePanel mEmptyStatePanel;
        readonly PendingChangesViewCheckedStateManager mCheckedStateManager;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly bool mIsGluonMode;
        readonly EditorWindow mParentWindow;
        readonly WindowStatusBar mWindowStatusBar;
        readonly INewChangesInWk mNewChangesInWk;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IIncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        readonly IIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
        readonly CheckPendingChanges.IUpdatePendingChanges mUpdatePendingChanges;
        readonly WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly ISaveAssets mSaveAssets;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IHistoryViewLauncher mHistoryViewLauncher;
        readonly IShowShelveInView mShowShelveInView;
        readonly IShowChangesetInView mShowChangesetInView;
        readonly IViewSwitcher mViewSwitcher;
        readonly WorkspaceWindow mWorkspaceWindow;
        readonly ViewHost mViewHost;
        readonly RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;

        const string NOT_EXISTING_WORKSPACE_MESSAGE_ID = "WK_DOESNT_EXIST";

        static readonly ILog mLog = PlasticApp.GetLogger("PendingChangesTab");
    }
}
