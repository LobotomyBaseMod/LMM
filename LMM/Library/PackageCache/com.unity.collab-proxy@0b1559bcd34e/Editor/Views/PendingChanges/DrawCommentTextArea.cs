using System;

using UnityEditor;
using UnityEngine;

using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal static class DrawCommentTextArea
    {
        internal static void ForComment(
            CommentTextArea textArea,
            Action onTextAreaChanged,
            bool isOperationRunning)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space(3, false);

                using (new EditorGUILayout.VerticalScope())
                {
                    using (new GuiEnabled(!isOperationRunning))
                    {
                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.Space(4);

                        Rect availableRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandHeight(true));

                        textArea.OnGUI(
                            availableRect,
                            UnityStyles.PendingChangesTab.CommentTextArea,
                            UnityStyles.PendingChangesTab.CommentPlaceHolder);

                        if (EditorGUI.EndChangeCheck())
                            onTextAreaChanged();
                    }
                }

                EditorGUILayout.Space(3, false);
            }
        }

        internal static void ForSummary(
            SummaryTextArea textArea,
            Action onTextAreaChanged,
            bool isOperationRunning)
        {
            using (new GuiEnabled(!isOperationRunning))
            {
                EditorGUI.BeginChangeCheck();

                textArea.OnGUI(
                    UnityStyles.PendingChangesTab.SummaryTextArea,
                    UnityStyles.PendingChangesTab.SummaryPlaceHolder);

                if (EditorGUI.EndChangeCheck())
                    onTextAreaChanged();
            }
        }
    }
}
