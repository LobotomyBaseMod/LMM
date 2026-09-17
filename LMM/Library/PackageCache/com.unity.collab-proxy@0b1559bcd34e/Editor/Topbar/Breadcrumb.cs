using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Topbar
{
    internal static class Breadcrumb
    {
        internal static void DoBreadcrumb(
            string workingObjectName,
            string workingObjectFullSpec,
            string workingObjectComment,
            string workingObjectType)
        {
            if (string.IsNullOrEmpty(workingObjectName))
                return;

            string tooltip = workingObjectFullSpec;
            if (!string.IsNullOrEmpty(workingObjectComment))
                tooltip += Environment.NewLine + workingObjectComment;

            GUIContent labelContent = new GUIContent(workingObjectName, tooltip);

            int breadcrumbPadding = 6;

            Rect iconRect = GUILayoutUtility.GetRect(
                UnityConstants.STATUS_BAR_ICON_SIZE,
                UnityConstants.STATUS_BAR_ICON_SIZE,
                GUILayout.ExpandWidth(false));
            iconRect.y += (EditorStyles.toolbar.fixedHeight - UnityConstants.STATUS_BAR_ICON_SIZE) / 2;

            Rect textRect = GUILayoutUtility.GetRect(labelContent, UnityStyles.BreadcrumbBar.Text);
            Rect breadcrumbBackgroundRect = new Rect(
                0,
                1,
                textRect.x + textRect.width + breadcrumbPadding,
                EditorStyles.toolbar.fixedHeight - 1);

            Color breadcrumbBgColor = UnityStyles.Colors.ToolbarBackground;

            EditorGUI.DrawRect(breadcrumbBackgroundRect, breadcrumbBgColor);

            GUI.DrawTexture(
                iconRect,
                GetWorkingObjectIcon(workingObjectType),
                ScaleMode.ScaleToFit);

            GUI.Label(textRect, labelContent, UnityStyles.BreadcrumbBar.Text);

            // Handle right-click context menu on the label
            if (Event.current.type == UnityEngine.EventType.ContextClick &&
                textRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent(PlasticLocalization.Name.Copy.GetString()), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = workingObjectFullSpec;
                });
                menu.ShowAsContext();
                Event.current.Use();
            }

            int arrowWidth = 10;

            DrawArrow(new Rect(
                    breadcrumbBackgroundRect.x + breadcrumbBackgroundRect.width,
                    breadcrumbBackgroundRect.y,
                    arrowWidth,
                    EditorStyles.toolbar.fixedHeight),
                breadcrumbBgColor,
                UnityStyles.Colors.BarBorder);

            GUILayout.Space(arrowWidth);
        }

        static Texture GetWorkingObjectIcon(string workingObjectType)
        {
            if (workingObjectType == PlasticLocalization.Name.Changeset.GetString())
                return Images.GetChangesetsIcon();

            if (workingObjectType == PlasticLocalization.Name.Label.GetString())
                return Images.GetLabelIcon();

            return Images.GetBranchIcon();
        }

        static void DrawArrow(
            Rect rect,
            Color backgroundColor,
            Color foregroundColor)
        {
            Vector2 topLeft = new Vector2(rect.x, rect.y);
            Vector2 bottomLeft = new Vector2(rect.x, rect.y + rect.height);
            Vector2 middleRight = new Vector2(rect.x + rect.width, rect.y + rect.height / 2);

            if (Event.current.type == EventType.Repaint)
            {
                GUI.BeginClip(rect);
                GL.PushMatrix();
                GL.LoadPixelMatrix();

                GL.Begin(GL.TRIANGLES);
                GL.Color(backgroundColor);
                GL.Vertex3(topLeft.x - rect.x, topLeft.y, 0);
                GL.Vertex3(bottomLeft.x - rect.x, bottomLeft.y, 0);
                GL.Vertex3(middleRight.x - rect.x, middleRight.y, 0);
                GL.End();

                GL.PopMatrix();
                GUI.EndClip();
            }

            Handles.BeginGUI();
            Handles.color = foregroundColor;
            Handles.DrawAAPolyLine(2f, topLeft, middleRight);
            Handles.DrawAAPolyLine(2f, bottomLeft, middleRight);
            Handles.EndGUI();
        }
    }
}
