using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI.Progress
{
    internal static class DrawProgressForWindow
    {
        internal static void ForIndeterminateProgress(
            ProgressControlsForWindow.Data data)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(3);

            DoProgressSpinner(data.ProgressPercent);

            DoProgressLabel(data.ProgressMessage);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        static void DoProgressSpinner(float progressPercent)
        {
            EditorGUILayout.BeginVertical();

            GUILayout.FlexibleSpace();

            LoadingSpinner.OnGUI(progressPercent);

            GUILayout.Space(1);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }

        static void DoProgressLabel(string progressMessage)
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Space(1);

            GUILayout.FlexibleSpace();

            GUILayout.Label(progressMessage, UnityStyles.StatusBar.Label);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }
    }
}
