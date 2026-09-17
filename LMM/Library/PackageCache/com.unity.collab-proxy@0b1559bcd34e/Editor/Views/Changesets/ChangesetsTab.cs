using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Common;
using Codice.CM.Common;
using Codice.CM.Common.Mount;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Update;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.QueryViews.Changesets;
using GluonGui;
using PlasticGui.WorkspaceWindow.CodeReview;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.Inspector.Properties;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Changesets.Dialogs;
using Unity.PlasticSCM.Editor.Views.Diff;
using Unity.PlasticSCM.Editor.Views.Merge;
using Unity.PlasticSCM.Editor.Views.Properties;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
using IGluonUpdateReport = PlasticGui.Gluon.IUpdateReport;
#if !UNITY_6000_3_OR_NEWER
using SplitterGUILayout = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterGUILayout;
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal partial class ChangesetsTab :
        IRefreshableView,
        IChangesetMenuOperations,
        ChangesetsViewMenu.IMenuOperations,
        ILaunchCodeReviewWindow,
        IGetQueryText,
        IGetFilterText,
        FillChangesetsView.IShowContentView
    {
        internal DateFilter DateFilterForTesting { set { mDateFilter = value; } }
        internal ChangesetsListView Table { get { return mChangesetsListView; } }
        internal IChangesetMenuOperations Operations { get { return this; } }
        internal string EmptyStateMessage { get { return mEmptyStatePanel.Text; } }
        internal bool IsVisible { get; set; } = true; // we need to initialize it to true for the OnDelayedSelectionChanged event to be executed on tests

        internal interface IRevertToChangesetListener
        {
            void OnSuccessOperation();
        }

        internal ChangesetsTab(
            WorkspaceInfo wkInfo,
            ChangesetInfo changesetToSelect,
            ViewHost viewHost,
            WorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IMergeViewLauncher mergeViewLauncher,
            IHistoryViewLauncher historyViewLauncher,
            IUpdateReport updateReport,
            IGluonUpdateReport gluonUpdateReport,
            IShelvedChangesUpdater shelvedChangesUpdater,
            IRevertToChangesetListener revertToChangesetListener,
            IAssetStatusCache assetStatusCache,
            ISaveAssets saveAssets,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            EditorWindow parentWindow,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mViewHost = viewHost;
            mWorkspaceWindow = workspaceWindow;
            mViewSwitcher = viewSwitcher;
            mGluonUpdateReport = gluonUpdateReport;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mRevertToChangesetListener = revertToChangesetListener;
            mAssetStatusCache = assetStatusCache;
            mSaveAssets = saveAssets;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
            mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            mPendingChangesUpdater = pendingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mParentWindow = parentWindow;
            mIsGluonMode = isGluonMode;

            mProgressControls = new ProgressControlsForViews();
            mShelvePendingChangesQuestionerBuilder = new ShelvePendingChangesQuestionerBuilder(parentWindow);
            mEnableSwitchAndShelveFeatureDialog = new EnableSwitchAndShelveFeature(
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                mParentWindow);
            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);

            mFillChangesetsView = new FillChangesetsView(
                wkInfo,
                null,
                null,
                this,
                this,
                this);

            BuildComponents(
                wkInfo,
                workspaceWindow,
                workspaceWindow,
                viewSwitcher,
                historyViewLauncher,
                pendingChangesUpdater,
                developerIncomingChangesUpdater,
                gluonIncomingChangesUpdater,
                parentWindow,
                mFillChangesetsView);

            mChangesetOperations = new ChangesetOperations(
                wkInfo,
                workspaceWindow,
                viewSwitcher,
                mergeViewLauncher,
                mProgressControls,
                updateReport,
                new ApplyShelveReport(parentWindow),
                new ContinueWithPendingChangesQuestionerBuilder(viewSwitcher, parentWindow),
                mShelvePendingChangesQuestionerBuilder,
                new ApplyShelveWithConflictsQuestionerBuilder(),
                pendingChangesUpdater,
                developerIncomingChangesUpdater,
                shelvedChangesUpdater,
                null,
                null,
                mEnableSwitchAndShelveFeatureDialog);

            RefreshAndSelect(changesetToSelect);
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
                mChangesetsListView.multiColumnHeader.state,
                UnityConstants.CHANGESETS_TABLE_SETTINGS_NAME);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DoActionsToolbar(
                mProgressControls,
                mDateFilter,
                mSearchField,
                mChangesetsListView,
                this);

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            GUI.enabled = !mProgressControls.IsOperationRunning();

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            mChangesetsListView.OnGUI(rect);

            if (Event.current.type == EventType.Layout)
                mShouldShowEmptyState = !mEmptyStatePanel.IsEmpty();

            if (mShouldShowEmptyState)
                mEmptyStatePanel.OnGUI(rect);

            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            if (mProgressControls.IsOperationRunning())
            {
                OverlayProgress.DoOverlayProgress(
                    viewRect,
                    mProgressControls.ProgressData.ProgressPercent,
                    mProgressControls.ProgressData.ProgressMessage);
            }
        }

        internal void SetWorkingObjectInfo(ChangesetInfo changesetInfo)
        {
            mFillChangesetsView.UpdateWorkingObject(changesetInfo);
        }

        internal void SetRevertToChangesetOperationInterfacesForTesting(
            RevertToChangesetOperation.IGetStatusForWorkspace getStatusForWorkspace,
            RevertToChangesetOperation.IUndoCheckoutOperation undoCheckoutOperation,
            RevertToChangesetOperation.IRevertToChangesetMergeController revertToChangesetMergeController)
        {
            mGetStatusForWorkspace = getStatusForWorkspace;
            mUndoCheckoutOperation = undoCheckoutOperation;
            mRevertToChangesetMergeController = revertToChangesetMergeController;
        }

        internal void SetLaunchToolForTesting(
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor)
        {
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
        }

        internal void RefreshAndSelect(RepObjectInfo repObj)
        {
            List<object> changesetsToSelect = repObj == null ?
                null : new List<object> { repObj };

            if (EditorWindow.focusedWindow == mParentWindow)
                Selection.activeObject = null;

            mFillChangesetsView.FillView(
                mChangesetsListView,
                mProgressControls,
                null,
                null,
                null,
                changesetsToSelect);
        }

        void IRefreshableView.Refresh()
        {
            RefreshAndSelect(null);
        }

        int IChangesetMenuOperations.GetSelectedChangesetsCount()
        {
            return ChangesetsSelection.GetSelectedChangesetsCount(mChangesetsListView);
        }

        void IChangesetMenuOperations.DiffChangeset()
        {
            LaunchDiffOperations.DiffChangeset(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                ChangesetsSelection.GetSelectedRepository(mChangesetsListView),
                ChangesetsSelection.GetSelectedChangeset(mChangesetsListView),
                mIsGluonMode);
        }

        void IChangesetMenuOperations.DiffSelectedChangesets()
        {
            List<RepObjectInfo> selectedChangesets = ChangesetsSelection.
                GetSelectedRepObjectInfos(mChangesetsListView);

            if (selectedChangesets.Count < 2)
                return;

            LaunchDiffOperations.DiffSelectedChangesets(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                ChangesetsSelection.GetSelectedRepository(mChangesetsListView),
                (ChangesetExtendedInfo)selectedChangesets[0],
                (ChangesetExtendedInfo)selectedChangesets[1],
                mIsGluonMode);
        }

        void IChangesetMenuOperations.SwitchToChangeset()
        {
            SwitchToChangesetForMode();
        }

        void IChangesetMenuOperations.DiffWithAnotherChangeset() { }

        void IChangesetMenuOperations.CreateBranch()
        {
            CreateBranchForMode();
        }

        void IChangesetMenuOperations.LabelChangeset()
        {
            ChangesetLabelData changesetLabelData = LabelChangesetDialog.Label(
                mParentWindow,
                ChangesetsSelection.GetSelectedRepository(mChangesetsListView),
                ChangesetsSelection.GetSelectedChangeset(mChangesetsListView));

            mChangesetOperations.LabelChangeset(changesetLabelData);
        }

        void IChangesetMenuOperations.MergeChangeset()
        {
            mChangesetOperations.MergeChangeset(
                ChangesetsSelection.GetSelectedRepository(mChangesetsListView),
                ChangesetsSelection.GetSelectedChangeset(mChangesetsListView));
        }

        void IChangesetMenuOperations.CherryPickChangeset() { }

        void IChangesetMenuOperations.SubtractiveChangeset() { }

        void IChangesetMenuOperations.SubtractiveChangesetInterval() { }

        void IChangesetMenuOperations.CherryPickChangesetInterval() { }

        void IChangesetMenuOperations.MergeToChangeset() { }

        void IChangesetMenuOperations.MoveChangeset() { }

        void IChangesetMenuOperations.DeleteChangeset() { }

        void IChangesetMenuOperations.BrowseRepositoryOnChangeset() { }

        void IChangesetMenuOperations.CreateCodeReview()
        {
            RepositorySpec repSpec = ChangesetsSelection.GetSelectedRepository(mChangesetsListView);
            ChangesetInfo changesetInfo = ChangesetsSelection.GetSelectedChangeset(mChangesetsListView);

            NewCodeReviewBehavior choice = SelectNewCodeReviewBehavior.For(repSpec.Server);

            switch (choice)
            {
                case NewCodeReviewBehavior.CreateAndOpenInDesktop:
                    mChangesetOperations.CreateCodeReview(repSpec, changesetInfo, this);
                    break;
                case NewCodeReviewBehavior.RequestFromUnityCloud:
                    OpenRequestReviewPage.ForChangeset(repSpec, changesetInfo.ChangesetId);
                    break;
                case NewCodeReviewBehavior.Ask:
                default:
                    break;
            }
        }

        void ILaunchCodeReviewWindow.Show(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ReviewInfo reviewInfo,
            RepObjectInfo repObjectInfo,
            bool bShowReviewChangesTab)
        {
            LaunchTool.OpenCodeReview(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                repSpec,
                reviewInfo.Id,
                mIsGluonMode);
        }

        void IChangesetMenuOperations.RevertToChangeset()
        {
            if (((IChangesetMenuOperations)this).GetSelectedChangesetsCount() != 1)
                return;

            ChangesetInfo targetChangesetInfo = ((ChangesetsViewMenu.IMenuOperations)this).GetSelectedChangeset();

            RevertToChangesetOperation.RevertTo(
                mWkInfo,
                mViewSwitcher,
                mWorkspaceWindow,
                mProgressControls,
                mGetStatusForWorkspace,
                mUndoCheckoutOperation,
                mRevertToChangesetMergeController,
                GuiMessage.Get(),
                targetChangesetInfo,
                mPendingChangesUpdater,
                RefreshAsset.BeforeLongAssetOperation,
                () => RefreshAsset.AfterLongAssetOperation(mAssetStatusCache),
                mRevertToChangesetListener.OnSuccessOperation);
        }

        void ChangesetsViewMenu.IMenuOperations.DiffBranch()
        {
            LaunchDiffOperations.DiffBranch(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                ChangesetsSelection.GetSelectedRepository(mChangesetsListView),
                ChangesetsSelection.GetSelectedChangeset(mChangesetsListView),
                mIsGluonMode);
        }

        ChangesetInfo ChangesetsViewMenu.IMenuOperations.GetSelectedChangeset()
        {
            return ChangesetsSelection.GetSelectedChangeset(
                mChangesetsListView);
        }

        string IGetQueryText.Get()
        {
            return GetChangesetsQuery.For(mDateFilter);
        }

        string IGetFilterText.Get()
        {
            return mChangesetsListView.searchString;
        }

        void IGetFilterText.Clear()
        {
            // Not used by the Plugin, needed for the Reset filters button
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mChangesetsListView.SetFocusAndEnsureSelectedItem();
        }

        void FillChangesetsView.IShowContentView.ShowContentPanel()
        {
            mEmptyStatePanel.UpdateContent(string.Empty);
        }

        void FillChangesetsView.IShowContentView.ShowEmptyStatePanel(
            string explanationText, bool showResetFilterButton)
        {
            mEmptyStatePanel.UpdateContent(explanationText);
        }

        void OnDelayedSelectionChanged()
        {
            if (!IsVisible)
                return;

            if (EditorWindow.focusedWindow != mParentWindow)
                return;

            List<RepObjectInfo> selectedChangesets = ChangesetsSelection.
                GetSelectedRepObjectInfos(mChangesetsListView);

            if (selectedChangesets.Count != 1)
                return;

            RepositorySpec repSpec = ChangesetsSelection.GetSelectedRepository(
                mChangesetsListView);

            MountPointWithPath mountPoint =
                MountPointWithPath.BuildWorkspaceRootMountPoint(repSpec);

            SelectedRepObjectInfoData selectedBranchData = SelectedRepObjectInfoData.Create(
                selectedChangesets[0],
                repSpec,
                mountPoint);

            Selection.activeObject = selectedBranchData;
        }

        static void DoActionsToolbar(
            ProgressControlsForViews progressControls,
            DateFilter dateFilter,
            SearchField searchField,
            ChangesetsListView changesetsListView,
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

            DrawDateFilter(progressControls, dateFilter, view);

            GUILayout.FlexibleSpace();

            DrawSearchField.For(
                searchField,
                changesetsListView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            EditorGUILayout.EndHorizontal();
        }

        static void DrawDateFilter(
            ProgressControlsForViews progressControls,
            DateFilter dateFilter,
            IRefreshableView view)
        {
            GUI.enabled = !progressControls.IsOperationRunning();

            EditorGUI.BeginChangeCheck();

            dateFilter.FilterType = (DateFilter.Type)
                EditorGUILayout.EnumPopup(
                    dateFilter.FilterType,
                    EditorStyles.toolbarDropDown,
                    GUILayout.Width(UnityConstants.TOOLBAR_DATE_FILTER_COMBO_WIDTH));

            if (EditorGUI.EndChangeCheck())
            {
                EnumPopupSetting<DateFilter.Type>.Save(
                    dateFilter.FilterType,
                    UnityConstants.CHANGESETS_DATE_FILTER_SETTING_NAME);

                view.Refresh();
            }

            GUI.enabled = true;
        }

        void BuildComponents(
            WorkspaceInfo wkInfo,
            IWorkspaceWindow workspaceWindow,
            IRefreshView refreshView,
            IViewSwitcher viewSwitcher,
            IHistoryViewLauncher historyViewLauncher,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            EditorWindow parentWindow,
            FillChangesetsView fillChangesetsView)
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            DateFilter.Type dateFilterType =
                EnumPopupSetting<DateFilter.Type>.Load(
                    UnityConstants.CHANGESETS_DATE_FILTER_SETTING_NAME,
                    DateFilter.Type.LastMonth);
            mDateFilter = new DateFilter(dateFilterType);

            ChangesetsListHeaderState headerState =
                ChangesetsListHeaderState.GetDefault();
            TreeHeaderSettings.Load(
                headerState,
                UnityConstants.CHANGESETS_TABLE_SETTINGS_NAME,
                (int)ChangesetsListColumn.CreationDate,
                false,
                ChangesetsListHeaderState.GetDefaultVisibleColumns());

            mChangesetsListView = new ChangesetsListView(
                headerState,
                ChangesetsListHeaderState.GetColumnNames(),
                new ChangesetsViewMenu(
                    this,
                    this,
                    fillChangesetsView,
                    mIsGluonMode),
                fillChangesetsView,
                fillChangesetsView,
                delayedSelectionChangedAction: OnDelayedSelectionChanged,
                doubleClickAction: ((IChangesetMenuOperations)this).DiffChangeset,
                afterItemsChangedAction: fillChangesetsView.ShowContentOrEmptyState);

            mChangesetsListView.Reload();
        }

        bool mShouldShowEmptyState;
        DateFilter mDateFilter;
        SearchField mSearchField;
        ChangesetsListView mChangesetsListView;

        RevertToChangesetOperation.IGetStatusForWorkspace mGetStatusForWorkspace =
            new RevertToChangesetOperation.GetStatusFromWorkspace();
        RevertToChangesetOperation.IUndoCheckoutOperation mUndoCheckoutOperation =
            new RevertToChangesetOperation.UndoCheckout();
        RevertToChangesetOperation.IRevertToChangesetMergeController mRevertToChangesetMergeController =
            new RevertToChangesetOperation.RevertToChangesetMergeController();
        LaunchTool.IProcessExecutor mProcessExecutor;
        LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;

        readonly ChangesetOperations mChangesetOperations;
        readonly FillChangesetsView mFillChangesetsView;

        readonly EmptyStatePanel mEmptyStatePanel;
        readonly SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog mEnableSwitchAndShelveFeatureDialog;
        readonly bool mIsGluonMode;
        readonly IShelvePendingChangesQuestionerBuilder mShelvePendingChangesQuestionerBuilder;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly ProgressControlsForViews mProgressControls;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly GluonIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly EditorWindow mParentWindow;
        readonly ISaveAssets mSaveAssets;
        readonly WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        readonly IRevertToChangesetListener mRevertToChangesetListener;
        readonly WorkspaceInfo mWkInfo;
        readonly WorkspaceWindow mWorkspaceWindow;
        readonly ViewHost mViewHost;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
        readonly IGluonUpdateReport mGluonUpdateReport;
        readonly IViewSwitcher mViewSwitcher;
    }
}
