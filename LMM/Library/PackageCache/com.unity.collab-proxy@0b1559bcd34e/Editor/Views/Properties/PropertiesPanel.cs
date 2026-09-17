using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common.Threading;

using Codice.CM.Common;
using Codice.Utils;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.Views.Attributes;

namespace Unity.PlasticSCM.Editor.Views.Properties
{
    internal class PropertiesPanel
    {
        internal PropertiesPanel(
            Action repaintAction,
            IWorkspaceWindow workspaceWindow,
            EditorWindow window)
        {
            mRepaintAction = repaintAction;

            mCommentsFoldout = SessionState.GetBool(
                COMMENTS_FOLDOUT_KEY, defaultValue: true);
            mAttributesFoldout = SessionState.GetBool(
                ATTRIBUTES_FOLDOUT_KEY, defaultValue: true);

            mCommentsPanel = new CommentsPanel(
                repaintAction);

            mAttributesPanel = new AttributesPanel(
                repaintAction,
                workspaceWindow,
                window);
        }

        internal void ClearInfo()
        {
            PersistCommentDraftIfNeeded();

            mCommentsPanel.ClearInfo();
            mAttributesPanel.Clear();

            mSelectedObject = null;
            mRepSpec = null;

            mRepaintAction();
        }

        internal void UpdateInfo(
            RepObjectInfo selectedObject,
            RepositorySpec repSpec)
        {
            PersistCommentDraftIfNeeded();

            mSelectedObject = selectedObject;
            mRepSpec = repSpec;

            mCommentsPanel.ClearInfo();
            mAttributesPanel.Clear();
            mCommentsPanel.SetComment(GetComment(mSelectedObject));

            TryRestoreCommentDraft();

            mAttributesPanel.UpdateRepositorySpec(mRepSpec);
            mAttributesPanel.UpdateInfo(mSelectedObject.Id);

            mRepaintAction();
        }

        internal void Update()
        {
            mAttributesPanel.Update();

            if (mSelectedObject == null ||
                mRepSpec == null ||
                mLastRefreshVersion == PropertiesRefreshNotifier.Version)
                return;

            // if the version is different, it means that there was
            // change that requires refreshing the properties
            mLastRefreshVersion = PropertiesRefreshNotifier.Version;
            RefreshCommentAndAttributes();
        }

        internal void OnGUI()
        {
            if (mSelectedObject == null || mRepSpec == null)
                return;

            EditorGUILayout.BeginVertical(UnityStyles.Inspector.HeaderBackgroundStyle);

            GUILayout.Space(5);

            Texture image = GetImage(mSelectedObject, mRepSpec, () =>
            {
                mRepaintAction();
            });

            string title = GetTitle(mSelectedObject);
            string description = GetDescription(mSelectedObject, mRepSpec);

            DrawPropertiesArea(
                image,
                title,
                description);

            DrawCommentsFoldoutHeader();

            if (mCommentsFoldout)
            {
                mCommentsPanel.OnGUI();
            }

            GUILayout.Space(5);

            DrawAttributesFoldoutHeader();

            if (mAttributesFoldout)
            {
                mAttributesPanel.OnGUI();
            }

            GUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }

        void DrawCommentsFoldoutHeader()
        {
            bool isEditable = IsObjectEditable();

            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                EditorStyles.foldout,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            float buttonSpacing = 4f;
            float rightPadding = 4f;
            float buttonsWidth = 0f;

            string editText = PlasticLocalization.Name.EditButton.GetString();
            string saveText = PlasticLocalization.Name.SaveButton.GetString();
            string cancelText = PlasticLocalization.Name.CancelButton.GetString();

            GUIContent editContent = new GUIContent(editText);
            GUIContent saveContent = new GUIContent(saveText);
            GUIContent cancelContent = new GUIContent(cancelText);

            Vector2 editSize = EditorStyles.miniButton.CalcSize(editContent);
            Vector2 saveSize = EditorStyles.miniButton.CalcSize(saveContent);
            Vector2 cancelSize = EditorStyles.miniButton.CalcSize(cancelContent);

            if (isEditable)
            {
                buttonsWidth = mCommentsPanel.IsEditing
                    ? saveSize.x + buttonSpacing + cancelSize.x + rightPadding
                    : editSize.x + rightPadding;
            }

            // Draw foldout in the remaining space on the left
            Rect foldoutRect = new Rect(
                headerRect.x,
                headerRect.y,
                headerRect.width - buttonsWidth,
                headerRect.height);

            bool previousCommentsFoldout = mCommentsFoldout;
            mCommentsFoldout = EditorGUI.Foldout(
                foldoutRect,
                mCommentsFoldout,
                PlasticLocalization.Name.CommentsLabelSingular.GetString(),
                toggleOnLabelClick: true);

            if (previousCommentsFoldout != mCommentsFoldout)
                SessionState.SetBool(COMMENTS_FOLDOUT_KEY, mCommentsFoldout);

            if (!isEditable)
                return;

            DrawEditableHeader(
                headerRect,
                rightPadding,
                cancelSize,
                buttonSpacing,
                saveSize,
                saveContent,
                cancelContent,
                editSize,
                editContent);
        }

