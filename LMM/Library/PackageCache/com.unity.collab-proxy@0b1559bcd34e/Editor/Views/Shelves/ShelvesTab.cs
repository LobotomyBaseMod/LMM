using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using Codice.CM.Common.Mount;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.QueryViews.Shelves;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Inspector.Properties;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Diff;
using Unity.PlasticSCM.Editor.Views.Properties;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
using GluonShelveOperations = GluonGui.WorkspaceWindow.Views.Shelves.ShelveOperations;
#if !UNITY_6000_3_OR_NEWER
using SplitterGUILayout = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterGUILayout;
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.Views.Shelves
{
    internal partial class ShelvesTab :
        IRefreshableView,
        IShelveMenuOperations,
        ShelvesViewMenu.IMenuOperations,
        IGetQueryText,
        IGetFilter,
        FillShelvesView.IShowContentView
    {
        internal string EmptyStateMessage { get { return mEmptyStatePanel.Text; } }
        internal ShelvesListView Table { get { return mShelvesListView; } }
        internal IShelveMenuOperations Operations { get { return this; } }
        internal IProgressControls ProgressControls { get { return mProgressControls; } }
        internal OwnerFilter OwnerFilterForTesting { set { mOwnerFilter = value; } }
        internal bool IsVisible { get; set; } = true; // we need to initialize it to true for the OnDelayedSelectionChanged event to be executed on tests

        internal ShelvesTab(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ChangesetInfo shelveToSelect,
            WorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IMergeViewLauncher mergeViewLauncher,
            IHistoryViewLauncher historyViewLauncher,
            GluonShelveOperations.ICheckinView pendingChangesTab,
            IProgressOperationHandler progressOperationHandler,
            IUpdateProgress updateProgress,
            IShelvedChangesUpdater shelvedChangesUpdater,
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
            mRepSpec = repSpec;
            mRefreshView = workspaceWindow;
            mMergeViewLauncher = mergeViewLauncher;
            mPendingChangesTab = pendingChangesTab;
            mProgressOperationHandler = progressOperationHandler;
            mUpdateProgress = updateProgress;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mSaveAssets = saveAssets;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
            mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            mParentWindow = parentWindow;
            mIsGluonMode = isGluonMode;

            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);

            mProgressControls = new ProgressControlsForViews();

            mFillShelvesView = new FillShelvesView(
                wkInfo,
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
                mFillShelvesView);

            RefreshAndSelect(shelveToSelect);
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
                mShelvesListView.multiColumnHeader.state,
                UnityConstants.SHELVES_TABLE_SETTINGS_NAME);
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
                ref mOwnerFilter,
                mSearchField,
                mShelvesListView,
                this);

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            GUI.enabled = !mProgressControls.IsOperationRunning();

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            mShelvesListView.OnGUI(rect);

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

        void IRefreshableView.Refresh()
        {
            RefreshAndSelect(null);
        }

        //IQueryRefreshableView
        public void RefreshAndSelect(RepObjectInfo repObj)
        {
            mFillShelvesView.FillView(
                mShelvesListView,
                mProgressControls,
                null,
                null,
                (ChangesetInfo)repObj);
        }

        int IShelveMenuOperations.GetSelectedShelvesCount()
        {
            return ShelvesSelection.GetSelectedShelvesCount(mShelvesListView);
        }

        void IShelveMenuOperations.OpenSelectedShelveInNewWindow()
        {
            LaunchDiffOperations.DiffChangeset(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                ShelvesSelection.GetSelectedRepository(mShelvesListView),
                ShelvesSelection.GetSelectedShelve(mShelvesListView),
                mIsGluonMode);
        }

        void IShelveMenuOperations.ApplyShelveInWorkspace()
        {
            bool isCancelled;
            mSaveAssets.UnderWorkspaceWithConfirmation(
                mWkInfo.ClientPath, mWorkspaceOperationsMonitor,
                out isCancelled);

            if (isCancelled)
                return;

            ChangesetInfo shelveToApply = ShelvesSelection.GetSelectedShelve(mShelvesListView);

            if (mIsGluonMode)
            {
                GluonShelveOperations.ApplyPartialShelveset(
                    mWkInfo,
                    shelveToApply,
                    mRefreshView,
                    PlasticExeLauncher.BuildForResolveConflicts(
                        mWkInfo, true, mShowDownloadPlasticExeWindow),
                    this,
                    mProgressControls,
                    mPendingChangesTab,
                    mUpdateProgress,
                    mProgressOperationHandler,
                    mShelvedChangesUpdater);
                return;
            }

            ShelveOperations.ApplyShelveInWorkspace(
                mRepSpec,
                shelveToApply,
                mMergeViewLauncher,
                mProgressOperationHandler);
        }

        void IShelveMenuOperations.DeleteShelve()
        {
            ShelveOperations.DeleteShelve(
                ShelvesSelection.GetSelectedRepositories(mShelvesListView),
                ShelvesSelection.GetSelectedShelves(mShelvesListView),
                this,
                mProgressControls,
                mShelvedChangesUpdater);
        }

        ChangesetInfo ShelvesViewMenu.IMenuOperations.GetSelectedShelve()
        {
            return ShelvesSelection.GetSelectedShelve(mShelvesListView);
        }

        string IGetQueryText.Get()
        {
            return QueryConstants.BuildShelvesQuery(mOwnerFilter == OwnerFilter.MyShelves);
        }

        Filter IGetFilter.Get()
        {
            return new Filter(mShelvesListView.searchString);
        }

        void IGetFilter.Clear()
        {
            // Not used by the Plugin, needed for the Reset filters button
        }

        void FillShelvesView.IShowContentView.ShowContentPanel()
        {
            mEmptyStatePanel.UpdateContent(string.Empty);
        }

        void FillShelvesView.IShowContentView.ShowEmptyStatePanel(
            string explanationText, bool showResetFilterButton)
        {
            mEmptyStatePanel.UpdateContent(explanationText);
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mShelvesListView.SetFocusAndEnsureSelectedItem();
        }

        void OnDelayedSelectionChanged()
        {
            if (!IsVisible)
                return;

            if (EditorWindow.focusedWindow != mParentWindow)
                return;

            List<RepObjectInfo> selectedShelves = ShelvesSelection.
                GetSelectedRepObjectInfos(mShelvesListView);

            if (selectedShelves.Count != 1)
                return;

            RepositorySpec repSpec = ShelvesSelection.GetSelectedRepository(
                mShelvesListView);

            MountPointWithPath mountPoint = MountPointWithPath.BuildWorkspaceRootMountPoint(repSpec);

            SelectedRepObjectInfoData selectedBranchData = SelectedRepObjectInfoData.Create(
                selectedShelves[0],
                repSpec,
                mountPoint);

            Selection.activeObject = selectedBranchData;
        }

        static void DoActionsToolbar(
            ProgressControlsForViews progressControls,
            ref OwnerFilter ownerFilter,
            SearchField searchField,
            ShelvesListView shelvesListView,
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

            DrawOwnerFilter(progressControls, ref ownerFilter, view);

            GUILayout.FlexibleSpace();

            DrawSearchField.For(
                searchField,
                shelvesListView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            EditorGUILayout.EndHorizontal();
        }

        static void DrawOwnerFilter(
            ProgressControlsForViews progressControls,
            ref OwnerFilter ownerFilter,
            IRefreshableView view)
        {
            GUI.enabled = !progressControls.IsOperationRunning();

            EditorGUI.BeginChangeCheck();

            ownerFilter = (OwnerFilter)
                EditorGUILayout.EnumPopup(
                    ownerFilter,
                    EditorStyles.toolbarDropDown,
                    GUILayout.Width(100));

            if (EditorGUI.EndChangeCheck())
            {
                EnumPopupSetting<OwnerFilter>.Save(
                    ownerFilter,
                    UnityConstants.SHELVES_OWNER_FILTER_SETTING_NAME);

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
            FillShelvesView fillShelvesView)
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            mOwnerFilter = EnumPopupSetting<OwnerFilter>.Load(
                UnityConstants.SHELVES_OWNER_FILTER_SETTING_NAME,
                OwnerFilter.MyShelves);

            ShelvesListHeaderState headerState =
                ShelvesListHeaderState.GetDefault();

            TreeHeaderSettings.Load(
                headerState,
                UnityConstants.SHELVES_TABLE_SETTINGS_NAME,
                (int)ShelvesListColumn.CreationDate,
                false,
                ShelvesListHeaderState.GetDefaultVisibleColumns());

            mShelvesListView = new ShelvesListView(
                headerState,
                ShelvesListHeaderState.GetColumnNames(),
                new ShelvesViewMenu(this, this),
                fillShelvesView,
                delayedSelectionChangedAction: OnDelayedSelectionChanged,
                doubleClickAction: ((IShelveMenuOperations)this).OpenSelectedShelveInNewWindow,
                afterItemsChangedAction: fillShelvesView.ShowContentOrEmptyState);

            mShelvesListView.Reload();
        }

        internal enum OwnerFilter
        {
            MyShelves,
            AllShelves
        }
        bool mShouldShowEmptyState;
        OwnerFilter mOwnerFilter;
        SearchField mSearchField;
        ShelvesListView mShelvesListView;
        readonly FillShelvesView mFillShelvesView;
        readonly ProgressControlsForViews mProgressControls;

        readonly EmptyStatePanel mEmptyStatePanel;
        readonly bool mIsGluonMode;
        readonly EditorWindow mParentWindow;
        readonly WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        readonly LaunchTool.IProcessExecutor mProcessExecutor;
        readonly ISaveAssets mSaveAssets;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
        readonly IUpdateProgress mUpdateProgress;
        readonly IProgressOperationHandler mProgressOperationHandler;
        readonly GluonShelveOperations.ICheckinView mPendingChangesTab;
        readonly IMergeViewLauncher mMergeViewLauncher;
        readonly IRefreshView mRefreshView;
        readonly RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;
    }
}
