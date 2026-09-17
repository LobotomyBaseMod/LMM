using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands;

using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.FsNodeReaders;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.Gluon;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Merge;
using PlasticGui.WorkspaceWindow.QueryViews;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.StatusBar;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Toolbar;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Branches;
using Unity.PlasticSCM.Editor.Views.Changesets;
using Unity.PlasticSCM.Editor.Views.History;
using Unity.PlasticSCM.Editor.Views.IncomingChanges.Gluon;
using Unity.PlasticSCM.Editor.Views.Labels;
using Unity.PlasticSCM.Editor.Views.Locks;
using Unity.PlasticSCM.Editor.Views.Merge.Developer;
using Unity.PlasticSCM.Editor.Views.Merge;
using Unity.PlasticSCM.Editor.Views.PendingChanges;
using Unity.PlasticSCM.Editor.Views.Shelves;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
using GluonCheckIncomingChanges = PlasticGui.Gluon.WorkspaceWindow.CheckIncomingChanges;
using ObjectInfo = Codice.CM.Common.ObjectInfo;

namespace Unity.PlasticSCM.Editor
{
    [Serializable]
    internal class SerializableViewSwitcherState
    {
        internal ViewSwitcher.TabType SelectedTab;
        internal ViewSwitcher.TabType PreviousSelectedTab;

        internal SerializableMergeTabState MergeTabState;
        internal SerializableBranchesTabState BranchesTabState;
        internal SerializableHistoryTabState HistoryTabState;
    }

    internal interface IShowChangesetInView
    {
        void ShowChangesetInView(ChangesetInfo changesetInfo);
    }

    internal interface IShowShelveInView
    {
        void ShowShelveInView(ChangesetInfo shelveInfo);
    }