        void DrawEditableHeader(Rect headerRect, float rightPadding, Vector2 cancelSize, float buttonSpacing, Vector2 saveSize,
            GUIContent saveContent, GUIContent cancelContent, Vector2 editSize, GUIContent editContent)
        {
            if (mCommentsPanel.IsEditing)
            {
                DrawEditingHeader(
                    headerRect,
                    rightPadding,
                    cancelSize,
                    buttonSpacing,
                    saveSize,
                    saveContent,
                    cancelContent);

                return;
            }

            Rect editRect = new Rect(
                headerRect.xMax - rightPadding - editSize.x,
                headerRect.y,
                editSize.x,
                headerRect.height);

            if (GUI.Button(editRect, editContent, EditorStyles.miniButton))
            {
                mCommentsPanel.EnterEditMode();
                // expand the foldout when entering edit mode
                mCommentsFoldout = true;
                SessionState.SetBool(COMMENTS_FOLDOUT_KEY, true);
            }
        }

        void DrawEditingHeader(Rect headerRect, float rightPadding, Vector2 cancelSize, float buttonSpacing, Vector2 saveSize,
            GUIContent saveContent, GUIContent cancelContent)
        {
            using (new GuiEnabled(!mIsSavingComment))
            {
                Rect saveRect = new Rect(
                    headerRect.xMax - rightPadding - cancelSize.x - buttonSpacing - saveSize.x,
                    headerRect.y,
                    saveSize.x,
                    headerRect.height);

                Rect cancelRect = new Rect(
                    headerRect.xMax - rightPadding - cancelSize.x,
                    headerRect.y,
                    cancelSize.x,
                    headerRect.height);

                if (GUI.Button(saveRect, saveContent, EditorStyles.miniButton))
                    SaveComment();

                if (GUI.Button(cancelRect, cancelContent, EditorStyles.miniButton))
                    CancelEdit();
            }
        }

        void DrawAttributesFoldoutHeader()
        {
            Rect foldoutRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                EditorStyles.foldout,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            bool previousAttributesFoldout = mAttributesFoldout;
            mAttributesFoldout = EditorGUI.Foldout(
                foldoutRect,
                mAttributesFoldout,
                PlasticLocalization.Name.Attributes.GetString(),
                toggleOnLabelClick: true);

            if (previousAttributesFoldout != mAttributesFoldout)
                SessionState.SetBool(ATTRIBUTES_FOLDOUT_KEY, mAttributesFoldout);
        }

        void SaveComment()
        {
            mIsSavingComment = true;
            string newComment = mCommentsPanel.EditedComment;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    PlasticGui.Plastic.API.UpdateObjectComment(mRepSpec, mSelectedObject, newComment);
                },
                afterOperationDelegate: delegate
                {
                    mIsSavingComment = false;

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        mRepaintAction();
                        return;
                    }

                    ClearCommentDraft();
                    mCommentsPanel.ExitEditMode(newComment);

                    PropertiesRefreshNotifier.Notify();
                    mLastRefreshVersion = PropertiesRefreshNotifier.Version;

