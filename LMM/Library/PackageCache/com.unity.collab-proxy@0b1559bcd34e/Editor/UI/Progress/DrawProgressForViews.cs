using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI.Progress
{
    internal static class DrawProgressForViews
    {
        internal static void ForNotificationArea(
            ProgressControlsForViews.Data data)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.HelpBox(
                data.NotificationMessage,
                data.NotificationType);

            EditorGUILayout.EndHorizontal();
        }

        internal static void ForDeterminateProgressBar(
            ProgressControlsForViews.Data data)
        {
            ForProgressBar(data, true);
        }

        static void ForProgressBar(ProgressControlsForViews.Data data, bool showPercentage)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(10);

            DoProgressBar(data.ProgressPercent);

            GUILayout.Space(3);

            DoProgressBarLabel(data, showPercentage);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        internal static void ForIndeterminateProgressSpinner(
            ProgressControlsForViews.Data data)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(10);

            DoProgressSpinner(data.ProgressPercent);

            DoProgressSpinnerLabel(data.ProgressMessage);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        static void DoProgressBar(float progressPercent)
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(22));

            GUILayout.FlexibleSpace();

            Rect progressRect = GUILayoutUtility.GetRect(30, 10);

            EditorGUI.ProgressBar(progressRect, progressPercent, string.Empty);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }

        static void DoProgressSpinner(float progressPercent)
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(22));

            GUILayout.FlexibleSpace();

            LoadingSpinner.OnGUI(progressPercent);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }

        static void DoProgressBarLabel(ProgressControlsForViews.Data data, bool showPercentage)
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(22));

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                showPercentage ?
                    string.Format("{0} ({1}%)", data.ProgressMessage, (int)(data.ProgressPercent * 100)) :
                    data.ProgressMessage,
                UnityStyles.ProgressLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }

        static void DoProgressSpinnerLabel(string progressMessage)
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(EditorGUIUtility.singleLineHeight));

            GUILayout.FlexibleSpace();

            GUILayout.Space(1);

            GUILayout.Label(progressMessage, UnityStyles.ProgressLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }
    }
}
