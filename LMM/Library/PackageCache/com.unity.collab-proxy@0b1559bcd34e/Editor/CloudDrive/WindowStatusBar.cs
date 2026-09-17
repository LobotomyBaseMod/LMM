using UnityEditor;
using UnityEngine;

using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.CloudDrive
{
    internal static class WindowStatusBar
    {
        internal static void OnGUI(Rect rect, ProgressControlsForWindow.Data progressData)
        {
            EditorGUI.DrawRect(rect, UnityStyles.Colors.BackgroundBar);

            DoContentArea(rect, progressData);
        }

        static void DoContentArea(Rect rect, ProgressControlsForWindow.Data progressData)
        {
            GUILayout.BeginArea(rect);

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (progressData.IsOperationRunning)
                    DrawProgressForWindow.ForIndeterminateProgress(progressData);
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndArea();
        }
    }
}
