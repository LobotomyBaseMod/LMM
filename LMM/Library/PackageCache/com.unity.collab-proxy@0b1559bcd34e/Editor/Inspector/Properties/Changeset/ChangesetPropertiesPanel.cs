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

namespace Unity.PlasticSCM.Editor.Inspector.Properties.Changeset
{
    internal class ChangesetPropertiesPanel
    {
        internal DiffPanel DiffPanel => mDiffPanel;

        internal ChangesetPropertiesPanel(
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

            mRepaint = repaint;

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
        }

        internal void Update()
        {
            mDiffPanel.Update();
            mPropertiesPanel.Update();
        }

        internal void OnEnable()
        {
            mDiffPanel.OnEnable();
        }

        internal void OnDisable()
        {
            mDiffPanel.OnDisable();
        }

        internal void ResetForTesting()
        {
            ClearInfo();
        }

        internal void ClearInfo()
        {
            mPropertiesPanel.ClearInfo();
            mDiffPanel.ClearInfo();
        }

        internal void SetSelectedObject(SelectedRepObjectInfoData selectedRepObjectInfoData)
        {
            mPropertiesPanel.UpdateInfo(selectedRepObjectInfoData.ObjectInfo, selectedRepObjectInfoData.RepSpec);
            mDiffPanel.UpdateInfo(selectedRepObjectInfoData.MountPoint, selectedRepObjectInfoData.ObjectInfo);
        }

        internal void OnInspectorGUI()
        {
            mDiffPanel.OnGUI();
        }

        internal void OnHeaderGUI()
        {
            EditorGUILayout.BeginVertical();

            mPropertiesPanel.OnGUI();

            Rect separatorRect = GUILayoutUtility.GetRect(
                0,
                1,
                GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(separatorRect, UnityStyles.Colors.BarBorder);

            EditorGUILayout.EndVertical();
        }

        DiffPanel mDiffPanel;
        PropertiesPanel mPropertiesPanel;

        readonly Action mRepaint;
    }
}