    internal class ViewSwitcher :
        IViewSwitcher,
        IShowChangesetInView,
        IShowShelveInView,
        IMergeViewLauncher,
        IGluonViewSwitcher,
        IHistoryViewLauncher,
        MergeInProgress.IShowMergeView
    {
        internal enum TabType
        {
            None = 0,
            PendingChanges = 1,
            IncomingChanges = 2,
            Changesets = 3,
            Shelves = 4,
            Branches = 5,
            Labels = 6,
            Locks = 7,
            Merge = 8,
            History = 9,
        }

        internal PendingChangesTab PendingChangesTab { get; private set; }
        internal IIncomingChangesTab IncomingChangesTab { get; private set; }
        internal ChangesetsTab ChangesetsTab { get; private set; }
        internal ShelvesTab ShelvesTab { get; private set; }
        internal BranchesTab BranchesTab { get; private set; }
        internal LabelsTab LabelsTab { get; private set; }
        internal LocksTab LocksTab { get; private set; }
        internal MergeTab MergeTab { get; private set; }
        internal HistoryTab HistoryTab { get; private set; }
        internal SerializableViewSwitcherState State { get { return mState; } }

        internal ViewSwitcher(
            RepositorySpec repSpec,
            WorkspaceInfo wkInfo,
            ViewHost viewHost,
            bool isGluonMode,
            GluonCheckIncomingChanges.IUpdateIncomingChanges gluonUpdateIncomingChanges,
            IAssetStatusCache assetStatusCache,
            ISaveAssets saveAssets,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            WindowStatusBar windowStatusBar,
            EditorWindow parentWindow,
            INewChangesInWk newChangesInWk,
            PendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            CheckPendingChanges.IUpdatePendingChanges updatePendingChanges)
        {
            mRepSpec = repSpec;
            mWkInfo = wkInfo;
            mViewHost = viewHost;
            mIsGluonMode = isGluonMode;
            mGluonUpdateIncomingChanges = gluonUpdateIncomingChanges;
            mAssetStatusCache = assetStatusCache;
            mSaveAssets = saveAssets;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
            mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            mWindowStatusBar = windowStatusBar;
            mParentWindow = parentWindow;
            mNewChangesInWk = newChangesInWk;
            mPendingChangesUpdater = pendingChangesUpdater;
            mDeveloperIncomingChangesUpdater = developerIncomingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mUpdatePendingChanges = updatePendingChanges;

            mSideBarTreeView = new SideBarTreeView(repSpec, isGluonMode, ShowView);
        }

        internal bool IsViewSelected(TabType tab)
        {
            return mState.SelectedTab == tab;
        }

        internal void SetShelvedChanges(
            ShelvedChangesUpdater shelvedChangesUpdater,
            CheckShelvedChanges.IUpdateShelvedChangesNotification updateShelvedChanges)
        {
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mUpdateShelvedChanges = updateShelvedChanges;
        }

        internal void SetWorkspaceWindow(WorkspaceWindow workspaceWindow)
        {
            mWorkspaceWindow = workspaceWindow;
        }

        internal void InitializeFromState(SerializableViewSwitcherState state)
        {
            mState = state;

            if (mState.MergeTabState != null &&
                mState.MergeTabState.IsInitialized)
                BuildMergeViewFromState(mState.MergeTabState);

            if (mState.HistoryTabState != null &&
                mState.HistoryTabState.IsInitialized)
                BuildHistoryViewFromState(mState.HistoryTabState);

            if (mState.BranchesTabState != null &&
                mState.BranchesTabState.IsInitialized)
                BuildBranchesViewFromState(mState.BranchesTabState);

            ShowInitialView(mState.SelectedTab);
        }

        internal void RefreshPendingChangesView(PendingChangesStatus pendingChangesStatus)
        {
            if (PendingChangesTab == null)
                return;

            PendingChangesTab.Refresh(pendingChangesStatus);
        }

        internal void AutoRefreshPendingChangesView()
        {
            AutoRefresh.PendingChangesView(PendingChangesTab);
        }

        internal void AutoRefreshIncomingChangesView()
        {
            AutoRefresh.IncomingChangesView(IncomingChangesTab);
        }

        internal void AutoRefreshMergeView()
        {
            if (mIsGluonMode)
                return;

            AutoRefresh.IncomingChangesView(MergeTab);
        }

        internal void RefreshView(ViewType viewType)
        {
            IRefreshableView view = GetRefreshableView(viewType);

            if (view != null)
            {
                view.Refresh();
                return;
            }

            if (viewType == ViewType.PendingChangesView)
            {
                RefreshAsset.VersionControlCache(mAssetStatusCache);
                return;
            }

            if (viewType == ViewType.LocksView)
            {
                mAssetStatusCache.ClearLocks();
                return;
            }

            if (viewType == ViewType.BranchesListPopup)
                UVCSToolbar.Controller.LoadBranches();
        }

        internal void RefreshWorkingObjectViews(WorkingObjectInfo workingObjectInfo)
        {
            if (BranchesTab != null)
                BranchesTab.SetWorkingObjectInfo(workingObjectInfo.BranchInfo);

            if (ChangesetsTab != null)
                ChangesetsTab.SetWorkingObjectInfo(workingObjectInfo.ChangesetInfo);

            IWorkingObjectRefreshableView refreshable = GetWindowIfOpened.BranchExplorer()?.BranchExplorerView;

            if (refreshable != null)
                refreshable.Refresh(workingObjectInfo);
        }

        internal void OnEnable()
        {
            if (PendingChangesTab != null)
                PendingChangesTab.OnEnable();

            if (IncomingChangesTab != null)
                IncomingChangesTab.OnEnable();

            if (ChangesetsTab != null)
                ChangesetsTab.OnEnable();

            if (ShelvesTab != null)
                ShelvesTab.OnEnable();

            if (BranchesTab != null)
                BranchesTab.OnEnable();

            if (LabelsTab != null)
                LabelsTab.OnEnable();

            if (LocksTab != null)
                LocksTab.OnEnable();

            if (MergeTab != null)
                MergeTab.OnEnable();

            if (HistoryTab != null)
                HistoryTab.OnEnable();
        }

        internal void OnDisable()
        {
            if (PendingChangesTab != null)
            {
                PendingChangesTab.OnDisable();
            }

            if (IncomingChangesTab != null)
            {
                IncomingChangesTab.OnDisable();
            }

            if (ChangesetsTab != null)
            {
                ChangesetsTab.OnDisable();
            }

            if (ShelvesTab != null)
            {
                ShelvesTab.OnDisable();
            }

            if (BranchesTab != null)
            {
                mState.BranchesTabState = BranchesTab.GetSerializableState();
                BranchesTab.OnDisable();
            }

            if (LabelsTab != null)
            {
                LabelsTab.OnDisable();
            }

            if (LocksTab != null)
            {
                LocksTab.OnDisable();
            }

            if (MergeTab != null)
            {
                mState.MergeTabState = MergeTab.GetSerializableState();
                MergeTab.OnDisable();
            }

            if (HistoryTab != null)
            {
                mState.HistoryTabState = HistoryTab.GetSerializableState();
                HistoryTab.OnDisable();
            }
        }

        internal void Update()
        {
            if (IsViewSelected(TabType.PendingChanges))
            {
                PendingChangesTab.Update();
                return;
            }

            if (IsViewSelected(TabType.IncomingChanges))
            {
                IncomingChangesTab.Update();
                return;
            }

            if (IsViewSelected(TabType.Changesets))
            {
                ChangesetsTab.Update();
                return;
            }

            if (IsViewSelected(TabType.Shelves))
            {
                ShelvesTab.Update();
                return;
            }

            if (IsViewSelected(TabType.Branches))
            {
                BranchesTab.Update();
                return;
            }

            if (IsViewSelected(TabType.Labels))
            {
                LabelsTab.Update();
                return;
            }

            if (IsViewSelected(TabType.Locks))
            {
                LocksTab.Update();
                return;
            }

            if (IsViewSelected(TabType.Merge))
            {
                MergeTab.Update();
                return;
            }

            if (IsViewSelected(TabType.History))
            {
                HistoryTab.Update();
                return;
            }
        }

        internal void SidebarButtonsGUI()
        {
            GUILayout.BeginHorizontal();

            Rect rect = GUILayoutUtility.GetRect(
                mSideBarTreeView.GetTotalWidth(),
                0,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            mSideBarTreeView.SetHistoryVisible(HistoryTab != null);
            mSideBarTreeView.SetMergeVisible(MergeTab != null);
            mSideBarTreeView.OnGUI(rect);

            Rect result = GUILayoutUtility.GetRect(
                1,
                rect.height,
                GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(result, UnityStyles.Colors.BarBorder);

            GUILayout.EndHorizontal();
        }

        internal void SelectedViewGUI(ResolvedUser currentUser)
        {
            if (IsViewSelected(TabType.PendingChanges))
            {
                PendingChangesTab.OnGUI(currentUser);
                return;
            }

            if (IsViewSelected(TabType.IncomingChanges))
            {
                IncomingChangesTab.OnGUI();
                return;
            }

            if (IsViewSelected(TabType.Changesets))
            {
                ChangesetsTab.OnGUI();
                return;
            }

            if (IsViewSelected(TabType.Shelves))
            {
                ShelvesTab.OnGUI();
                return;
            }

            if (IsViewSelected(TabType.Branches))
            {
                BranchesTab.OnGUI();
                return;
            }

            if (IsViewSelected(TabType.Labels))
            {
                LabelsTab.OnGUI();
                return;
            }

            if (IsViewSelected(TabType.Locks))
            {
                LocksTab.OnGUI();
                return;
            }

            if (IsViewSelected(TabType.Merge))
            {
                MergeTab.OnGUI(CloseMergeTab);
                return;
            }

            if (IsViewSelected(TabType.History))
            {
                HistoryTab.OnGUI(CloseHistoryTab);
                return;
            }
        }

        internal void ShowPendingChangesView()
        {
            OpenPendingChangesTab();

            PendingChangesTab.AutoRefresh();

            SetSelectedView(TabType.PendingChanges);
        }

        internal void ShowIncomingChangesView()
        {
            if (IncomingChangesTab == null)
            {
                IncomingChangesTab = BuildIncomingChangesTab(mIsGluonMode);

                mViewHost.AddRefreshableView(
                    ViewType.IncomingChangesView,
                    (IRefreshableView)IncomingChangesTab);
            }

            IncomingChangesTab.AutoRefresh();

            SetSelectedView(TabType.IncomingChanges);
        }

        internal void ShowChangesetsView(ChangesetInfo changesetToSelect = null)
        {
            if (ChangesetsTab == null)
            {
                OpenPendingChangesTab();

                ChangesetsTab = new ChangesetsTab(
                    mWkInfo,
                    changesetToSelect,
                    mViewHost,
                    mWorkspaceWindow,
                    this,
                    this,
                    this,
                    mWorkspaceWindow,
                    mWorkspaceWindow,
                    mShelvedChangesUpdater,
                    PendingChangesTab,
                    mAssetStatusCache,
                    mSaveAssets,
                    mShowDownloadPlasticExeWindow,
                    mProcessExecutor,
                    mWorkspaceOperationsMonitor,
                    mPendingChangesUpdater,
                    mDeveloperIncomingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    mParentWindow,
                    mIsGluonMode);

                mViewHost.AddRefreshableView(ViewType.ChangesetsView, ChangesetsTab);
            }
            else
            {
                if (changesetToSelect != null)
                    ChangesetsTab.RefreshAndSelect(changesetToSelect);
            }

            SetSelectedView(TabType.Changesets);
        }

        internal void ShowShelvesView(ChangesetInfo shelveToSelect = null)
        {
            if (ShelvesTab == null)
            {
                OpenPendingChangesTab();

                ShelvesTab = new ShelvesTab(
                    mWkInfo,
                    mRepSpec,
                    shelveToSelect,
                    mWorkspaceWindow,
                    this,
                    this,
                    this,
                    PendingChangesTab,
                    mIsGluonMode ?
                        mWorkspaceWindow.GluonProgressOperationHandler :
                        mWorkspaceWindow.DeveloperProgressOperationHandler,
                    mWorkspaceWindow.GluonProgressOperationHandler,
                    mShelvedChangesUpdater,
                    mSaveAssets,
                    mShowDownloadPlasticExeWindow,
                    mProcessExecutor,
                    mWorkspaceOperationsMonitor,
                    mPendingChangesUpdater,
                    mDeveloperIncomingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    mParentWindow,
                    mIsGluonMode);

                mViewHost.AddRefreshableView(ViewType.ShelvesView, ShelvesTab);

                TrackFeatureUseEvent.For(
                    mRepSpec, TrackFeatureUseEvent.Features.OpenShelvesView);
            }
            else
            {
                if (shelveToSelect != null)
                    ShelvesTab.RefreshAndSelect(shelveToSelect);
            }

            SetSelectedView(TabType.Shelves);
        }

        internal void ShowBranchesView()
        {
            if (BranchesTab == null)
            {
                BranchesTab = BuildBranchesTab(false);

                mViewHost.AddRefreshableView(ViewType.BranchesView, BranchesTab);

                TrackFeatureUseEvent.For(
                    mRepSpec, TrackFeatureUseEvent.Features.UnityPackage.OpenBranchesView);
            }

            SetSelectedView(TabType.Branches);
        }

        internal void ShowLabelsView()
        {
            if (LabelsTab == null)
            {
                LabelsTab = new LabelsTab(
                    mWkInfo,
                    mWorkspaceWindow,
                    this,
                    this,
                    mViewHost,
                    mWorkspaceWindow,
                    mWorkspaceWindow,
                    mPendingChangesUpdater,
                    mDeveloperIncomingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    mShelvedChangesUpdater,
                    mAssetStatusCache,
                    mShowDownloadPlasticExeWindow,
                    mProcessExecutor,
                    mParentWindow,
                    mIsGluonMode);

                mViewHost.AddRefreshableView(ViewType.LabelsView, LabelsTab);

                TrackFeatureUseEvent.For(
                    mRepSpec, TrackFeatureUseEvent.Features.UnityPackage.OpenLabelsView);
            }

            SetSelectedView(TabType.Labels);
        }

        internal void ShowLocksView()
        {
            if (LocksTab == null)
            {
                LocksTab = new LocksTab(
                    mWkInfo,
                    mRepSpec,
                    mWorkspaceWindow,
                    mAssetStatusCache,
                    mParentWindow);

                mViewHost.AddRefreshableView(ViewType.LocksView, LocksTab);

                TrackFeatureUseEvent.For(
                    mRepSpec, TrackFeatureUseEvent.Features.OpenLocksView);
            }

            SetSelectedView(TabType.Locks);
        }

        internal void ShowHistoryView(
            RepositorySpec repSpec,
            long itemId,
            string path,
            bool isDirectory)
        {
            if (HistoryTab == null)
            {
                HistoryTab = BuildHistoryTab(
                    repSpec, itemId, path, isDirectory);

                mViewHost.AddRefreshableView(
                    ViewType.HistoryView, HistoryTab);
            }
            else
            {
                HistoryTab.RefreshForItem(repSpec, itemId, path, isDirectory);
            }

            SetSelectedView(TabType.History);
        }

        internal void ShowBranchesViewForTesting(BranchesTab branchesTab)
        {
            BranchesTab = branchesTab;

            ShowBranchesView();
        }

        internal void ShowMergeViewForTesting(MergeTab mergeTab)
        {
            MergeTab = mergeTab;

            ShowMergeView();
        }

        internal void ShowShelvesViewForTesting(ShelvesTab shelvesTab)
        {
            ShelvesTab = shelvesTab;

            ShowShelvesView();
        }

        void IViewSwitcher.ShowView(ViewType viewType)
        {
        }

        void IViewSwitcher.ShowPendingChanges()
        {
            ShowPendingChangesView();
            mParentWindow.Repaint();
        }

        void IViewSwitcher.ShowShelvesView()
        {
            ShowShelvesView();
        }

        void IViewSwitcher.ShowSyncView(string syncViewToSelect)
        {
            throw new NotImplementedException();
        }

        void IViewSwitcher.ShowBranchExplorerView()
        {
            //TODO: Codice
            //launch plastic with branch explorer view option
        }

        void IViewSwitcher.DisableMergeView()
        {
            DisableMergeTab();
        }

        IMergeView IViewSwitcher.GetMergeView()
        {
            return MergeTab;
        }

        bool IViewSwitcher.IsIncomingChangesView()
        {
            return IsViewSelected(TabType.IncomingChanges);
        }

        void IViewSwitcher.CloseMergeView()
        {
            CloseMergeTab();
        }

        void IShowChangesetInView.ShowChangesetInView(ChangesetInfo changesetInfo)
        {
            ShowChangesetsView(changesetInfo);
        }

        void IShowShelveInView.ShowShelveInView(ChangesetInfo shelveInfo)
        {
            ShowShelvesView(shelveInfo);
        }

        IMergeView IMergeViewLauncher.MergeFrom(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            bool showDiscardChangesButton)
        {
            return ((IMergeViewLauncher)this).MergeFromInterval(
                repSpec, objectInfo, null, mergeType, showDiscardChangesButton);
        }

        IMergeView IMergeViewLauncher.MergeFrom(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            ShowIncomingChangesFrom from,
            bool showDiscardChangesButton)
        {
            return MergeFromInterval(repSpec, objectInfo, null, mergeType, from, showDiscardChangesButton);
        }

        IMergeView IMergeViewLauncher.MergeFromInterval(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            ObjectInfo ancestorChangesetInfo,
            EnumMergeType mergeType,
            bool showDiscardChangesButton)
        {
            return MergeFromInterval(
                repSpec, objectInfo, null, mergeType, ShowIncomingChangesFrom.NotificationBar, showDiscardChangesButton);
        }

        IMergeView IMergeViewLauncher.FromCalculatedMerge(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            CalculatedMergeResult calculatedMergeResult,
            bool showDiscardChangesButton)
        {
            return ShowMergeViewFromCalculatedMerge(
                repSpec, objectInfo, mergeType, calculatedMergeResult, showDiscardChangesButton);
        }

        void IGluonViewSwitcher.ShowIncomingChangesView()
        {
            ShowIncomingChangesView();

            mParentWindow.Repaint();
        }

        void IHistoryViewLauncher.ShowHistoryView(
            RepositorySpec repSpec,
            long itemId,
            string path,
            bool isDirectory)
        {
            ShowHistoryView(repSpec, itemId, path, isDirectory);

            mParentWindow.Repaint();
        }

        void MergeInProgress.IShowMergeView.MergeLinkNotFound()
        {
            // Nothing to do on the plugin when there is no pending merge link
        }

        void MergeInProgress.IShowMergeView.ForPendingMergeLink(
            RepositorySpec repSpec,
            MergeType pendingLinkMergeType,
            ChangesetInfo srcChangeset,
            ChangesetInfo baseChangeset)
        {
            EnumMergeType mergeType = MergeTypeConverter.TranslateMergeType(pendingLinkMergeType);

            MergeTab = BuildMergeTab(
                repSpec,
                srcChangeset,
                baseChangeset,
                mergeType,
                ShowIncomingChangesFrom.None,
                MergeTypeClassifier.IsIncomingMerge(mergeType),
                false,
                false);

            mViewHost.AddRefreshableView(ViewType.MergeView, MergeTab);

            ShowMergeView();
        }

        void ShowInitialView(TabType viewToShow)
        {
            mState.SelectedTab = TabType.None;

            ShowView(viewToShow);

            if (mState.SelectedTab != TabType.None)
                return;

            ShowPendingChangesView();
        }

        void BuildBranchesViewFromState(SerializableBranchesTabState state)
        {
            BranchesTab = BuildBranchesTab(
                state.ShowHiddenBranches);

            mViewHost.AddRefreshableView(ViewType.BranchesView, BranchesTab);
        }

        void BuildHistoryViewFromState(SerializableHistoryTabState state)
        {
            HistoryTab = BuildHistoryTab(
                state.RepSpec,
                state.ItemId,
                state.Path,
                state.IsDirectory);

            mViewHost.AddRefreshableView(ViewType.HistoryView, HistoryTab);
        }

        void BuildMergeViewFromState(SerializableMergeTabState state)
        {
            MergeTab = BuildMergeTab(
                state.RepSpec,
                state.GetObjectInfo(),
                state.GetAncestorObjectInfo(),
                state.MergeType,
                state.From,
                state.IsIncomingMerge,
                state.IsMergeFinished,
                false);

            mViewHost.AddRefreshableView(ViewType.MergeView, MergeTab);
        }

        void OpenPendingChangesTab()
        {
            if (PendingChangesTab != null)
                return;

            PendingChangesTab = new PendingChangesTab(
                mWkInfo,
                mRepSpec,
                mViewHost,
                mWorkspaceWindow,
                this,
                this,
                this,
                this,
                this,
                mAssetStatusCache,
                mSaveAssets,
                mShowDownloadPlasticExeWindow,
                mWorkspaceOperationsMonitor,
                mNewChangesInWk,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mGluonIncomingChangesUpdater,
                mShelvedChangesUpdater,
                mUpdatePendingChanges,
                mWindowStatusBar,
                mParentWindow,
                mIsGluonMode);

            mViewHost.AddRefreshableView(ViewType.CheckinView, PendingChangesTab);
        }

        IMergeView MergeFromInterval(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            ObjectInfo ancestorChangesetInfo,
            EnumMergeType mergeType,
            ShowIncomingChangesFrom from,
            bool showDiscardChangesButton)
        {
            if (MergeTypeClassifier.IsIncomingMerge(mergeType))
            {
                ShowIncomingChangesView();
                mParentWindow.Repaint();
                return IncomingChangesTab as IMergeView;
            }

            ShowMergeViewFromInterval(
                repSpec, objectInfo, ancestorChangesetInfo, mergeType, from, showDiscardChangesButton);
            mParentWindow.Repaint();
            return MergeTab;
        }

        void ShowHistoryView()
        {
            SetSelectedView(TabType.History);
        }

        void ShowMergeViewFromInterval(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            ObjectInfo ancestorChangesetInfo,
            EnumMergeType mergeType,
            ShowIncomingChangesFrom from,
            bool showDiscardChangesButton)
        {
            if (MergeTab != null && MergeTab.IsProcessingMerge)
            {
                ShowMergeView();
                return;
            }

            if (MergeTab != null)
            {
                mViewHost.RemoveRefreshableView(ViewType.MergeView, MergeTab);
                MergeTab.OnDisable();
            }

            MergeTab = BuildMergeTab(
                repSpec,
                objectInfo,
                ancestorChangesetInfo,
                mergeType,
                from,
                false,
                false,
                showDiscardChangesButton);

            mViewHost.AddRefreshableView(ViewType.MergeView, MergeTab);

            ShowMergeView();
        }

        IMergeView ShowMergeViewFromCalculatedMerge(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            CalculatedMergeResult calculatedMergeResult,
            bool showDiscardChangesButton)
        {
            if (MergeTab != null && MergeTab.IsProcessingMerge)
            {
                ShowMergeView();
                mParentWindow.Repaint();
                return MergeTab;
            }

            if (MergeTab != null)
            {
                mViewHost.RemoveRefreshableView(ViewType.MergeView, MergeTab);
                MergeTab.OnDisable();
            }

            MergeTab = BuildMergeTabFromCalculatedMerge(
                repSpec, objectInfo, mergeType, calculatedMergeResult, showDiscardChangesButton);

            mViewHost.AddRefreshableView(ViewType.MergeView, MergeTab);

            ShowMergeView();
            mParentWindow.Repaint();
            return MergeTab;
        }

        void ShowMergeView()
        {
            if (MergeTab == null)
                return;

            MergeTab.AutoRefresh();

            SetSelectedView(TabType.Merge);
        }

        void DisableMergeTab()
        {
            if (MergeTab == null)
                return;

            mViewHost.RemoveRefreshableView(
                ViewType.MergeView, MergeTab);

            MergeTab.OnDisable();
            MergeTab = null;

            mState.MergeTabState = null;
        }

        void CloseMergeTab()
        {
            DisableMergeTab();

            ShowPreviousViewFrom(TabType.Merge);

            mParentWindow.Repaint();
        }

        void CloseHistoryTab()
        {
            mViewHost.RemoveRefreshableView(
                ViewType.HistoryView, HistoryTab);

            HistoryTab.OnDisable();
            HistoryTab = null;

            mState.HistoryTabState = null;

            ShowPreviousViewFrom(TabType.History);

            mParentWindow.Repaint();
        }

        IIncomingChangesTab BuildIncomingChangesTab(bool isGluonMode)
        {
            if (isGluonMode)
            {
                return new IncomingChangesTab(
                    mWkInfo,
                    mViewHost,
                    mWorkspaceWindow,
                    mGluonUpdateIncomingChanges,
                    mAssetStatusCache,
                    mShowDownloadPlasticExeWindow,
                    mPendingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    mWindowStatusBar,
                    mParentWindow);
            }

            PlasticNotifier plasticNotifier = new PlasticNotifier();

            MergeViewLogic.IMergeController mergeController = new MergeController(
                mWkInfo,
                mRepSpec,
                null,
                null,
                EnumMergeType.IncomingMerge,
                true,
                plasticNotifier);

            return MergeTab.Build(
                mWkInfo,
                mRepSpec,
                null,
                null,
                EnumMergeType.IncomingMerge,
                ShowIncomingChangesFrom.NotificationBar,
                mWorkspaceWindow,
                this,
                this,
                mergeController,
                new MergeViewLogic.GetWorkingBranch(),
                mUpdateShelvedChanges,
                mWorkspaceWindow,
                mAssetStatusCache,
                mShowDownloadPlasticExeWindow,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mShelvedChangesUpdater,
                plasticNotifier,
                mWindowStatusBar,
                mParentWindow,
                true,
                false,
                false);
        }

        HistoryTab BuildHistoryTab(
            RepositorySpec repSpec,
            long itemId,
            string path,
            bool isDirectory)
        {
            HistoryTab result = new HistoryTab(
                mWkInfo,
                mViewHost,
                mWorkspaceWindow,
                mAssetStatusCache,
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mGluonIncomingChangesUpdater,
                mParentWindow,
                mIsGluonMode);

            result.RefreshForItem(repSpec, itemId, path, isDirectory);

            return result;
        }

        BranchesTab BuildBranchesTab(bool showHiddenBranches)
        {
            BranchesTab result = new BranchesTab(
                mWkInfo,
                mViewHost,
                mWorkspaceWindow,
                this,
                this,
                mWorkspaceWindow,
                mWorkspaceWindow,
                mShelvedChangesUpdater,
                mAssetStatusCache,
                mSaveAssets,
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                mWorkspaceOperationsMonitor,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mGluonIncomingChangesUpdater,
                mParentWindow,
                mIsGluonMode,
                showHiddenBranches);

            return result;
        }

        MergeTab BuildMergeTabFromCalculatedMerge(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            CalculatedMergeResult calculatedMergeResult,
            bool showDiscardChangesButton)
        {
            return BuildMergeTab(
                repSpec,
                objectInfo,
                null,
                mergeType,
                ShowIncomingChangesFrom.None,
                false,
                false,
                showDiscardChangesButton,
                calculatedMergeResult);
        }

        MergeTab BuildMergeTab(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            ObjectInfo ancestorObjectInfo,
            EnumMergeType mergeType,
            ShowIncomingChangesFrom from,
            bool isIncomingMerge,
            bool isMergeFinished,
            bool showDiscardChangesButton,
            CalculatedMergeResult calculatedMergeResult = null)
        {
            PlasticNotifier plasticNotifier = new PlasticNotifier();

            MergeViewLogic.IMergeController mergeController = new MergeController(
                mWkInfo,
                repSpec,
                objectInfo,
                ancestorObjectInfo,
                mergeType,
                false,
                plasticNotifier);

            if (calculatedMergeResult != null)
            {
                return MergeTab.BuildFromCalculatedMerge(
                    mWkInfo,
                    repSpec,
                    objectInfo,
                    ancestorObjectInfo,
                    mergeType,
                    from,
                    mWorkspaceWindow,
                    this,
                    this,
                    mergeController,
                    new MergeViewLogic.GetWorkingBranch(),
                    mUpdateShelvedChanges,
                    mWorkspaceWindow,
                    mAssetStatusCache,
                    mShowDownloadPlasticExeWindow,
                    mPendingChangesUpdater,
                    mDeveloperIncomingChangesUpdater,
                    mShelvedChangesUpdater,
                    plasticNotifier,
                    mWindowStatusBar,
                    mParentWindow,
                    calculatedMergeResult,
                    isIncomingMerge,
                    isMergeFinished,
                    showDiscardChangesButton);
            }

            return MergeTab.Build(
                mWkInfo,
                repSpec,
                objectInfo,
                ancestorObjectInfo,
                mergeType,
                from,
                mWorkspaceWindow,
                this,
                this,
                mergeController,
                new MergeViewLogic.GetWorkingBranch(),
                mUpdateShelvedChanges,
                mWorkspaceWindow,
                mAssetStatusCache,
                mShowDownloadPlasticExeWindow,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mShelvedChangesUpdater,
                plasticNotifier,
                mWindowStatusBar,
                mParentWindow,
                isIncomingMerge,
                isMergeFinished,
                showDiscardChangesButton);
        }

        void ShowView(TabType viewToShow)
        {
            switch (viewToShow)
            {
                case TabType.PendingChanges:
                    ShowPendingChangesView();
                    break;

                case TabType.IncomingChanges:
                    ShowIncomingChangesView();
                    break;

                case TabType.Changesets:
                    ShowChangesetsView();
                    break;

                case TabType.Branches:
                    ShowBranchesView();
                    break;

                case TabType.Shelves:
                    ShowShelvesView();
                    break;

                case TabType.Locks:
                    ShowLocksView();
                    break;

                case TabType.Merge:
                    ShowMergeView();
                    break;

                case TabType.History:
                    ShowHistoryView();
                    break;

                case TabType.Labels:
                    ShowLabelsView();
                    break;
            }
        }

        void ShowPreviousViewFrom(TabType tabToClose)
        {
            if (!IsViewSelected(tabToClose))
                return;

            if (GetRefreshableViewBasedOnSelectedTab(mState.PreviousSelectedTab) == null)
                mState.PreviousSelectedTab = TabType.PendingChanges;

            ShowView(mState.PreviousSelectedTab);
        }

        IRefreshableView GetRefreshableViewBasedOnSelectedTab(TabType selectedTab)
        {
            switch (selectedTab)
            {
                case TabType.PendingChanges:
                    return PendingChangesTab;

                case TabType.IncomingChanges:
                    return (IRefreshableView)IncomingChangesTab;

                case TabType.Changesets:
                    return ChangesetsTab;

                case TabType.Shelves:
                    return ShelvesTab;

                case TabType.Branches:
                    return BranchesTab;

                case TabType.Labels:
                    return LabelsTab;

                case TabType.Locks:
                    return LocksTab;

                case TabType.Merge:
                    return MergeTab;

                case TabType.History:
                    return HistoryTab;

                default:
                    return null;
            }
        }

        IRefreshableView GetRefreshableView(ViewType viewType)
        {
            switch (viewType)
            {
                case ViewType.PendingChangesView:
                    return PendingChangesTab;

                case ViewType.IncomingChangesView:
                    return (IRefreshableView)IncomingChangesTab;

                case ViewType.ChangesetsView:
                    return ChangesetsTab;

                case ViewType.ShelvesView:
                    return ShelvesTab;

                case ViewType.BranchesView:
                    return BranchesTab;

                case ViewType.LabelsView:
                    return LabelsTab;

                case ViewType.LocksView:
                    return LocksTab;

                case ViewType.MergeView:
                    return MergeTab;

                case ViewType.HistoryView:
                    return HistoryTab;

                case ViewType.BranchExplorerView:
                    return GetWindowIfOpened.BranchExplorer()?.BranchExplorerView;

                default:
                    return null;
            }
        }

        void SetSelectedView(TabType tab)
        {
            mSideBarTreeView.SetSelectedItem(tab);

            if (mState.SelectedTab != tab && mState.SelectedTab != TabType.None)
                mState.PreviousSelectedTab = mState.SelectedTab;

            mState.SelectedTab = tab;

            if (PendingChangesTab != null)
                PendingChangesTab.IsVisible = tab == TabType.PendingChanges;

            if (IncomingChangesTab != null)
                IncomingChangesTab.IsVisible = tab == TabType.IncomingChanges;

            if (ChangesetsTab != null)
                ChangesetsTab.IsVisible = tab == TabType.Changesets;

            if (ShelvesTab != null)
                ShelvesTab.IsVisible = tab == TabType.Shelves;

            if (BranchesTab != null)
                BranchesTab.IsVisible = tab == TabType.Branches;

            if (LabelsTab != null)
                LabelsTab.IsVisible = tab == TabType.Labels;
        }

        SerializableViewSwitcherState mState;

        CheckShelvedChanges.IUpdateShelvedChangesNotification mUpdateShelvedChanges;
        ShelvedChangesUpdater mShelvedChangesUpdater;
        WorkspaceWindow mWorkspaceWindow;
        readonly EditorWindow mParentWindow;
        readonly INewChangesInWk mNewChangesInWk;
        readonly PendingChangesUpdater mPendingChangesUpdater;
        readonly IncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        readonly GluonIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly CheckPendingChanges.IUpdatePendingChanges mUpdatePendingChanges;
        readonly WindowStatusBar mWindowStatusBar;
        readonly WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        readonly LaunchTool.IProcessExecutor mProcessExecutor;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly ISaveAssets mSaveAssets;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly SideBarTreeView mSideBarTreeView;
        readonly GluonCheckIncomingChanges.IUpdateIncomingChanges mGluonUpdateIncomingChanges;
        readonly bool mIsGluonMode;
        readonly ViewHost mViewHost;
        readonly WorkspaceInfo mWkInfo;
        readonly RepositorySpec mRepSpec;
    }
}
