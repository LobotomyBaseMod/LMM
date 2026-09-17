using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Properties
{
    internal class CommentsPanel
    {
        internal bool IsEditing { get; private set; }
        internal string EditedComment => mCommentTextArea?.Text;
        internal bool IsDirty => IsEditing && EditedComment != mOriginalComment;

        internal CommentsPanel(
            Action repaintAction)
        {
            mRepaintAction = repaintAction;
            mCommentTextArea = new CommentTextArea(mRepaintAction);
        }

        internal void SetComment(string comment)
        {
            mComment = comment;
        }

        internal void EnterEditMode()
        {
            DoEnterEditMode(mComment);
        }

        internal void RestoreDraft(string draftText)
        {
            DoEnterEditMode(draftText);
        }

        internal void CancelEditMode()
        {
            IsEditing = false;
            mCommentTextArea.Text = null;
            mOriginalComment = null;
        }

        internal void ExitEditMode(string comment)
        {
            mComment = comment;
            IsEditing = false;
            mCommentTextArea.Text = null;
            mOriginalComment = null;
        }

        internal void ClearInfo()
        {
            mNeedsCommentScroll = false;
            mCommentScrollPosition = Vector2.zero;
            mCommentTextHeight = 0f;
            mViewAvailableWidth = 0f;
            mComment = null;

            // Reset edit state
            IsEditing = false;
            mCommentTextArea.Text = null;
            mOriginalComment = null;
        }

        internal void OnGUI()
        {
            if (mComment == null && !IsEditing)
                return;

            if (IsEditing)
            {
                DrawEditMode();
                return;
            }

            DrawReadOnlyMode();
        }

        void DrawReadOnlyMode()
        {
            GUIContent commentContent = new GUIContent(
                mComment == string.Empty ?
                    PlasticLocalization.Name.NoCommentSet.GetString() :
                    mComment);

            // Get the available width
            Rect availableWidthRect = GUILayoutUtility.GetRect(
                0,
                0,
                GUILayout.ExpandWidth(true));

            GUIStyle commentsStyle = mComment == string.Empty
                ? UnityStyles.PropertiesPanel.EmptyComment
                : UnityStyles.PropertiesPanel.Comment;

            bool needsScroll = mNeedsCommentScroll;

            // Max height is 6 lines of text
            float maxCommentHeight = commentsStyle.lineHeight * 6;

            // calculate the text height only if we have a valid width
            // so the layout pass has already happened
            if (Event.current.type == EventType.Repaint && availableWidthRect.width > 1)
            {
                mViewAvailableWidth = availableWidthRect.width;
                mCommentTextHeight = commentsStyle.CalcHeight(
                    commentContent,
                    mViewAvailableWidth);

                needsScroll = mCommentTextHeight >= maxCommentHeight;
            }

            // Use cached height, or maxHeight as fallback
            float scrollViewHeight = Mathf.Min(
                mCommentTextHeight > 0 ? mCommentTextHeight : maxCommentHeight,
                maxCommentHeight);

            if (mNeedsCommentScroll)
            {
                mCommentScrollPosition = GUILayout.BeginScrollView(
                    mCommentScrollPosition,
                    GUILayout.Height(scrollViewHeight));
            }

            Rect textRect = GUILayoutUtility.GetRect(
                commentContent,
                commentsStyle,
                GUILayout.ExpandHeight(true));

            EditorGUI.SelectableLabel(textRect, commentContent.text, commentsStyle);

            if (mNeedsCommentScroll)
                GUILayout.EndScrollView();
            else
                GUILayout.Space(3);

            if (mNeedsCommentScroll != needsScroll)
            {
                mNeedsCommentScroll = needsScroll;
                mRepaintAction();
            }
        }

        void DrawEditMode()
        {
            mCommentTextArea.OnGUI(EDIT_TEXTAREA_HEIGHT);
        }

        void DoEnterEditMode(string comment)
        {
            if (IsEditing)
                return;

            IsEditing = true;
            mOriginalComment = mComment ?? string.Empty;
            mCommentTextArea.Text = comment;
            mCommentTextArea.SetFocusAndSelectAll();
        }

        readonly Action mRepaintAction;

        string mComment;
        string mOriginalComment;

        Vector2 mCommentScrollPosition = Vector2.zero;

        bool mNeedsCommentScroll;
        float mCommentTextHeight = 0f;
        float mViewAvailableWidth = 0f;

        CommentTextArea mCommentTextArea;

        const float EDIT_TEXTAREA_HEIGHT = 90f;
    }
}
