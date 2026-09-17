using System;

using UnityEngine;
using UnityEditor;

using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UndoRedo;

namespace Unity.PlasticSCM.Editor.Views
{
    internal class SummaryTextArea : UndoRedoTextArea
    {
        internal SummaryTextArea(
            Action repaint,
            Action focusCommentAction,
            string text = null,
            string watermark = null) : base(repaint, text, watermark)
        {
            mFocusCommentAction = focusCommentAction;
        }

        protected override void ProcessKeyPressed(UndoRedoHelper undoRedoHelper, Event e)
        {
            if (ShouldFocusComment(e))
            {
                e.Use();
                mFocusCommentAction();
            }
        }

        internal void OnGUI(GUIStyle style, GUIStyle watermarkStyle)
        {
            Rect position = GUILayoutUtility.GetRect(
                0, style.fixedHeight, GUILayout.ExpandWidth(true));

            OnGUIInternal(
                () => EditorGUI.TextField(position, mText, style),
                () => EditorGUI.LabelField(position, mWatermarkText, watermarkStyle));
        }

        static bool ShouldFocusComment(Event e)
        {
            if (IsDownArrowPressed(e))
                return true;

            if (IsRightArrowPressedAtEnd(e))
                return true;

            if (IsTabPressed(e))
                return true;

            if (IsEnterPressed(e))
                return true;

            return false;
        }

        static bool IsDownArrowPressed(Event e)
        {
            return Keyboard.IsKeyPressed(e, KeyCode.DownArrow)
                   && !Keyboard.HasShiftModifier(e);
        }

        static bool IsRightArrowPressedAtEnd(Event e)
        {
            if (!Keyboard.IsKeyPressed(e, KeyCode.RightArrow))
                return false;

            if (Keyboard.HasShiftModifier(e))
                return false;

            TextEditor editor = GetActiveTextEditor();

            if (editor == null)
                return false;

            return editor.cursorIndex == editor.text.Length;
        }

        static bool IsTabPressed(Event e)
        {
            return Keyboard.IsCharacterPressed(e, '\t');
        }

        static bool IsEnterPressed(Event e)
        {
            return Keyboard.IsCharacterPressed(e, '\n');
        }

        readonly Action mFocusCommentAction;
    }
}
