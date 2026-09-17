using UnityEditor;
using UnityEngine;

namespace Unity.CodeEditor
{
    internal static class TextEditorColors
    {
        internal static readonly Color DefaultText = EditorGUIUtility.isProSkin
            ? new Color(210f / 255, 210f / 255, 210f / 255)
            : new Color(9f / 255, 9f / 255, 9f / 255);

        internal static readonly Color LineNumbersForeground = EditorGUIUtility.isProSkin
            ? new Color(0.5f, 0.5f, 0.5f, 1f)
            : new Color(0.43f, 0.43f, 0.43f, 1f);

        internal static readonly Color Background = EditorGUIUtility.isProSkin
            ? new Color(55f / 255, 55f / 255, 55f / 255)
            : new Color(217f / 255, 217f / 255, 217f / 255);

        internal static readonly Color LineNumbersSeparator = EditorGUIUtility.isProSkin
            ? new Color(0.14f, 0.14f, 0.14f, 1f)
            : new Color(116f / 255, 116f / 255, 116f / 255);

        internal static readonly Color ColumnRuler = EditorGUIUtility.isProSkin
            ? new Color(0.5f, 0.5f, 0.5f, 0.35f)
            : new Color(0.5f, 0.5f, 0.5f, 0.2f);

        internal static readonly Color NonPrintableCharacter = EditorGUIUtility.isProSkin
            ? new Color(0.5f, 0.5f, 0.5f, 0.45f)
            : new Color(0.5f, 0.5f, 0.5f, 0.3f);

        internal static readonly Color Selection =
            new Color(32f / 255, 150f / 255, 243f / 255, 0.4f);

        internal static readonly Color CurrentLineBackground = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.04f)
            : new Color(0f, 0f, 0f, 0.04f);

        internal static readonly Color CurrentLineBorder = EditorGUIUtility.isProSkin
            ? new Color(0.14f, 0.14f, 0.14f, 1f)
            : new Color(0.85f, 0.85f, 0.85f, 1f);
    }
}
