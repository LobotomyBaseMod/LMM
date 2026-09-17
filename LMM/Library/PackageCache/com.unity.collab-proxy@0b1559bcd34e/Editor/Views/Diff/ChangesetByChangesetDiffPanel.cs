using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using Codice.CM.Common.Mount;
using Codice.Utils;
using PlasticGui;
using PlasticGui.Diff;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.Diff.Type;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Properties;

#if !UNITY_6000_3_OR_NEWER
using SplitterGUILayout = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterGUILayout;
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal class ChangesetByChangesetDiffPanel :
        ExploreChangesets.IChangesetsPanel
    {
        internal DiffPanel DiffPanel { get { return mDiffPanel; } }

        internal ChangesetByChangesetDiffPanel(
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

            mEmptyStatePanel = new EmptyStatePanel(repaint);
            mProgressControls = new ProgressControlsForViews();

            mShowDiffsDataCalculator = ShowChangesetDiffsDataCalculator.BuildForUnix(
                new GetDifferences());

            BuildComponents();

            mDiffSplitterState = new SplitterState(
                SplitterSettings.Load(
                    UnityConstants.DIFF_SPLITTER_SETTINGS_NAME,
                    new float[] { 0.3f, 0.7f }),
                    new int[] { 100, 233 },
                    new int[] { 100000, 100000 }
            );
        }

        internal void ClearInfo()
        {
            mSelectedMountWithPath = null;
            mSelectedRepObjectInfo = null;

            RefreshChangesetsList();

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

            if (repObjectInfo is BranchInfo)
            {
                RefreshChangesetsList();
            }

            mRepaint();
        }

        internal void OnEnable()
        {
            mDiffPanel.OnEnable();
        }

        internal void OnDisable()
        {
            mDiffPanel.OnDisable();

            SplitterSettings.Save(
                mDiffSplitterState,
                UnityConstants.DIFF_SPLITTER_SETTINGS_NAME);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mRepaint);
            mDiffPanel.Update();
            mPropertiesPanel.Update();
        }

        internal void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            SplitterGUILayout.BeginVerticalSplit(mDiffSplitterState);

            DoChangesetsArea();
            DoPropertiesAndDiffArea();

            SplitterGUILayout.EndVerticalSplit();

            if (mProgressControls.HasNotification())
            {
                DrawProgressForViews.ForNotificationArea(
                    mProgressControls.ProgressData);
            }

            EditorGUILayout.EndVertical();
        }

        string ExploreChangesets.IChangesetsPanel.GetFilterText()
        {
            return mChangesetsListView.searchString;
        }

        void ExploreChangesets.IChangesetsPanel.ShowContentPanel()
        {
            mEmptyStatePanel.UpdateContent(string.Empty);
        }

        void ExploreChangesets.IChangesetsPanel.ShowEmptyStatePanel(string explanationText)
        {
            mEmptyStatePanel.UpdateContent(explanationText);
        }

        void ExploreChangesets.IChangesetsPanel.ShowWaitingAnimation()
        {
            ((IProgressControls)mProgressControls).ShowProgress(
                PlasticLocalization.Name.Loading.GetString());
        }

        void ExploreChangesets.IChangesetsPanel.SetChangesetList(
            List<ChangesetInfo> changesets,
            ChangesetInfo csetInfoToSelect,
            Dictionary<SEID, string> resolvedUsers)
        {
            mResolvedUsers = resolvedUsers;

            ((IPlasticTable<ChangesetInfo>)mChangesetsListView).FillEntriesAndSelectRows(
                changesets,
                changesets.Count == 0 ?
                    new List<ChangesetInfo>() :
                    new List<ChangesetInfo> { csetInfoToSelect ?? changesets[0] },
                null);
        }

        void ExploreChangesets.IChangesetsPanel.HideWaitingAnimation()
        {
            ((IProgressControls)mProgressControls).HideProgress();
        }

        string ResolveUserName(SEID seid)
        {
            string result = seid.Data;
            mResolvedUsers?.TryGetValue(seid, out result);
            return result;
        }

        void RefreshChangesetsList()
        {
            mPropertiesPanel.ClearInfo();
            mDiffPanel.ClearInfo();

            mExploreChangesets.SetBranchInfo(
                (BranchInfo)mSelectedRepObjectInfo,
                mSelectedMountWithPath?.RepSpec);

            mExploreChangesets.LoadChangesets(mChangesetsListView.searchString);
        }

        void DoPropertiesAndDiffArea()
        {
            GUILayout.BeginVertical();

            DoPropertiesArea();
            DoDiffArea();

            GUILayout.EndVertical();
        }

        void DoChangesetsArea()
        {
            EditorGUILayout.BeginVertical();

            DoChangesetsToolbar();

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            DoChangesetsListView();

            EditorGUILayout.EndVertical();

            if (mProgressControls.IsOperationRunning())
            {
                OverlayProgress.DoOverlayProgress(
                    viewRect,
                    mProgressControls.ProgressData.ProgressPercent,
                    mProgressControls.ProgressData.ProgressMessage);
            }
        }

        void DoPropertiesArea()
        {
            GUILayout.BeginVertical();

            Rect separatorRect = GUILayoutUtility.GetRect(
                0,
                1,
                GUILayout.ExpandHeight(false),
                GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(separatorRect, UnityStyles.Colors.BarBorder);

            mPropertiesPanel.OnGUI();

            GUILayout.EndVertical();
        }

        void DoChangesetsToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();

            GUILayout.Space(3);

            DrawSearchField.For(
                mSearchField,
                mChangesetsListView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            EditorGUILayout.EndHorizontal();
        }

        void DoChangesetsListView()
        {
            GUI.enabled = !mProgressControls.IsOperationRunning();

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            mChangesetsListView.OnGUI(rect);

            if (Event.current.type == EventType.Layout)
                mShouldShowEmptyState = !mEmptyStatePanel.IsEmpty();

            if (mShouldShowEmptyState)
                mEmptyStatePanel.OnGUI(rect);

            GUI.enabled = true;
        }

        void DoDiffArea()
        {
            EditorGUILayout.BeginVertical();

            Rect separatorRect = GUILayoutUtility.GetRect(
                0,
                1,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(separatorRect, UnityStyles.Colors.BarBorder);
            mDiffPanel.OnGUI();

            EditorGUILayout.EndVertical();
        }

        void BuildComponents()
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            mPropertiesPanel = new PropertiesPanel(
                mRepaint,
                mWorkspaceWindow,
                mParentWindow);

            mDiffPanel = new DiffPanel(
                mRepaint,
                mWkInfo,
                mWorkspaceWindow,
                mViewSwitcher,
                mHistoryViewLauncher,
                mRefreshView,
                mAssetStatusCache,
                mShowDownloadPlasticExeWindow,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mGluonIncomingChangesUpdater,
                mParentWindow,
                mIsGluonMode);

            mExploreChangesets = new ExploreChangesets(mDiffPanel, this);

            mChangesetsListView = new ChangesetByChangesetListView(
                mExploreChangesets,
                ResolveUserName,
                delayedSelectionChangedAction: OnDelayedChangesetSelectionChanged);

            mChangesetsListView.Reload();
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mChangesetsListView.SetFocusAndEnsureSelectedItem();
        }

        void OnDelayedChangesetSelectionChanged()
        {
            List<ChangesetInfo> selectedChangesetInfos = mChangesetsListView.GetSelectedChangesetInfos();

            if (selectedChangesetInfos.Count == 0 || selectedChangesetInfos[0] == null)
                return;

            if (mSelectedMountWithPath == null)
                return;

            mPropertiesPanel.UpdateInfo(selectedChangesetInfos[0], mSelectedMountWithPath.RepSpec);

            mExploreChangesets.LoadChangesetDiffs(
                selectedChangesetInfos[0],
                mShowDiffsDataCalculator,
                new CancelToken());
        }

        RepObjectInfo mSelectedRepObjectInfo;
        MountPointWithPath mSelectedMountWithPath;

        bool mShouldShowEmptyState;
        SearchField mSearchField;
        ChangesetByChangesetListView mChangesetsListView;
        PropertiesPanel mPropertiesPanel;
        DiffPanel mDiffPanel;

        ExploreChangesets mExploreChangesets;
        Dictionary<SEID, string> mResolvedUsers;

        readonly ProgressControlsForViews mProgressControls;
        readonly ExploreChangesets.IShowDiffsDataCalculator mShowDiffsDataCalculator;

        readonly EmptyStatePanel mEmptyStatePanel;

        readonly Action mRepaint;
        readonly SplitterState mDiffSplitterState;
        readonly bool mIsGluonMode;
        readonly EditorWindow mParentWindow;
        readonly IRefreshView mRefreshView;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IHistoryViewLauncher mHistoryViewLauncher;
        readonly IViewSwitcher mViewSwitcher;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly WorkspaceInfo mWkInfo;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IIncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        readonly IIncomingChangesUpdater mGluonIncomingChangesUpdater;
    }
}
