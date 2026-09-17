using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.Toolbar.PopupWindow.Operations;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow
{
    internal class UncontrolledPopupWindow : PopupWindowContent
    {
        internal UncontrolledPopupWindow(
            UncontrolledPopupOperations operations,
            Action repaintToolbar,
            Vector2 size)
        {
            mOperations = operations;
            mRepaintToolbar = repaintToolbar;
            mSize = size;
        }

        public override Vector2 GetWindowSize()
        {
            return mSize;
        }

        public override void OnClose()
        {
            RepaintToolbar();
        }

        void RepaintToolbar()
        {
            if (mRepaintToolbar == null)
                return;

            mRepaintToolbar();
        }

        public override void OnGUI(Rect rect)
        {
            if (Keyboard.IsKeyPressed(Event.current, KeyCode.Escape))
            {
                editorWindow.Close();
                return;
            }

#if UNITY_EDITOR_WIN && UNITY_6000_0_OR_NEWER && !UNITY_6000_3_OR_NEWER
            // Workaround for Unity 6.0-6.2 on Windows: close popup if mouse goes above toolbar
            if (ToolbarMouseBoundary.IsAboveToolbar(rect, Event.current.mousePosition))
            {
                editorWindow.Close();
                return;
            }
#endif

            GUILayout.Space(1);

            Rect useVersionControlRect;

            if (PopupWindowDrawing.DrawMenuItem(
                    PlasticLocalization.Name.UseUnityVersionControl.GetString(),
                    null,
                    null,
                    out useVersionControlRect))
            {
                ExecuteAndClosePopup(mOperations.ShowUVCSWindow);
                return;
            }

            Rect hideVersionControlRect;

            if (PopupWindowDrawing.DrawMenuItem(
                    PlasticLocalization.Name.HideVersionControlToolbar.GetString(),
                    Images.GetHideVersionControlIcon(),
                    null,
                    out hideVersionControlRect))
            {
                ExecuteAndClosePopup(mOperations.HideUVCSToolbarButton);
                return;
            }

            Rect delimiterRect = new Rect(
                hideVersionControlRect.x,
                hideVersionControlRect.y + hideVersionControlRect.height,
                hideVersionControlRect.width,
                PopupWindowDrawing.DELIMITER_HEIGHT);

            PopupWindowDrawing.DrawDelimiterRect(
                delimiterRect,
                UnityStyles.Colors.SplitLineColor);

            Rect settingsRect;

            if (PopupWindowDrawing.DrawMenuItem(
                    PlasticLocalization.Name.Settings.GetString(),
                    Images.GetSettingsIcon(),
                    null,
                    out settingsRect))
            {
                ExecuteAndClosePopup(mOperations.ShowUVCSSettings);
                return;
            }

            Rect settingsDelimiteRect = new Rect(
                settingsRect.x,
                settingsRect.y + settingsRect.height,
                settingsRect.width,
                PopupWindowDrawing.DELIMITER_HEIGHT);

            PopupWindowDrawing.DrawDelimiterRect(
                settingsDelimiteRect,
                UnityStyles.Colors.SplitLineColor);

            Rect learnMoreRect;

            if (PopupWindowDrawing.DrawMenuItem(
                    PlasticLocalization.Name.LearnMoreAboutUnityVersionControl.GetString(),
                    null,
                    null,
                    out learnMoreRect))
            {
                ExecuteAndClosePopup(mOperations.OpenUVCSLandingPageInBrowser);
                return;
            }

            mLastHoveredIndex = PopupWindowDrawing.RepaintWhenHoveredMenuItemChanged(
                Repaint,
                mLastHoveredIndex,
                useVersionControlRect,
                hideVersionControlRect,
                settingsRect,
                learnMoreRect);

#if UNITY_EDITOR_WIN && UNITY_6000_0_OR_NEWER && !UNITY_6000_3_OR_NEWER
            // Request continuous repaint to track mouse position even outside popup rect
            if (editorWindow != null)
                editorWindow.Repaint();
#endif
        }

        void ExecuteAndClosePopup(Action action)
        {
            editorWindow.Close();
            EditorDispatcher.Dispatch(action);
        }

        void Repaint()
        {
            if (editorWindow == null)
                return;

            editorWindow.Repaint();
        }

        readonly Action mRepaintToolbar;
        readonly UncontrolledPopupOperations mOperations;
        readonly Vector2 mSize;

        int mLastHoveredIndex = -1;
    }
}
