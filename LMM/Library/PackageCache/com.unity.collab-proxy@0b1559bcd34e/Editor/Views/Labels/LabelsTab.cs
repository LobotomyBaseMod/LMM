using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.QueryViews.Labels;
using PlasticGui.WorkspaceWindow.Views.QueryViews.Labels;
using PlasticGui.WorkspaceWindow.Update;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Inspector.Properties;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Labels.Dialogs;
using Unity.PlasticSCM.Editor.Views.Merge;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
using IGluonUpdateReport = PlasticGui.Gluon.IUpdateReport;
#if !UNITY_6000_3_OR_NEWER
using SplitterGUILayout = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterGUILayout;
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.Views.Labels
{
    internal partial class LabelsTab :
        IRefreshableView,
        IQueryRefreshableView,
        ILabelMenuOperations,
        IGetQueryText,
        IGetFilterText,
        FillLabelsView.IShowContentView
    {
        internal LabelsListView Table { get { return mLabelsListView; } }
        internal ILabelMenuOperations Operations { get { return this; } }
        internal string EmptyStateMessage { get { return mEmptyStatePanel.Text; } }
        internal DateFilter DateFilterForTesting { set { mDateFilter = value; } }
        internal bool IsVisible { get; set; } = true; // we need to initialize it to true for the OnDelayedSelectionChanged event to be executed on tests

        internal LabelsTab(
            WorkspaceInfo wkInfo,
            WorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IMergeViewLauncher mergeViewLauncher,
            ViewHost viewHost,
            IUpdateReport updateReport,
            IGluonUpdateReport gluonUpdateReport,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            IShelvedChangesUpdater shelvedChangesUpdater,
            IAssetStatusCache assetStatusCache,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            EditorWindow parentWindow,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mViewHost = viewHost;
            mWorkspaceWindow = workspaceWindow;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
            mParentWindow = parentWindow;
            mIsGluonMode = isGluonMode;
            mGluonUpdateReport = gluonUpdateReport;
            mPendingChangesUpdater = pendingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mShelvePendingChangesQuestionerBuilder = new ShelvePendingChangesQuestionerBuilder(parentWindow);
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mAssetStatusCache = assetStatusCache;
            mShelvePendingChangesQuestionerBuilder =
                new ShelvePendingChangesQuestionerBuilder(parentWindow);
            mEnableSwitchAndShelveFeatureDialog = new EnableSwitchAndShelveFeature(
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                mParentWindow);
            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);

            mProgressControls = new ProgressControlsForViews();

            mFillLabelsView = new FillLabelsView(
                wkInfo,
                null,
                null,
                this,
                this,
                this);

            BuildComponents(mFillLabelsView);

            mLabelOperations = new LabelOperations(
                wkInfo,
                workspaceWindow,
                mergeViewLauncher,
                this,
                ViewType.LabelsView,
                mProgressControls,
                updateReport,
                new ApplyShelveReport(parentWindow),
                new ContinueWithPendingChangesQuestionerBuilder(viewSwitcher, parentWindow),
                mShelvePendingChangesQuestionerBuilder,
                new ApplyShelveWithConflictsQuestionerBuilder(),
                pendingChangesUpdater,
                developerIncomingChangesUpdater,
                shelvedChangesUpdater,
                mEnableSwitchAndShelveFeatureDialog);

            RefreshAndSelect(null);
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
                mLabelsListView.multiColumnHeader.state,
                UnityConstants.LABELS_TABLE_SETTINGS_NAME);
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
                mLabelsListView,
                this);

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            GUI.enabled = !mProgressControls.IsOperationRunning();

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            mLabelsListView.OnGUI(rect);

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

        internal void RefreshAndSelect(RepObjectInfo repObj)
        {
            List<object> labelsToSelect = repObj == null ?
                null : new List<object> { repObj };

            if (EditorWindow.focusedWindow == mParentWindow)
                Selection.activeObject = null;

            mFillLabelsView.FillView(
                mLabelsListView,
                mProgressControls,
                null,
                null,
                null,
                labelsToSelect);
        }

        void IRefreshableView.Refresh()
        {
            RefreshAndSelect(null);
        }

        void IQueryRefreshableView.RefreshAndSelect(RepObjectInfo repObj)
        {
            RefreshAndSelect(repObj);
        }

        int ILabelMenuOperations.GetSelectedLabelsCount()
        {
            return LabelsSelection.GetSelectedVisibleLabelsCount(mLabelsListView);
        }

        void ILabelMenuOperations.CreateLabel()
        {
            LabelCreationData labelCreationData = CreateLabelDialog.CreateLabel(
                mParentWindow, mWkInfo);

            mLabelOperations.CreateLabel(
                labelCreationData,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void ILabelMenuOperations.ApplyLabelToWorkspace()
        {
            RepositorySpec repSpec =
                LabelsSelection.GetSelectedRepository(mLabelsListView);
            MarkerExtendedInfo label =
                LabelsSelection.GetSelectedLabel(mLabelsListView);

            mLabelOperations.ApplyLabelToWorkspace(repSpec, label);
        }

        void ILabelMenuOperations.SwitchToLabel()
        {
            RepositorySpec repSpec =
                LabelsSelection.GetSelectedRepository(mLabelsListView);
            MarkerExtendedInfo label =
                LabelsSelection.GetSelectedLabel(mLabelsListView);

            mLabelOperations.SwitchToLabel(
                repSpec,
                label,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void ILabelMenuOperations.BrowseRepositoryOnLabel() { }

        void ILabelMenuOperations.DiffWithAnotherLabel() { }

        void ILabelMenuOperations.DiffSelectedLabels()
        {
            RepositorySpec repSpec =
                LabelsSelection.GetSelectedRepository(mLabelsListView);
            List<RepObjectInfo> selectedLabels =
                LabelsSelection.GetSelectedRepObjectInfos(mLabelsListView);

            if (selectedLabels.Count < 2)
                return;

            LaunchDiffOperations.DiffSelectedLabels(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                repSpec,
                (MarkerExtendedInfo)selectedLabels[0],
                (MarkerExtendedInfo)selectedLabels[1],
                mIsGluonMode);
        }

        void ILabelMenuOperations.MergeLabel()
        {
            RepositorySpec repSpec =
                LabelsSelection.GetSelectedRepository(mLabelsListView);
            MarkerExtendedInfo label =
                LabelsSelection.GetSelectedLabel(mLabelsListView);

            mLabelOperations.MergeLabel(repSpec, label);
        }

        void ILabelMenuOperations.MergeToLabel() { }

        void ILabelMenuOperations.CreateBranchFromLabel()
        {
            CreateBranchForMode();
        }

        void ILabelMenuOperations.RenameLabel()
        {
            RepositorySpec repSpec =
                LabelsSelection.GetSelectedRepository(mLabelsListView);
            MarkerExtendedInfo label =
                LabelsSelection.GetSelectedLabel(mLabelsListView);

            LabelRenameData labelRenameData = RenameLabelDialog.GetLabelRenameData(
                repSpec,
                label,
                mParentWindow);

            mLabelOperations.RenameLabel(labelRenameData);
        }

        void ILabelMenuOperations.DeleteLabel()
        {
            RepositorySpec repSpec =
                LabelsSelection.GetSelectedRepository(mLabelsListView);
            MarkerExtendedInfo label =
                LabelsSelection.GetSelectedLabel(mLabelsListView);

            mLabelOperations.DeleteLabel(
                new List<RepositorySpec>() { repSpec },
                new List<MarkerExtendedInfo>() { label });
        }

        void ILabelMenuOperations.ViewPermissions() { }

        string IGetQueryText.Get()
        {
            return GetLabelsQuery(mDateFilter);
        }

        string IGetFilterText.Get()
        {
            return mLabelsListView.searchString;
        }

        void IGetFilterText.Clear()
        {
            // Not used by the Plugin, needed for the Reset filters button
        }

        void FillLabelsView.IShowContentView.ShowContentPanel()
        {
            mEmptyStatePanel.UpdateContent(string.Empty);
        }

        void FillLabelsView.IShowContentView.ShowEmptyStatePanel(
            string explanationText, bool showResetFilterButton)
        {
            mEmptyStatePanel.UpdateContent(explanationText);
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mLabelsListView.SetFocusAndEnsureSelectedItem();
        }

        static string GetLabelsQuery(DateFilter dateFilter)
        {
            if (dateFilter.FilterType == DateFilter.Type.AllTime)
                return QueryConstants.LabelsBeginningQuery;

            string whereClause = QueryConstants.GetDateWhereClause(
                dateFilter.GetTimeAgo());

            return string.Format("{0} {1}",
                QueryConstants.LabelsBeginningQuery,
                whereClause);
        }

        void OnDelayedSelectionChanged()
        {
            if (!IsVisible)
                return;

            if (EditorWindow.focusedWindow != mParentWindow)
                return;

            List<RepObjectInfo> selectedLabels = LabelsSelection.
                GetSelectedRepObjectInfos(mLabelsListView);

            if (selectedLabels.Count != 1)
                return;

            RepositorySpec repSpec = LabelsSelection.GetSelectedRepository(mLabelsListView);

            SelectedRepObjectInfoData selectedBranchData = SelectedRepObjectInfoData.Create(
                selectedLabels[0],
                repSpec);

            Selection.activeObject = selectedBranchData;
        }

        static void DoActionsToolbar(
            ProgressControlsForViews progressControls,
            DateFilter dateFilter,
            SearchField searchField,
            LabelsListView labelsListView,
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
                labelsListView,
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
                    UnityConstants.LABELS_DATE_FILTER_SETTING_NAME);

                view.Refresh();
            }

            GUI.enabled = true;
        }

        void BuildComponents(FillLabelsView fillLabelsView)
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            DateFilter.Type dateFilterType =
                EnumPopupSetting<DateFilter.Type>.Load(
                    UnityConstants.LABELS_DATE_FILTER_SETTING_NAME,
                    DateFilter.Type.LastMonth);
            mDateFilter = new DateFilter(dateFilterType);

            LabelsListHeaderState headerState =
                LabelsListHeaderState.GetDefault();

            TreeHeaderSettings.Load(
                headerState,
                UnityConstants.LABELS_TABLE_SETTINGS_NAME,
                (int)LabelsListColumn.CreationDate,
                false);

            mLabelsListView = new LabelsListView(
                headerState,
                LabelsListHeaderState.GetColumnNames(),
                LabelsViewMenu.BuildForContextMenu(this),
                fillLabelsView,
                delayedSelectionChangedAction: OnDelayedSelectionChanged,
                doubleClickAction: ((ILabelMenuOperations)this).BrowseRepositoryOnLabel,
                afterItemsChangedAction: fillLabelsView.ShowContentOrEmptyState);

            mLabelsListView.Reload();
        }

        bool mShouldShowEmptyState;
        SearchField mSearchField;
        DateFilter mDateFilter;
        LabelsListView mLabelsListView;

        readonly LabelOperations mLabelOperations;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly LaunchTool.IProcessExecutor mProcessExecutor;

        readonly EmptyStatePanel mEmptyStatePanel;
        readonly FillLabelsView mFillLabelsView;

        readonly bool mIsGluonMode;
        readonly ViewHost mViewHost;
        readonly IGluonUpdateReport mGluonUpdateReport;
        readonly WorkspaceWindow mWorkspaceWindow;
        readonly ProgressControlsForViews mProgressControls;
        readonly EditorWindow mParentWindow;
        readonly WorkspaceInfo mWkInfo;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly GluonIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly IShelvePendingChangesQuestionerBuilder mShelvePendingChangesQuestionerBuilder;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog mEnableSwitchAndShelveFeatureDialog;
    }
}