                    mRepaintAction();
                });
        }

        void CancelEdit()
        {
            ClearCommentDraft();

            mCommentsPanel.CancelEditMode();
            mRepaintAction();
        }

        bool IsObjectEditable()
        {
            return mSelectedObject != null
                   && mSelectedObject.Id != -1
                   && mRepSpec != null;
        }

        void PersistCommentDraftIfNeeded()
        {
            if (!mCommentsPanel.IsEditing || !mCommentsPanel.IsDirty)
                return;

            if (mSelectedObject == null)
                return;

            string commentDraftKey = GetCommentDraftKey(mSelectedObject);
            SessionState.SetString(commentDraftKey, mCommentsPanel.EditedComment);
        }

        void TryRestoreCommentDraft()
        {
            if (mSelectedObject == null)
                return;

            string commentDraftKey = GetCommentDraftKey(mSelectedObject);
            string commentDraft = SessionState.GetString(commentDraftKey, null);

            if (string.IsNullOrEmpty(commentDraft))
                return;

            mCommentsPanel.SetComment(GetComment(mSelectedObject));
            mCommentsPanel.RestoreDraft(commentDraft);
            mCommentsFoldout = true;
            SessionState.SetBool(COMMENTS_FOLDOUT_KEY, true);
        }

        void ClearCommentDraft()
        {
            if (mSelectedObject == null)
                return;

            string commentDraftKey = GetCommentDraftKey(mSelectedObject);
            SessionState.EraseString(commentDraftKey);
        }

        static void DrawPropertiesArea(
            Texture image,
            string title,
            string description)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Space(7);

            DrawImage(image);

            GUILayout.Space(5);

            GUILayout.BeginVertical();

            // Get the available width
            Rect availableWidthRect = GUILayoutUtility.GetRect(
                0,
                0,
                GUILayout.ExpandWidth(true));

            DrawSelectableLabel(
                title,
                UnityStyles.PropertiesPanel.Title,
                availableWidthRect.width);

            GUILayout.Space(3);

            DrawSelectableLabel(
                description,
                UnityStyles.PropertiesPanel.Description,
                availableWidthRect.width);

            GUILayout.Space(5);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        static string GetCommentDraftKey(RepObjectInfo repObject)
        {
            return string.Format(
                COMMENT_DRAFT_KEY_FORMAT,
                repObject.GUID.ToString());
        }

        static void DrawImage(Texture image)
        {
            if (image == null)
                return;

            Rect imageRect = GUILayoutUtility.GetRect(
                IMAGE_SIZE, IMAGE_SIZE,
                GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            imageRect.y += 3;

            GUI.DrawTexture(imageRect, image, ScaleMode.ScaleToFit);
        }

        static void DrawSelectableLabel(string text, GUIStyle style, float availableWidth)
        {
            Vector2 objectNameContentSize = style.CalcSize(new GUIContent(text));
            float objectNameClampedWidth = Mathf.Min(objectNameContentSize.x, availableWidth);
            Rect objectNameRect = GUILayoutUtility.GetRect(
                objectNameClampedWidth,
                objectNameContentSize.y,
                style);
            EditorGUI.SelectableLabel(objectNameRect, text, style);
        }

        static Texture GetImage(
            RepObjectInfo selectedObject,
            RepositorySpec repSpec,
            Action avatarLoadedAction)
        {
            if (selectedObject is ChangesetInfo)
            {
                string userName = PlasticGui.Plastic.API.GetUserName(
                    repSpec.Server, selectedObject.Owner);

                Texture2D image = GetAvatar.ForEmail(userName, avatarLoadedAction);
                return image;
            }

            if (selectedObject is BranchInfo)
                return Images.GetBranchIcon();

            if (selectedObject is MarkerInfo)
                return Images.GetLabelIcon();

            return null;
        }

        static string GetTitle(RepObjectInfo selectedObject)
        {
            if (selectedObject is ChangesetInfo changeset)
            {
                if (changeset.ChangesetId == -1)
                {
                    return PlasticLocalization.Name
                        .BranchExplorerCheckoutChangesetTitle.GetString();
                }

                string objectName = changeset.ChangesetId < 0 ?
                    PlasticLocalization.Name.Shelve.GetString() :
                    PlasticLocalization.Name.Changeset.GetString();

                return string.Concat(
                    Math.Abs(changeset.ChangesetId),
                    " ",
                    "(",
                    objectName,
                    ")");
            }

            if (selectedObject is BranchInfo branch)
            {
                return string.Concat(
                    branch.Name,
                    " ",
                    "(",
                    PlasticLocalization.Name.Branch.GetString(),
                    ")");
            }

            if (selectedObject is MarkerInfo marker)
            {
                return string.Concat(
                    marker.Name,
                    " ",
                    "(",
                    PlasticLocalization.Name.Label.GetString(),
                    ")");
            }

            return null;
        }

        static string GetDescription(
            RepObjectInfo selectedObject,
            RepositorySpec repSpec)
        {
            string userName = PlasticGui.Plastic.API.GetUserName(
                repSpec.Server, selectedObject.Owner);

            string result = PlasticLocalization.Name.DiffHeaderDetailsStringFormat.GetString(
                userName,
                selectedObject.LocalTimeStamp);

            if (selectedObject is ChangesetInfo changesetInfo)
                result += " | " + ShortGuid.Get(changesetInfo.GUID.ToString());

            return result;
        }

        static string GetComment(RepObjectInfo selectedObject)
        {
            if (selectedObject == null)
                return string.Empty;

            return selectedObject.Comment;
        }

        void RefreshCommentAndAttributes()
        {
            mCommentsPanel.SetComment(GetComment(mSelectedObject));
            mAttributesPanel.Refresh();
            mRepaintAction();
        }

        readonly CommentsPanel mCommentsPanel;
        readonly AttributesPanel mAttributesPanel;
        readonly Action mRepaintAction;

        RepObjectInfo mSelectedObject;
        RepositorySpec mRepSpec;

        long mLastRefreshVersion = PropertiesRefreshNotifier.Version;
        bool mCommentsFoldout;
        bool mIsSavingComment = false;
        bool mAttributesFoldout;

        const int IMAGE_SIZE = 32;
        const string COMMENT_DRAFT_KEY_FORMAT = "CommentDraft|{0}";
        const string COMMENTS_FOLDOUT_KEY = "PropertiesPanel.CommentsFoldout";
        const string ATTRIBUTES_FOLDOUT_KEY = "PropertiesPanel.AttributesFoldout";
    }
}
