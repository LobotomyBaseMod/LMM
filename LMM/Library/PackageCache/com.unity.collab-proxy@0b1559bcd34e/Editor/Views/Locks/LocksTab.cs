using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.Help.Actions;
using PlasticGui.WorkspaceWindow.Locks;
using PlasticGui.WorkspaceWindow.QueryViews;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.Views.Locks
{
    internal sealed class LocksTab :
        IRefreshableView,
        ILockMenuOperations,
        IGetFilter
    {
        internal LocksListView Table { get { return mLocksListView; } }

        internal ILockMenuOperations Operations { get { return this; } }

        internal LocksTab(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            IRefreshView refreshView,
            IAssetStatusCache assetStatusCache,
            EditorWindow parentWindow)
        {
            mRepSpec = repSpec;
            mRefreshView = refreshView;
            mAssetStatusCache = assetStatusCache;
            mParentWindow = parentWindow;
            mProgressControls = new ProgressControlsForViews();

            BuildComponents(wkInfo, mRepSpec);

            mFillLocksTable = new FillLocksTable(this, mLocksListView);

            ((IRefreshableView) this).Refresh();
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

            mLocksListView.OnDisable();
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);

            if (mConfigureLockRulesButtonClicked)
            {
                OpenConfigureLockRulesPage.Run(mRepSpec.Server);
                mConfigureLockRulesButtonClicked = false;
            }
        }

        internal void OnGUI()
        {
            DoActionsToolbar(
                mProgressControls,
                mSearchField,
                mLocksListView,
                this,
                mIsReleaseLocksButtonEnabled,
                mIsRemoveLocksButtonEnabled,
                this);

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            DoLocksArea(
                mLocksListView,
                mProgressControls.IsOperationRunning());

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
            mFillLocksTable.FillTable(
                mRepSpec,
                mLocksListView,
                mProgressControls);

            if (mAssetStatusCache != null)
                mAssetStatusCache.ClearLocks();
        }

        List<LockInfo.LockStatus> ILockMenuOperations.GetSelectedLocksStatus()
        {
            return mLocksListView.GetSelectedLocks().
                Select(lockInfo => lockInfo.Status).ToList();
        }

        void ILockMenuOperations.ReleaseLocks()
        {
            LockOperations.ReleaseLocks(
                mRepSpec,
                mLocksListView.GetSelectedLocks(),
                this,
                mRefreshView,
                mProgressControls,
                () => RefreshAsset.VersionControlCache(mAssetStatusCache));
        }

        void ILockMenuOperations.RemoveLocks()
        {
            LockOperations.RemoveLocks(
                mRepSpec,
                mLocksListView.GetSelectedLocks(),
                this,
                mRefreshView,
                mProgressControls,
                () => RefreshAsset.VersionControlCache(mAssetStatusCache));
        }

        Filter IGetFilter.Get()
        {
            return new Filter(mLocksListView.searchString);
        }

        void IGetFilter.Clear()
        {
            // Not used by the Plugin, needed for the Reset filters button
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mLocksListView.SetFocusAndEnsureSelectedItem();
        }

        void OnSelectionChanged()
        {
            LockMenuOperations operations = LockMenuUpdater.GetAvailableMenuOperations(
                ((ILockMenuOperations)this).GetSelectedLocksStatus());

            mIsReleaseLocksButtonEnabled = operations.HasFlag(
                LockMenuOperations.Release);
            mIsRemoveLocksButtonEnabled = operations.HasFlag(
                LockMenuOperations.Remove);
        }

        void OnItemsChanged(IEnumerable<LockInfo> items)
        {
            mFillLocksTable.ShowContentOrEmptyStatePanel(items);
        }

        void DoActionsToolbar(
            ProgressControlsForViews progressControls,
            SearchField searchField,
            LocksListView locksListView,
            ILockMenuOperations lockMenuOperations,
            bool isReleaseLocksButtonEnabled,
            bool isRemoveLocksButtonEnabled,
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

            DoReleaseLocksButton(
                lockMenuOperations,
                isReleaseLocksButtonEnabled);

            DoRemoveLocksButton(
                lockMenuOperations,
                isRemoveLocksButtonEnabled);

            GUILayout.FlexibleSpace();

            mConfigureLockRulesButtonClicked = DoConfigureLockRulesButton();

            GUILayout.Space(2);

            DrawSearchField.For(
                searchField,
                locksListView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            EditorGUILayout.EndHorizontal();
        }

        static void DoLocksArea(
            LocksListView locksListView,
            bool isOperationRunning)
        {
            GUI.enabled = !isOperationRunning;

            var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            locksListView.OnGUI(rect);

            GUI.enabled = true;
        }

        static void DoReleaseLocksButton(
            ILockMenuOperations lockMenuOperations,
            bool isEnabled)
        {
            GUI.enabled = isEnabled;

            if (GUILayout.Button(new GUIContent(
                    PlasticLocalization.Name.ReleaseLocksButton.GetString(),
                    PlasticLocalization.Name.ReleaseLocksButtonTooltip.GetString()),
                    EditorStyles.toolbarButton))
            {
                lockMenuOperations.ReleaseLocks();
            }

            GUI.enabled = true;
        }

        static void DoRemoveLocksButton(
            ILockMenuOperations lockMenuOperations,
            bool isEnabled)
        {
            GUI.enabled = isEnabled;

            if (GUILayout.Button(new GUIContent(
                    PlasticLocalization.Name.RemoveLocksButton.GetString(),
                    PlasticLocalization.Name.RemoveLocksButtonTooltip.GetString()),
                    EditorStyles.toolbarButton))
            {
                lockMenuOperations.RemoveLocks();
            }

            GUI.enabled = true;
        }

        bool DoConfigureLockRulesButton()
        {
            return GUILayout.Button(PlasticLocalization.Name.
                    ConfigureLockRules.GetString(),
                    EditorStyles.toolbarButton);
        }

        void BuildComponents(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec)
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed +=
                SearchField_OnDownOrUpArrowKeyPressed;

            mLocksListView = new LocksListView(
                wkInfo,
                repSpec,
                LocksListHeaderState.GetDefault(),
                LocksListHeaderState.GetColumnNames(),
                new LocksViewMenu(this),
                OnSelectionChanged,
                OnItemsChanged,
                mParentWindow.Repaint);

            mLocksListView.Reload();
        }

        bool mIsReleaseLocksButtonEnabled;
        bool mIsRemoveLocksButtonEnabled;
        bool mConfigureLockRulesButtonClicked;

        SearchField mSearchField;
        LocksListView mLocksListView;

        readonly ProgressControlsForViews mProgressControls;
        readonly FillLocksTable mFillLocksTable;
        readonly EditorWindow mParentWindow;
        readonly IRefreshView mRefreshView;
        readonly RepositorySpec mRepSpec;
        readonly IAssetStatusCache mAssetStatusCache;
    }
}
