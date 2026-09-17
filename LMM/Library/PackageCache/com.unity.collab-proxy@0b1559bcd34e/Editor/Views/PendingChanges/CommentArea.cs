using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Utils;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal class CommentArea
    {
        internal interface IPendingChangesTabOperations
        {
            void CheckinAction(bool isGluonMode);
            void ShelveAction(bool isGluonMode);
            void ShowShelvesView();
            void UndoChangesAction(bool isGluonMode);
            void UndoUnchanged();
            void UndoCheckoutsKeepingLocalChanges();
        }

        internal bool KeepItemsLocked => mKeepItemsLocked;

        internal CommentArea(
            IPendingChangesTabOperations pendingChangesTabOperations,
            bool isGluonMode,
            Action clearIsCommentWarningNeeded,
            Action repaintAction)
        {
            mOperations = pendingChangesTabOperations;
            mIsGluonMode = isGluonMode;
            mClearIsCommentWarningNeeded = clearIsCommentWarningNeeded;
            mRepaint = repaintAction;

            BuildComponents();

            mSummaryTextArea.Text = SessionState.GetString(
                UnityConstants.PENDING_CHANGES_CI_SUMMARY_KEY_NAME,
                string.Empty);
            mCommentTextArea.Text = SessionState.GetString(
                UnityConstants.PENDING_CHANGES_CI_COMMENTS_KEY_NAME,
                string.Empty);
        }

        internal void OnDisable()
        {
            SessionState.SetString(
                UnityConstants.PENDING_CHANGES_CI_SUMMARY_KEY_NAME,
                mSummaryTextArea.Text);
            SessionState.SetString(
                UnityConstants.PENDING_CHANGES_CI_COMMENTS_KEY_NAME,
                mCommentTextArea.Text);
        }

        internal string GetComment()
        {
            if (string.IsNullOrEmpty(mSummaryTextArea.Text))
                return mCommentTextArea.Text;

            if (string.IsNullOrEmpty(mCommentTextArea.Text))
                return mSummaryTextArea.Text;

            return mSummaryTextArea.Text + "\n" + mCommentTextArea.Text;
        }

        internal void ClearComments()
        {
            mCommentTextArea.Text = string.Empty;
            mSummaryTextArea.Text = string.Empty;

            SessionState.EraseString(UnityConstants.PENDING_CHANGES_CI_COMMENTS_KEY_NAME);
            SessionState.EraseString(UnityConstants.PENDING_CHANGES_CI_SUMMARY_KEY_NAME);

            mRepaint();
        }

        internal void OnGUI(ResolvedUser currentUser, bool isOperationRunning)
        {
            using (new EditorGUILayout.VerticalScope(UnityStyles.ToolbarBackground))
            {
                EditorGUILayout.Space(10);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space(2, false);

                    DrawUserIcon.ForPendingChangesTab(
                        currentUser,
                        mRepaint);

                    EditorGUILayout.Space(2, false);

                    DrawCommentTextArea.ForSummary(
                        mSummaryTextArea,
                        mClearIsCommentWarningNeeded,
                        isOperationRunning);

                    EditorGUILayout.Space(3, false);
                }

                DrawCommentTextArea.ForComment(
                    mCommentTextArea,
                    mClearIsCommentWarningNeeded,
                    isOperationRunning);

                EditorGUILayout.Space(2, false);

                DoOperationsToolbar(
                    mIsGluonMode,
                    mShelveDropdownMenu,
                    mUndoDropdownMenu,
                    isOperationRunning,
                    mLastValidWidth);

                EditorGUILayout.Space(10);
            }

            if (Event.current.type == EventType.Repaint)
                mLastValidWidth = GUILayoutUtility.GetLastRect().width;
        }

        void BuildComponents()
        {
            mCommentTextArea = new CommentTextArea(
                mRepaint,
                () =>
                {
                    mSummaryTextArea.SetFocus();
                    mSummaryTextArea.SetCursorToLastChar();
                },
                string.Empty,
                PlasticLocalization.Name.CheckinComment.GetString());

            mSummaryTextArea = new SummaryTextArea(
                mRepaint,
                () => { mCommentTextArea.SetFocus(); },
                string.Empty,
                "Summary (optional)");

            mShelveDropdownMenu = new GenericMenu();
            mShelveDropdownMenu.AddItem(
                new GUIContent(PlasticLocalization.Name.ShowShelvesButton.GetString()),
                false,
                () => mOperations.ShowShelvesView());

            mUndoDropdownMenu = new GenericMenu();
            mUndoDropdownMenu.AddItem(
                new GUIContent(PlasticLocalization.Name.UndoUnchangedButton.GetString()),
                false,
                mOperations.UndoUnchanged);

#if UNITY_2021_3
            // Workaround for Unity 2021.3.x IMGUI bug on macOS: The popup menu positioning algorithm
            // incorrectly sets Y coordinate to zero when the menu's bottom edge is positioned near
            // the Dock in maximized windows, causing the popup to render at the top of the display.
            // Adding a separator alters the menu height, preventing the positioning algorithm from
            // triggering this edge case.
            if (PlatformIdentifier.IsMac())
                mUndoDropdownMenu.AddSeparator(string.Empty);
#endif

            mUndoDropdownMenu.AddItem(
                new GUIContent(PlasticLocalization.Name.UndoCheckoutsKeepingChanges.GetString()),
                false,
                mOperations.UndoCheckoutsKeepingLocalChanges);

        }

        void DoOperationsToolbar(
            bool isGluonMode,
            GenericMenu shelveDropdownMenu,
            GenericMenu undoDropdownMenu,
            bool isOperationRunning,
            float availableWidth)
        {
            bool drawVertically = availableWidth < MIN_WIDTH_FOR_HORIZONTAL_LAYOUT;
            float buttonWidth;

            if (drawVertically)
            {
                buttonWidth = availableWidth - 6;
                EditorGUILayout.BeginVertical();
            }
            else
            {
                buttonWidth = (availableWidth - 6) / 3 - 3;
                EditorGUILayout.Space(6, false);
                EditorGUILayout.BeginHorizontal();
            }

            string checkinButtonText = PlasticLocalization.Name.Checkin.GetString();
            string shelveButtonText = PlasticLocalization.Name.Shelve.GetString();
            string undoButtonText = PlasticLocalization.Name.UndoChanges.GetString();

            using (new GuiEnabled(!isOperationRunning))
            {
                if (DrawActionButton.ForCommentSection(
                        checkinButtonText,
                        buttonWidth,
                        UnityStyles.PendingChangesTab.CheckinButton))
                {
                    mOperations.CheckinAction(isGluonMode);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawActionButtonWithMenu.ForCommentsSection(
                        shelveButtonText,
                        buttonWidth,
                        () => mOperations.ShelveAction(isGluonMode),
                        shelveDropdownMenu);
                }

                DoUndoButton(isGluonMode, buttonWidth, undoButtonText, undoDropdownMenu);
            }

            if (drawVertically)
                EditorGUILayout.EndVertical();
            else
                EditorGUILayout.EndHorizontal();

            if (!isGluonMode)
                return;

            mKeepItemsLocked = EditorGUILayout.ToggleLeft(
                PlasticLocalization.Name.KeepLocked.GetString(),
                mKeepItemsLocked,
                GUILayout.Width(UnityConstants.EXTRA_LARGE_BUTTON_WIDTH));
        }

        void DoUndoButton(
            bool isGluonMode,
            float width,
            string undoButtonText,
            GenericMenu undoDropdownMenu)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (isGluonMode)
                {
                    if (DrawActionButton.ForCommentSection(
                            undoButtonText,
                            width,
                            UnityStyles.PendingChangesTab.ActionButton))
                        mOperations.UndoChangesAction(true);

                    return;
                }

                DrawActionButtonWithMenu.ForCommentsSection(
                    undoButtonText,
                    width,
                    () => mOperations.UndoChangesAction(false),
                    undoDropdownMenu);
            }
        }

        bool mKeepItemsLocked;
        float mLastValidWidth;

        CommentTextArea mCommentTextArea;
        SummaryTextArea mSummaryTextArea;
        GenericMenu mShelveDropdownMenu;
        GenericMenu mUndoDropdownMenu;

        readonly IPendingChangesTabOperations mOperations;
        readonly bool mIsGluonMode;
        readonly Action mClearIsCommentWarningNeeded;
        readonly Action mRepaint;

        const int MIN_WIDTH_FOR_HORIZONTAL_LAYOUT = 250;
    }
}
