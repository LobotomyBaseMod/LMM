using System;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BrowseRepository;
using Unity.PlasticSCM.Editor.Views.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Inspector.Properties.Label
{
    internal class LabelPropertiesPanel : IGetRepositorySpec
    {
        internal LabelPropertiesPanel(
            Action repaint,
            WorkspaceInfo wkInfo,
            IWorkspaceWindow workspaceWindow,
            EditorWindow parentWindow)
        {
            mRepaint = repaint;

            mRepaint = repaint;
            mParentWindow = parentWindow;

            mPropertiesPanel = new PropertiesPanel(repaint, workspaceWindow, parentWindow);

            mBrowseRepositoryPanel = new BrowseRepositoryPanel(
                wkInfo,
                this,
                parentWindow);
        }

        internal void OnEnable()
        {
            mBrowseRepositoryPanel.OnEnable();
        }

        internal void OnDisable()
        {
            mBrowseRepositoryPanel.OnDisable();
        }

        internal void Update()
        {
            mBrowseRepositoryPanel.Update();
            mPropertiesPanel.Update();
        }

        internal void ResetForTesting()
        {
            ClearInfo();
        }

        internal void ClearInfo()
        {
            mSelectedRepObjectInfoData = null;
            mPropertiesPanel.ClearInfo();
            mBrowseRepositoryPanel.ClearInfo();
        }

        internal void SetSelectedObject(SelectedRepObjectInfoData selectedRepObjectInfoData)
        {
            mSelectedRepObjectInfoData = selectedRepObjectInfoData;

            mPropertiesPanel.UpdateInfo(selectedRepObjectInfoData.ObjectInfo, selectedRepObjectInfoData.RepSpec);
            mBrowseRepositoryPanel.UpdateInfo((MarkerExtendedInfo)selectedRepObjectInfoData.ObjectInfo);
        }

        internal void OnInspectorGUI()
        {
            mBrowseRepositoryPanel.OnGUI();
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

        RepositorySpec IGetRepositorySpec.Get()
        {
            if (mSelectedRepObjectInfoData == null)
                return null;

            return mSelectedRepObjectInfoData.RepSpec;
        }

        PropertiesPanel mPropertiesPanel;
        BrowseRepositoryPanel mBrowseRepositoryPanel;

        SelectedRepObjectInfoData mSelectedRepObjectInfoData;
        readonly Action mRepaint;
        readonly EditorWindow mParentWindow;
    }
}
