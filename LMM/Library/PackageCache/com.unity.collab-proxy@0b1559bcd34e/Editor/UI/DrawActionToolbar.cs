using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class DrawActionToolbar
    {
        internal static void Begin()
        {
            Rect result = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(result, UnityStyles.Colors.BarBorder);

            EditorGUILayout.BeginVertical(UnityStyles.ActionToolbar);
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
        }

        internal static void End()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
    }
}