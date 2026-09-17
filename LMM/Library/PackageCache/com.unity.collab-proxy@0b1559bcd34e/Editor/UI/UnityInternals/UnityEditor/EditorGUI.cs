using System.Reflection;

using UnityEngine;
using UnityEngine.Internal;

using UnityEditorGUI = UnityEditor.EditorGUI;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class EditorGUI
    {
        // Delegate that matches the signature of ScrollableTextAreaInternal
        internal delegate string ScrollableTextAreaInternalDelegate(
            Rect position,
            string text,
            ref Vector2 scrollPosition,
            GUIStyle style);

        // This will be set by Unity.Cloud.Collaborate assembly
        internal static ScrollableTextAreaInternalDelegate ScrollableTextAreaInternal { get; set; }

        internal delegate void DrawOutlineDelegate(Rect rect, float size, Color color);

        internal static DrawOutlineDelegate DrawOutline { get; set; }

        internal static RecycledTextEditor activeEditor
        {
            get
            {
                FieldInfo activeEditorField = typeof(UnityEditorGUI).GetField(
                    "activeEditor",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (activeEditorField == null)
                    return null;

                object activeEditor = activeEditorField.GetValue(null);

                if (activeEditor == null)
                    return null;

                return new RecycledTextEditor(activeEditor as TextEditor);
            }
        }

        internal static void LabelField(
            Rect position, string label, [DefaultValue("EditorStyles.label")] GUIStyle style)
        {
            UnityEditorGUI.LabelField(position, label, style);
        }

        internal class RecycledTextEditor
        {
            internal TextEditor InternalObject;

            internal RecycledTextEditor(TextEditor recycledTextEditor)
            {
                InternalObject = recycledTextEditor;
            }

            internal void EndEditing() => InternalEndEditing(this);

            internal delegate void EndEditingDelegate(RecycledTextEditor recycledTextEditor);

            internal static EndEditingDelegate InternalEndEditing { get; set; }
        }
    }
}
