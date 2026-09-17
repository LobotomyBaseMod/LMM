using System;

using UnityEngine;

using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;

namespace Unity.PlasticSCM.Editor.UI.UndoRedo
{
    internal class UndoRedoTextArea : UndoRedoHelper.IUndoRedoHost
    {
        internal UndoRedoTextArea(
            Action repaint,
            string text = null,
            string watermark = null)
        {
            mRepaint = repaint;
            mControlName = Guid.NewGuid().ToString();

            mUndoRedoHelper = new UndoRedoHelper(this);

            Text = text;
            WatermarkText = watermark;
        }

        internal string Text
        {
            get { return mText; }
            set
            {
                mRequireFocusReset = true;
                mText = value;
                mCaretIndex = value != null ? value.Length : 0;

                OnTextChanged();
                mRepaint();
            }
        }

        internal string WatermarkText
        {
            get { return mWatermarkText; }
            set
            {
                mWatermarkText = value;
                mRepaint();
            }
        }

        internal int CaretIndex
        {
            get { return mCaretIndex; }
        }

        internal void SetFocus()
        {
            mRequireFocusSet = true;
        }

        internal void SetFocusAndSelectAll()
        {
            mRequireFocusSet = true;
            mRequireSelectAll = true;
        }

        internal void SetCursorToLastChar()
        {
            mHasToSetCursorToLastChar = true;
        }

        UndoRedoState UndoRedoHelper.IUndoRedoHost.UndoRedoState
        {
            get { return new UndoRedoState(mText, mCaretIndex); }
            set
            {
                mText = value.Text;
                mCaretIndex = value.CaretPosition;

                TextEditor editor = GetActiveTextEditor();

                if (editor == null)
                    return;

                editor.text = value.Text;
                editor.cursorIndex = value.CaretPosition;
                editor.selectIndex = mCaretIndex;
            }
        }

        protected void OnGUIInternal(Func<string> drawTextArea, Action drawWatermark)
        {
            GUI.SetNextControlName(mControlName);

            if (mIsFocused)
                ProcessKeyPressed(mUndoRedoHelper, Event.current);

            bool isNewKeyboardFocus = CommandEvent.IsNewKeyboardFocus(Event.current);

            // When the text area has focus, it won't reflect changes to the source string.
            // To update the text programmatically, we must:
            // 1. Remove focus
            // 2. Set the new text
            // 3. Restore focus
            int keyboardControlBackup = GUIUtility.keyboardControl;

            if (mRequireFocusReset && mIsFocused)
                GUIUtility.keyboardControl = 0;

            string oldText = mText;
            string newText = drawTextArea();
            mText = newText;

            TextEditor editor = GetActiveTextEditor();

            if (isNewKeyboardFocus && editor != null)
            {
                if (mHasToSetCursorToLastChar)
                {
                    editor.cursorIndex = editor.text.Length;
                    mHasToSetCursorToLastChar = false;
                }

                editor.SelectNone();
            }

            if (Event.current.type == EventType.Repaint && mRequireFocusSet)
            {
                GUI.FocusControl(mControlName);
                mRequireFocusSet = false;
            }

            if (Event.current.type != EventType.Layout)
                mIsFocused = IsFocused(mControlName);

            if (string.IsNullOrEmpty(mText) && !string.IsNullOrEmpty(mWatermarkText))
                drawWatermark();

            if (mRequireFocusReset && mIsFocused)
                GUIUtility.keyboardControl = keyboardControlBackup;

            mRequireFocusReset = false;

            bool repaintNeeded = false;

            if (editor != null)
            {
                if (Event.current.type == EventType.Repaint && mRequireSelectAll)
                {
                    editor.SelectAll();
                    mRequireSelectAll = false;
                    repaintNeeded = true;
                }

                int oldCaretIndex = mCaretIndex;
                int newCaretIndex = editor.cursorIndex;
                mCaretIndex = newCaretIndex;

                if (oldCaretIndex != newCaretIndex && Event.current.type != EventType.Used)
                    OnCaretIndexChanged();
            }

            if (oldText != newText)
                OnTextChanged();

            if (mIsFocused)
                ProcessKeyboardShorcuts(mUndoRedoHelper, Event.current);

            if (repaintNeeded)
                mRepaint();
        }

        void OnCaretIndexChanged()
        {
            UndoRedoState state;
            if (!mUndoRedoHelper.TryGetLastState(out state))
                return;

            if (state.Text == Text)
                mUndoRedoHelper.UpdateLastState();
        }

        void OnTextChanged()
        {
            mUndoRedoHelper.Snapshot();
        }

        protected virtual void ProcessKeyPressed(UndoRedoHelper undoRedoHelper, Event e)
        {
        }

        static void ProcessKeyboardShorcuts(UndoRedoHelper undoRedoHelper, Event e)
        {
            if (IsUndoShortcutPressed(e))
            {
                e.Use();

                if (!undoRedoHelper.CanUndo)
                    return;

                undoRedoHelper.Undo();
                return;
            }

            if (IsRedoShortcutPressed(e))
            {
                e.Use();

                if (!undoRedoHelper.CanRedo)
                    return;

                undoRedoHelper.Redo();
                return;
            }
        }

        static bool IsUndoShortcutPressed(Event e)
        {
            return Keyboard.IsControlOrCommandKeyPressed(e) &&
                   Keyboard.IsKeyPressed(e, KeyCode.Z);
        }

        static bool IsRedoShortcutPressed(Event e)
        {
            if (Keyboard.IsControlOrCommandAndShiftKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.Z))
                return true;

            if (Application.platform != RuntimePlatform.WindowsEditor)
                return false;

            return Keyboard.IsControlOrCommandKeyPressed(e) &&
                   Keyboard.IsKeyPressed(e, KeyCode.Y);
        }

        protected static TextEditor GetActiveTextEditor()
        {
            return EditorGUI.activeEditor?.InternalObject;
        }

        static bool IsFocused(string controlName)
        {
            return GUI.GetNameOfFocusedControl() == controlName;
        }

        bool mRequireFocusReset;
        bool mRequireFocusSet;
        bool mRequireSelectAll;
        bool mHasToSetCursorToLastChar;
        int mCaretIndex;
        bool mIsFocused;

        protected string mText;
        protected string mWatermarkText;

        readonly string mControlName;
        readonly Action mRepaint;
        readonly UndoRedoHelper mUndoRedoHelper;
    }
}
