using UnityEditor;
using UnityEngine;

using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.AssetUtils
{
    internal static class RepaintEditor
    {
        internal static void InspectorWindow()
        {
            UnityEditor.Editor[] editors =
                Resources.FindObjectsOfTypeAll<UnityEditor.Editor>();

            foreach (UnityEditor.Editor editor in editors)
                editor.Repaint();
        }

        internal static void ProjectWindow()
        {
            EditorWindow projectWindow = FindEditorWindow.ProjectWindow();

            if (projectWindow == null)
                return;

            projectWindow.Repaint();
        }

        internal static void HierarchyWindow()
        {
            EditorApplication.RepaintHierarchyWindow();
        }

        internal static void AllWindowsWithOverlays()
        {
            InspectorWindow();
            ProjectWindow();
            HierarchyWindow();
        }
    }
}
