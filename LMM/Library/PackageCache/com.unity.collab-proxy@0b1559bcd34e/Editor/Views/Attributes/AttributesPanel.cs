using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Attributes;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.Views.Properties;

namespace Unity.PlasticSCM.Editor.Views.Attributes
{
    internal class AttributesPanel :
        AttributesPanelOperations.IAttributesPanel,
        AttributePanel.IAttributesPanel
    {
        internal AttributesPanel(
                Action repaint,
                IWorkspaceWindow workspaceWindow,
                EditorWindow window)
        {
            mRepaint = repaint;
            mWorkspaceWindow = workspaceWindow;
            mWindow = window;

            mProgressControls = new ProgressControlsForViews();
        }

        internal void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mRepSpec = repSpec;
        }

        internal void UpdateInfo(long objId)
        {
            if (mObjId == objId)
                return;

            mObjId = objId;

            AttributesPanelOperations.Refresh(
                mRepSpec, mObjId, mProgressControls, this);
        }

        internal void Clear()
        {
            mObjId = -1;

            ClearAttributePanels();
        }

        internal void Refresh()
        {
            if (mObjId == -1 || mRepSpec == null)
                return;

            mIsApplyingAttribute = false;
            AttributesPanelOperations.Refresh(
                mRepSpec, mObjId, mProgressControls, this);
        }

        internal void Update()
        {
            foreach (AttributePanel attributePanel in mAttributePanels)
                attributePanel.Update();

            if (!mbOpenApplyAttributeDialog)
                return;

            mbOpenApplyAttributeDialog = false;
            ApplyAttributesButton_Click();
        }

        internal void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.Space(HORIZONTAL_MARGIN, false);

                DrawAttributePanels(mAttributePanels, mLastValidAvailableWidth - 2 * HORIZONTAL_MARGIN);

                EditorGUILayout.Space(HORIZONTAL_MARGIN, false);
            }

            Rect layoutRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                mLastValidAvailableWidth = layoutRect.width;
            }

            DrawAddAttributeButton();
        }

        void DrawAttributePanels(List<AttributePanel> attributePanels, float availableWidth)
        {
            float currentLineWidth = 0f;
            bool isNewLine = true;
            bool showScrollbar = mLastDesiredHeight > MAX_HEIGHT;

            if (showScrollbar)
                availableWidth -= UnityConstants.SCROLLBAR_WIDTH;

            mScrollPosition = GUILayout.BeginScrollView(
                mScrollPosition,
                false,
                false,
                GUIStyle.none,
                showScrollbar ? GUI.skin.verticalScrollbar : GUIStyle.none,
                GUILayout.Height(showScrollbar ? MAX_HEIGHT : mLastDesiredHeight));

            if (Event.current.type == EventType.Repaint)
                mLastDesiredHeight = 0;

            using (new GUILayout.VerticalScope())
            {
                foreach (AttributePanel attributePanel in attributePanels)
                {
                    float panelWidth = attributePanel.DesiredSize.x;

                    if (currentLineWidth + panelWidth >= availableWidth && !isNewLine)
                    {
                        GUILayout.EndHorizontal();
                        isNewLine = true;
                        currentLineWidth = 0f;
                    }

                    if (isNewLine)
                    {
                        GUILayout.BeginHorizontal();
                        isNewLine = false;

                        if (Event.current.type == EventType.Repaint)
                            mLastDesiredHeight += attributePanel.DesiredSize.y;
                    }

                    attributePanel.OnGUI(availableWidth);
                    currentLineWidth = currentLineWidth + panelWidth;
                }

                if (!isNewLine)
                    GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        void DrawAddAttributeButton()
        {
            if (GUILayout.Button(
                    new GUIContent("Add attribute", Images.GetAddIcon()),
                    UnityStyles.AttributesPanel.AddAttributeButton,
                    GUILayout.ExpandWidth(false)))
            {
                mbOpenApplyAttributeDialog = true;
            }
        }

        void ApplyAttributesButton_Click()
        {
            ApplyAttributeData applyAttributeData = AttributeDataDialog.BuildForApplyAttribute(
                mRepSpec, mWorkspaceWindow, mWindow);

            mIsApplyingAttribute = true;
            AttributesPanelOperations.ApplyAttribute(
                mRepSpec, mObjId, applyAttributeData, mProgressControls, this);
        }

        void AttributesPanelOperations.IAttributesPanel.Fill(
            AttributeRealizationInfo[] attributeRealizations)
        {
            bool shouldNotify = mIsApplyingAttribute;
            mIsApplyingAttribute = false;

            ClearAttributePanels();

            foreach (AttributeRealizationInfo attribute in attributeRealizations)
            {
                AttributePanel attributePanel = new AttributePanel(
                    mRepSpec,
                    attribute,
                    mProgressControls,
                    this,
                    mWorkspaceWindow,
                    mWindow);

                mAttributePanels.Add(attributePanel);
            }

            if (shouldNotify)
                PropertiesRefreshNotifier.Notify();

            mRepaint();
        }

        void AttributesPanelOperations.IAttributesPanel.EnableApplyAttributesButton()
        {
        }

        void AttributesPanelOperations.IAttributesPanel.DisableApplyAttributesButton()
        {
        }

        void AttributePanel.IAttributesPanel.RemovePanel(AttributePanel attributePanel)
        {
            mAttributePanels.Remove(attributePanel);
        }

        void ClearAttributePanels()
        {
            mAttributePanels.Clear();
        }

        float mLastDesiredHeight;
        float mLastValidAvailableWidth;
        Vector2 mScrollPosition;

        RepositorySpec mRepSpec;
        long mObjId;
        bool mbOpenApplyAttributeDialog;
        bool mIsApplyingAttribute;

        readonly IProgressControls mProgressControls;
        readonly List<AttributePanel> mAttributePanels = new List<AttributePanel>();

        readonly Action mRepaint;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly EditorWindow mWindow;

        const int HORIZONTAL_MARGIN = 2;
        const float MAX_HEIGHT = 90f;
    }
}
