using System;

using UnityEditor;
using UnityEngine;

using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UndoRedo;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal class CommentTextArea : UndoRedoTextArea
    {
        internal CommentTextArea(
            Action repaint,
            Action focusSummaryAction,
            string text = null,
            string watermark = null) : base(repaint, text, watermark)
        {
            mFocusSummaryAction = focusSummaryAction;
        }

        protected override void ProcessKeyPressed(UndoRedoHelper undoRedoHelper, Event e)
        {
            TextEditor editor = GetActiveTextEditor();

            if (editor == null)
                return;

            if (ShouldFocusSummary(editor, e))
            {
                e.Use();
                mFocusSummaryAction();
            }
        }

        internal void OnGUI(Rect position, GUIStyle style, GUIStyle watermarkStyle)
        {
            OnGUIInternal(
                () => EditorGUI.ScrollableTextAreaInternal(position, mText, ref mScrollPosition, style),
                () => EditorGUI.LabelField(position, mWatermarkText, watermarkStyle));
        }

        static bool ShouldFocusSummary(TextEditor editor, Event e)
        {
            if (IsUpArrowPressedAtFirstLine(editor, e))
                return true;

            if (IsBackspacePressedAtStart(editor, e))
                return true;

            if (IsLeftArrowPressedAtStart(editor, e))
                return true;

            return false;
        }

        static bool IsUpArrowPressedAtFirstLine(TextEditor editor, Event e)
        {
            return Keyboard.IsKeyPressed(e, KeyCode.UpArrow)
                   && !Keyboard.HasShiftModifier(e)
                   && editor.graphicalCursorPos.y - GetCommentAreaTopPadding() == 0;
        }

        static bool IsBackspacePressedAtStart(TextEditor editor, Event e)
        {
            return Keyboard.IsKeyPressed(e, KeyCode.Backspace)
                   && editor.cursorIndex == 0
                   && !editor.hasSelection;
        }

        static bool IsLeftArrowPressedAtStart(TextEditor editor, Event e)
        {
            return Keyboard.IsKeyPressed(e, KeyCode.LeftArrow)
                   && !Keyboard.HasShiftModifier(e)
                   && editor.cursorIndex == 0;
        }

        static float GetCommentAreaTopPadding()
        {
            return ((GUIStyle)UnityStyles.PendingChangesTab.CommentTextArea).padding.top;
        }

        Vector2 mScrollPosition;

        readonly Action mFocusSummaryAction;
    }
}
