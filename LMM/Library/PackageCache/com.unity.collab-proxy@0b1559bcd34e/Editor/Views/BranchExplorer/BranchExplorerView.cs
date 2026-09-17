using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

using Codice.Client.BaseCommands;
using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.ExplorerTree;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.Client.BaseCommands.Config;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Threading;
using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.BranchExplorer.Search;
using PlasticGui.WorkspaceWindow.Configuration;
using PlasticGui.WorkspaceWindow.Filters;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Headless;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Options;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using Unity.PlasticSCM.Editor.Views.Filters;
using LayoutFilters = Codice.Client.BaseCommands.LayoutFilters;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal class BranchExplorerView :
        VisualElement,
        IRefreshableView,
        IQueryRefreshableView,
        IWorkingObjectRefreshableView,
        IFocusedObjectObserver,
        IVirtualCanvasUpdateVisualsListener,
        IVirtualCanvasUpdateListener,
        FiltersPanel.IFilterableView,
        BranchExplorerOptionsWindow.IBranchExplorerView
    {
        internal BranchExplorerViewer BranchExplorerViewer { get { return mBranchExplorerViewer; } }

        internal BranchExplorerView(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            IPlasticWebRestApi restApi,
            LaunchTool.IProcessExecutor processExecutor,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            EditorWindow window)
        {
            mWkInfo = wkInfo;
            mRepSpec = repSpec;

            CreateGUI(repSpec, restApi, processExecutor, showDownloadPlasticExeWindow, window);

            mBranchExplorerViewer.InitializeBranchExplorerViewMenu(
                restApi,
                mWkInfo,
                window,
                new HeadlessWorkspaceWindow(() => { }, SetWorkingObjectInfo),
                new HeadlessViewSwitcher(),
                new HeadlessMergeViewLauncher(UVCSPlugin.Instance),
                this,
                mSelectedObjectResolver,
                mProgressControls,
                null, //guiHelpEvents,
                null, //mOpenedCodeReviewWindows,
                UVCSPlugin.Instance.AssetStatusCache,
                UVCSPlugin.Instance.PendingChangesUpdater,
                PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo) ?
                    UVCSPlugin.Instance.GluonIncomingChangesUpdater :
                    UVCSPlugin.Instance.DeveloperIncomingChangesUpdater,
                GetShelvedChangesUpdater(),
                null,
                processExecutor,
                showDownloadPlasticExeWindow);

            ((IRefreshableView)this).Refresh();
        }

        internal void Dispose()
        {
            if (mFocusListener != null)
                mFocusListener.RemoveFocusedObjectObserver();

            mBranchExplorerViewer.Dispose();
            mEmptyStatePanel.Dispose();

            UnregisterCallback<KeyDownEvent>(OnKeyDown);

            mSearchField.DelayedSearchTextChanged -= OnDelayedSearchChanged;
            mSearchField.NextSearchResultRequested -= OnNextSearchrResultRequested;
            mSearchField.PreviousSearchResultRequested -= OnPreviousSearchrResultRequested;
            mSearchField.Dispose();
        }

        internal void RecalculateLayout()
        {
            mProgressControls.ShowProgress(
                PlasticLocalization.GetString(PlasticLocalization.Name.CalculatingViewLayout),
                TimeSpan.FromMilliseconds(1000));

            mBranchExplorerViewer.SaveRelativePosition();

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    mBranchExplorerViewer.RecalculateLayout(mRepSpec);
                },
                afterOperationDelegate: delegate
                {
                    ((IProgressControls)mProgressControls).HideProgress();

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    mBranchExplorerViewer.UpdateLayout();

                    RefreshSearchResultsIfNeeded();
                    UpdateEmptyStateVisibility();
                });
        }

        void IVirtualCanvasUpdateVisualsListener.OnVisualsUpdated()
        {
            try
            {
                if (mIsPendingToApplySearch)
                {
                    ApplySearch();
                    return;
                }

                if (mIsFirstLoad)
                {
                    mBranchExplorerViewer.NavigateToHome(false, false);
                    return;
                }

                mBranchExplorerViewer.RestoreRelativePosition();
            }
            finally
            {
                mIsFirstLoad = false;
            }
        }

        void IFocusedObjectObserver.DelayedFocusedObjectChanged(ObjectDrawInfo focusedObject)
        {
            schedule.Execute(() =>
            {
                TrackDelayedFocusedObjectChangedEvent(focusedObject, mRepSpec);
            });

            mSelectedObjectDataUpdater.UpdateDisplayData(focusedObject);
        }

        static void TrackDelayedFocusedObjectChangedEvent(ObjectDrawInfo focusedObject, RepositorySpec repSpec)
        {
            if (focusedObject is ChangesetDrawInfo)
            {
                TrackFeatureUseEvent.For(
                    repSpec,
                    TrackFeatureUseEvent.Features.BranchExplorer.SelectChangeset);
                return;
            }

            if (focusedObject is BranchDrawInfo)
            {
                TrackFeatureUseEvent.For(
                    repSpec,
                    TrackFeatureUseEvent.Features.BranchExplorer.SelectBranch);
                return;
            }

            if (focusedObject is LabelDrawInfo)
            {
                TrackFeatureUseEvent.For(
                    repSpec,
                    TrackFeatureUseEvent.Features.BranchExplorer.SelectLabel);
                return;
            }
        }

        void IVirtualCanvasUpdateListener.OnDelayedCanvasUpdate()
        {
            // pending to check if it is needed
        }

        void FiltersPanel.IFilterableView.ApplyFilter()
        {
            RecalculateLayout();
        }

        bool FiltersPanel.IFilterableView.ApplyDateFilter(
            LayoutFilters.DateFilter oldFilter, LayoutFilters.DateFilter newFilter)
        {
            ((IRefreshableView)this).Refresh();
            return true;
        }

        void IRefreshableView.Refresh()
        {
            ((IQueryRefreshableView)this).RefreshAndSelect(null);
        }

        void IWorkingObjectRefreshableView.Refresh(WorkingObjectInfo workingObjectInfo)
        {
            SetWorkingObjectInfo(workingObjectInfo);
        }

        void BranchExplorerOptionsWindow.IBranchExplorerView.Redraw()
        {
            mBranchExplorerViewer.UpdateColorConfig();
            mBranchExplorerViewer.Redraw();
        }

        void BranchExplorerOptionsWindow.IBranchExplorerView.Refresh()
        {
            ((IRefreshableView)this).Refresh();
        }

        void BranchExplorerOptionsWindow.IBranchExplorerView.ClearSearchResults()
        {
            mPreviousSearch = null;
            mBranchExplorerViewer.Search.ClearSearchResults();
        }

        void IQueryRefreshableView.RefreshAndSelect(RepObjectInfo objectToSelect)
        {
            if (mIsUpdating)
                return;

            mIsUpdating = true;

            CheckRepositorySpecChanged();

            RefreshIncomingChangesUpdater();
            RefreshShelvedChangesUpdater();

            ((IProgressControls)mProgressControls).ShowProgress(PlasticLocalization.GetString(
                PlasticLocalization.Name.LoadingBranchExplorer));

            mBranchExplorerViewer.SaveRelativePosition();

            /*List<ResolvedUser> availableUsers = new List<ResolvedUser>();*/

            BrExLayout brExLayout = null;
            BrExTree brExTree = null;

            WorkingObjectInfo workingObjectInfo = null;
            bool bInitializeConfig = false;
            //ResolvedUser currentUser = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    if (mConfig == null)
                    {
                        mConfig = WorkspaceUIConfiguration.Get(mWkInfo);

                        mBranchExplorerViewer.SetWorkspaceUIConfiguration(mConfig);
                        mFiltersPanel.SetWorkspaceUIConfiguration(mConfig);

                        bInitializeConfig = true;
                    }

                    workingObjectInfo = CalculateWorkingObjectInfo(mWkInfo);
                    //UpdateWorkingBranchInBranchFilter(workingObjectInfo);

                    brExLayout = CalculateLayout(
                        mWkInfo,
                        mRepSpec,
                        mConfig,
                        workingObjectInfo,
                        out brExTree);

                    mBranchExplorerViewer.SetLayout(brExLayout, brExTree);

                    /*currentUser = PlasticGui.Plastic.API.GetCurrentUser(mRepSpec.Server);

                    UpdateFilterBranchNames(mConfig, brExTree.GetBranches());
                    UpdateFilterUserNames(mConfig, availableUsers);*/
                },
                afterOperationDelegate: delegate
                {
                    try
                    {
                        mBranchExplorerViewer.SetColorConfig(
                            mWkInfo,
                            mRepSpec);

                        if (bInitializeConfig)
                        {
                            mBranchExplorerViewer.Zoom.InitializeZoomLevel();
                        }

                        if (waiter.Exception != null)
                        {
                            ExceptionsHandler.DisplayException(waiter.Exception);
                            return;
                        }

                        mBranchExplorerViewer.UpdateLayout();

                        RefreshSearchResultsIfNeeded();
                        UpdateEmptyStateVisibility();

                        mBranchExplorerViewer.SelectObject(objectToSelect);

                        /*mFiltersPanel.SetData(
                            UserFilterModel.ToModel(availableUsers),
                            UserFilterModel.ToModel(currentUser),
                            BranchFilterModel.ToModel(brExTree.GetBranches(), workingObjectInfo.BranchInfo),
                            BranchFilterModel.ToModel(workingObjectInfo.BranchInfo));*/
                        mFiltersPanel.LoadConfiguration();
                    }
                    finally
                    {
                        ((IProgressControls)mProgressControls).HideProgress();
                        mIsUpdating = false;
                    }
                });
        }

        void SetWorkingObjectInfo(WorkingObjectInfo workingObjectInfo)
        {
            if (mIsUpdating)
                return;

            if (IsHomeChangesetVisible(workingObjectInfo.GetChangesetId()))
            {
                SetWorkingObjectInfoAndRecalculateLayout(workingObjectInfo);
                return;
            }

            ((IRefreshableView)this).Refresh();
        }

        bool IsHomeChangesetVisible(long homeChangesetId)
        {
            return FindDrawInfo.GetChangeset(
                mBranchExplorerViewer?.ExplorerLayout?.ChangesetDraws,
                homeChangesetId) != null;
        }

        void SetWorkingObjectInfoAndRecalculateLayout(WorkingObjectInfo workingObjectInfo)
        {
            if (mIsUpdating)
                return;

            mIsUpdating = true;

            mProgressControls.ShowProgress(
                PlasticLocalization.GetString(PlasticLocalization.Name.CalculatingViewLayout),
                TimeSpan.FromMilliseconds(1000));

            CheckRepositorySpecChanged();

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    Guid currentObject;
                    mBranchExplorerViewer.ExplorerTree.SetCurrentBranchAndChangeset(
                        workingObjectInfo.GetChangesetId(),
                        workingObjectInfo.BranchInfo != null ? workingObjectInfo.BranchInfo.Name : string.Empty,
                        out currentObject);

                    //UpdateWorkingBranchInBranchFilter(workingObjectInfo);

                    mBranchExplorerViewer.ExplorerTree.CalculateRelevantChangesets();

                    BrExLayout brExLayout = null;
                    BrExTree brExTree = null;

                    brExLayout = CalculateLayout(
                        mWkInfo,
                        mRepSpec,
                        mConfig,
                        workingObjectInfo,
                        out brExTree);

                    mBranchExplorerViewer.SetLayout(brExLayout, brExTree);
                },
                afterOperationDelegate: delegate
                {
                    ((IProgressControls)mProgressControls).HideProgress();

                    mIsUpdating = false;

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    /*mFiltersPanel.BranchFilterButtonPanel.FilterPopup.UpdateCurrentModel(
                        BranchFilterModel.ToModel(workingObjectInfo.BranchInfo));*/

                    mBranchExplorerViewer.UpdateLayout();
                });
        }

        static BrExLayout CalculateLayout(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            WorkspaceUIConfiguration config,
            WorkingObjectInfo workingObjectInfo,
            out BrExTree brExTree)
        {
            return PlasticGui.Plastic.API.GetBranchExplorerLayout(
                wkInfo,
                repSpec,
                GetFilters(wkInfo, repSpec, config.Rules, null),
                config.DisplayOptions,
                workingObjectInfo,
                out brExTree);
        }

        static FilterCollection GetFilters(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            List<Rule> rulesConfig,
            CustomFilterData customFilterData)
        {
            if (customFilterData == null)
                return ObtainFilters.FromRules(wkInfo, repSpec, rulesConfig);
            return CustomBranchExplorer.BuildCustomSelectedBranchesFilter(customFilterData);
        }

        static WorkingObjectInfo CalculateWorkingObjectInfo(WorkspaceInfo wkInfo)
        {
            if (wkInfo.IsTemporary)
                return WorkingObjectInfo.Empty;

            return WorkingObjectInfo.Calculate(wkInfo);
        }

        void UpdateEmptyStateVisibility()
        {
            if (mBranchExplorerViewer.ExplorerLayout == null)
                return;

            if (mBranchExplorerViewer.ExplorerLayout.ColumnDraws.Count == 0)
            {
                mBranchExplorerViewer.visible = false;
                mEmptyStatePanel.visible = true;
                return;
            }

            mBranchExplorerViewer.visible = true;
            mEmptyStatePanel.visible = false;
        }

        void CheckRepositorySpecChanged()
        {
            RepositorySpec repSpec = PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo);
            if (repSpec == null || repSpec.Equals(mRepSpec))
                return;

            mRepSpec = repSpec;
            UpdateRepositorySpecInViews(mRepSpec);
        }

        void UpdateRepositorySpecInViews(RepositorySpec repSpec)
        {
            mSelectedObjectResolver.UpdateRepositorySpec(repSpec);
            mSelectedObjectDataUpdater.UpdateRepositorySpec(repSpec);
            mBranchExplorerViewer.UpdateRepositorySpec(repSpec);
            mFiltersPanel.UpdateRepositorySpec(repSpec);
        }

        void ResetFiltersClick()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.DesktopGUI.Filters.Reset);

            mConfig.DisplayOptions.DateFilter = LayoutFilters.DateFilter.OneMonthAgo;
            mConfig.DisplayOptions.BranchFilter = null;
            mConfig.DisplayOptions.UserFilter = null;
            mConfig.DisplayOptions.DisplayOnlyRelevantChangesets = false;
            mConfig.DisplayOptions.DisplayOnlyPendingToMergeBranches = false;
            mConfig.DisplayOptions.ShowRelatedBranches = false;

            mConfig.Save(mWkInfo);
            mFiltersPanel.LoadConfiguration();

            RecalculateLayout();
        }

        void OnRefreshClicked()
        {
            ((IRefreshableView)this).Refresh();
        }

        void OnOptionsClicked()
        {
            BranchExplorerOptionsWindow.ShowWindow();
        }

        void RefreshIncomingChangesUpdater()
        {
            if (UVCSPlugin.Instance.DeveloperIncomingChangesUpdater != null)
                UVCSPlugin.Instance.DeveloperIncomingChangesUpdater.Update(DateTime.Now);

            if (UVCSPlugin.Instance.GluonIncomingChangesUpdater != null)
                UVCSPlugin.Instance.GluonIncomingChangesUpdater.Update(DateTime.Now);
        }

        void RefreshShelvedChangesUpdater()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.ShelvedChangesUpdater.Update(DateTime.Now);
        }

        void CreateGUI(
            RepositorySpec repSpec,
            IPlasticWebRestApi restApi,
            LaunchTool.IProcessExecutor processExecutor,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            EditorWindow window)
        {
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;

            var toolbar = ControlBuilder.Toolbar.Create();

            mRefreshButton = ControlBuilder.Toolbar.CreateImageButtonLeft(
                Images.GetRefreshIcon(),
                PlasticLocalization.Name.RefreshButton.GetString(),
                OnRefreshClicked);

            mFiltersPanel = new FiltersPanel(
                mWkInfo,
                mRepSpec,
                FilterableViewType.BranchExplorerView,
                this);

            toolbar.Add(mRefreshButton);
            toolbar.Add(mFiltersPanel);

            var flexibleSpacer = new ToolbarSpacer();
            flexibleSpacer.style.flexGrow = 1;
            toolbar.Add(flexibleSpacer);

            mSearchField = new BrExSearchField();
            mSearchField.DelayedSearchTextChanged += OnDelayedSearchChanged;
            mSearchField.NextSearchResultRequested += OnNextSearchrResultRequested;
            mSearchField.PreviousSearchResultRequested += OnPreviousSearchrResultRequested;
            toolbar.Add(mSearchField);

            Add(toolbar);

            mBranchExplorerViewer = CreateBranchExplorerArea(
                repSpec, restApi, processExecutor, showDownloadPlasticExeWindow, window);

            mEmptyStatePanel = new BranchExplorerEmptyStatePanel(
                ResetFiltersClick);
            mEmptyStatePanel.visible = false;
            mEmptyStatePanel.style.position = Position.Absolute;
            mEmptyStatePanel.style.left = 0;
            mEmptyStatePanel.style.right = 0;
            mEmptyStatePanel.style.top = 0;
            mEmptyStatePanel.style.bottom = 0;

            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.Add(mBranchExplorerViewer);
            contentContainer.Add(mEmptyStatePanel);
            Add(contentContainer);

            mSelectedObjectDataUpdater = new SelectedObjectDataUpdater(mRepSpec, window);

            mProgressControls = new OverlayProgressControls(
                mRefreshButton,
                mBranchExplorerViewer.OptionsButton,
                mBranchExplorerViewer.HomeButton,
                mBranchExplorerViewer.ZoomInButton,
                mBranchExplorerViewer.ZoomOutButton,
                mFiltersPanel);
            Add(mProgressControls);

            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!KeyboardEvents.IsFindShortcutPressed(evt))
                return;

            evt.StopPropagation();

            mSearchField.FocusSearchField();
        }

        void OnPreviousSearchrResultRequested()
        {
            FocusPreviousSearchResult();
        }

        void OnNextSearchrResultRequested()
        {
            FocusNextSearchResult();
        }

        void OnDelayedSearchChanged()
        {
            if (mIsUpdating)
            {
                mIsPendingToApplySearch = true;
                return;
            }

            ApplySearch();
        }

        void ApplySearch()
        {
            mIsPendingToApplySearch = false;

            if (mSearchField.Text == mPreviousSearch)
                return;

            BranchExplorerSearch search = mBranchExplorerViewer.Search;
            search.FindItems(mSearchField.Text.Trim());
            search.FocusFirstSearchResult();

            UpdateSearchCounter();

            mPreviousSearch = mSearchField.Text;
        }

        void FocusNextSearchResult()
        {
            if (mIsUpdating)
                return;

            if (mSearchField.Text == mPreviousSearch)
            {
                mBranchExplorerViewer.Search.FocusNextSearchResult();
                UpdateSearchCounter();
                return;
            }

            ApplySearch();
        }

        void FocusPreviousSearchResult()
        {
            if (mIsUpdating)
                return;

            if (mSearchField.Text == mPreviousSearch)
            {
                mBranchExplorerViewer.Search.FocusPreviousSearchResult();
                UpdateSearchCounter();
                return;
            }

            ApplySearch();
        }

        void RefreshSearchResultsIfNeeded()
        {
            if (mIsPendingToApplySearch)
                return;

            string findPattern = mSearchField.Text;

            if (string.IsNullOrEmpty(findPattern))
                return;

            mBranchExplorerViewer.Search.FindItems(findPattern.Trim());
            UpdateSearchCounter();
        }

        void UpdateSearchCounter()
        {
            BranchExplorerSearch search = mBranchExplorerViewer.Search;

            mSearchField.UpdateSearchCounter(
                search.CurrentSearchIndex,
                search.TotalSearchResults);
        }

        BranchExplorerViewer CreateBranchExplorerArea(
            RepositorySpec repSpec,
            IPlasticWebRestApi restApi,
            LaunchTool.IProcessExecutor processExecutor,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            EditorWindow window)
        {
            mFocusListener = new BranchExplorerFocusListener();
            mFocusListener.AddFocusedObjectObserver(this);

            IIssueTrackerExtension extension = PlasticGui.Plastic.API.GetIssueTracker(
                GlobalConfig.Instance, mWkInfo);

            BranchExplorerSelection selectionHandler =
                new BranchExplorerSelection(mFocusListener);

            mSelectedObjectResolver = new BranchExplorerSelectedObjectResolver(
                repSpec, selectionHandler);

            return new BranchExplorerViewer(
                mWkInfo,
                mRepSpec,
                extension,
                selectionHandler,
                this,
                this,
                OnOptionsClicked);
        }

        static IShelvedChangesUpdater GetShelvedChangesUpdater()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return null;

            return window.ShelvedChangesUpdater;
        }

        WorkspaceInfo mWkInfo;
        RepositorySpec mRepSpec;

        OverlayProgressControls mProgressControls;
        BranchExplorerFocusListener mFocusListener;
        BranchExplorerSelectedObjectResolver mSelectedObjectResolver;

        volatile bool mIsUpdating;
        bool mIsPendingToApplySearch;
        bool mIsFirstLoad = true;

        string mPreviousSearch;
        WorkspaceUIConfiguration mConfig;
        ToolbarButton mRefreshButton;
        FiltersPanel mFiltersPanel;
        BranchExplorerViewer mBranchExplorerViewer;
        BranchExplorerEmptyStatePanel mEmptyStatePanel;

        BrExSearchField mSearchField;

        SelectedObjectDataUpdater mSelectedObjectDataUpdater;
    }
}
