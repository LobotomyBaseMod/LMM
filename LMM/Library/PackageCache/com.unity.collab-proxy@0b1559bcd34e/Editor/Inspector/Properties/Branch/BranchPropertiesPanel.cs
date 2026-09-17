using System;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Diff;
using Unity.PlasticSCM.Editor.Views.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Inspector.Properties.Branch
{
    internal class BranchPropertiesPanel
    {
        internal bool IsChangesetByChangesetModeForTesting
        {
            get { return mToolbarIndex == 1; }
            set { mToolbarIndex = value ? 1 : 0; }
        }

        internal DiffPanel DiffPanel => mDiffPanel;

        internal BranchPropertiesPanel(
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
            mToolbarIndex = SessionState.GetInt(TOOLBAR_INDEX_KEY, defaultValue: 0);

            mPropertiesPanel = new PropertiesPanel(repaint, workspaceWindow, parentWindow);

            mDiffPanel = new DiffPanel(
                repaint,
                wkInfo,
                workspaceWindow,
                viewSwitcher,
                historyViewLauncher,
                refreshView,
                assetStatusCache,
                showDownloadPlasticExeWindow,
                pendingChangesUpdater,
                developerIncomingChangesUpdater,
                gluonIncomingChangesUpdater,
                parentWindow,
                isGluonMode);

            mChangesetByChangesetDiffPanel = new ChangesetByChangesetDiffPanel(
                repaint,
                wkInfo,
                workspaceWindow,
                viewSwitcher,
                historyViewLauncher,
                refreshView,
                assetStatusCache,
                showDownloadPlasticExeWindow,
                pendingChangesUpdater,
                developerIncomingChangesUpdater,
                gluonIncomingChangesUpdater,
                parentWindow,
                isGluonMode);
        }

        internal void OnEnable()
        {
            mDiffPanel.OnEnable();
            mChangesetByChangesetDiffPanel.OnEnable();
        }

        internal void OnDisable()
        {
            mDiffPanel.OnDisable();
            mChangesetByChangesetDiffPanel.OnDisable();
        }

        internal void Update()
        {
            mDiffPanel.Update();
            mPropertiesPanel.Update();
            mChangesetByChangesetDiffPanel.Update();
        }

        internal void ResetForTesting()
        {
            ClearInfo();

            mToolbarIndex = 0;
        }

        internal void ClearInfo()
        {
            mSelectedRepObjectInfoData = null;
            mPropertiesPanel.ClearInfo();
            mDiffPanel.ClearInfo();
            mChangesetByChangesetDiffPanel.ClearInfo();
        }

        internal void SetSelectedObject(SelectedRepObjectInfoData selectedRepObjectInfoData)
        {
            mSelectedRepObjectInfoData = selectedRepObjectInfoData;

            BranchInfo selectedBranchInfo = (BranchInfo)selectedRepObjectInfoData.ObjectInfo;

            if (mToolbarIndex == 1)
            {
                mChangesetByChangesetDiffPanel.UpdateInfo(selectedRepObjectInfoData.MountPoint, selectedBranchInfo);
            }
            else
            {
                mPropertiesPanel.UpdateInfo(selectedBranchInfo, selectedRepObjectInfoData.RepSpec);
                mDiffPanel.UpdateInfo(selectedRepObjectInfoData.MountPoint, selectedBranchInfo);
            }
        }

        internal void OnInspectorGUI()
        {
            if (mToolbarIndex == 1)
                mChangesetByChangesetDiffPanel.OnGUI();
            else
            {
                mDiffPanel.OnGUI();
            }
        }

        internal void OnHeaderGUI()
        {
            EditorGUILayout.BeginVertical();

            DoDiffModeButtons();

            DrawSeparator();

            if (mToolbarIndex == 1)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            mPropertiesPanel.OnGUI();

            DrawSeparator();

            EditorGUILayout.EndVertical();
        }

        void DoDiffModeButtons()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space(0.5f);

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            //GUI.enabled = !mProgressControls.IsOperationRunning();

            int previousToolbarIndex = mToolbarIndex;

            string[] options = new string[]
            {
                PlasticLocalization.Name.DiffEntireBranch.GetString(),
                PlasticLocalization.Name.DiffByChangeset.GetString()
            };

            mToolbarIndex = GUILayout.Toolbar(
                mToolbarIndex,
                options,
                UnityStyles.LargeButton,
                GUI.ToolbarButtonSize.FitToContents);

            GUILayout.FlexibleSpace();

            //GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(0.5f);

            EditorGUILayout.EndVertical();

            // If mode changed, update the panel with the currently selected branch
            if (previousToolbarIndex != mToolbarIndex)
            {
                SessionState.SetInt(TOOLBAR_INDEX_KEY, mToolbarIndex);
                SetSelectedObject(mSelectedRepObjectInfoData);
            }
        }

        static void DrawSeparator()
        {
            Rect bottomSeparatorRect = GUILayoutUtility.GetRect(
                0,
                1,
                GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bottomSeparatorRect, UnityStyles.Colors.BarBorder);
        }

        int mToolbarIndex;
        DiffPanel mDiffPanel;
        PropertiesPanel mPropertiesPanel;
        ChangesetByChangesetDiffPanel mChangesetByChangesetDiffPanel;
        SelectedRepObjectInfoData mSelectedRepObjectInfoData;
        readonly Action mRepaint;

        const string TOOLBAR_INDEX_KEY = "BranchPropertiesPanel.ToolbarIndex";
    }
}
