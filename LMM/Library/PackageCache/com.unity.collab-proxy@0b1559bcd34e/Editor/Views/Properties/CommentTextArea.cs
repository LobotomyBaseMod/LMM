using System;
using Unity.PlasticSCM.Editor.UI.UndoRedo;
using UnityEditor;
using UnityEngine;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.Properties
{
    class CommentTextArea : UndoRedoTextArea
    {
        internal CommentTextArea(Action repaint)
            : base(repaint, string.Empty) { }

        internal void OnGUI(float height)
        {
            Rect position = GUILayoutUtility.GetRect(
                GUIContent.none,
                EditorStyles.textArea,
                GUILayout.Height(height),
                GUILayout.ExpandWidth(true));

            OnGUIInternal(
                () => EditorGUI.ScrollableTextAreaInternal(position, mText, ref mScrollPosition, EditorStyles.textArea),
                () => { });
        }

        Vector2 mScrollPosition;
    }
}
