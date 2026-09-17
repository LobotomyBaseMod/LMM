using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.QueryViews.Changesets;
using Unity.PlasticSCM.Editor.UI;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.Changesets.Dialogs
{
    class LabelChangesetDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 710, 290);
            }
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.CreateLabelTitle.GetString();
        }

        protected override string GetExplanation()
        {
            return mExplanation;
        }

        protected override void DoComponentsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.LabelNameEntry.GetString(),
                    GUILayout.Width(100));

                Rect nameRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                GUI.SetNextControlName(NAME_FIELD_CONTROL_NAME);
                mLabelName = UnityEditor.EditorGUI.TextField(nameRect, mLabelName);

                FocusTextAreaIfNeeded(NAME_FIELD_CONTROL_NAME);
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(100)))
                {
                    GUILayout.Space(49);
                    GUILayout.Label(
                        PlasticLocalization.Name.CommentsEntry.GetString(),
                        GUILayout.Width(100));
                }

                Rect commentRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textArea,
                    GUILayout.Height(100),
                    GUILayout.ExpandWidth(true));

                GUI.SetNextControlName(COMMENT_TEXTAREA_CONTROL_NAME);
                mComment = EditorGUI.ScrollableTextAreaInternal(
                    commentRect,
                    mComment,
                    ref mScrollPosition,
                    EditorStyles.textArea);
            }

            GUILayout.Space(15);

            mLabelAllXlinkedRepositories = GUILayout.Toggle(mLabelAllXlinkedRepositories,
                PlasticLocalization.Name.LabelAllXlinksCheckButton.GetString());
        }

        internal static ChangesetLabelData Label(
            EditorWindow parentWindow,
            RepositorySpec repSpec,
            ChangesetInfo changesetInfo)
        {
            BranchInfo parentBranchInfo = BranchInfoCache.GetBranch(
                repSpec, changesetInfo.BranchId);
            string explanation = ChangesetLabelUserInfo.GetExplanation(
                repSpec, parentBranchInfo.BranchName, changesetInfo);

            LabelChangesetDialog dialog = Create(
                repSpec, changesetInfo, explanation);

            ResponseType dialogueResult = dialog.RunModal(parentWindow);
            ChangesetLabelData result = dialog.BuildCreationData();
            result.Result = dialogueResult == ResponseType.Ok;
            return result;
        }

        internal override void OkButtonAction()
        {
            ChangesetLabelValidation.AsyncValidation(
                BuildCreationData(), this, mProgressControls);
        }

        static LabelChangesetDialog Create(
            RepositorySpec repSpec, ChangesetInfo changesetInfo, string explanation)
        {
            var instance = CreateInstance<LabelChangesetDialog>();
            instance.IsResizable = false;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.AddControlConsumingEnterKey(COMMENT_TEXTAREA_CONTROL_NAME);
            instance.mEscapeKeyAction = instance.CloseButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.LabelButton.GetString();
            instance.mRepositorySpec = repSpec;
            instance.mLabelName = "";
            instance.mComment = "";
            instance.mLabelAllXlinkedRepositories = false;
            instance.mExplanation = explanation;
            instance.mChangesetId = changesetInfo.ChangesetId;
            return instance;
        }
        Vector2 mScrollPosition;

        ChangesetLabelData BuildCreationData()
        {
            mResultData = new ChangesetLabelData(
                mRepositorySpec,
                mLabelName,
                mChangesetId,
                mComment,
                mLabelAllXlinkedRepositories,
                (mResultData != null) ? mResultData.XlinksToLabel : null);

            return mResultData;
        }
        ChangesetLabelData mResultData = null;

        RepositorySpec mRepositorySpec;
        long mChangesetId;

        string mLabelName;
        string mComment;
        bool mLabelAllXlinkedRepositories;
        string mExplanation;

        const string NAME_FIELD_CONTROL_NAME = "LabelChangesetNameField";
        const string COMMENT_TEXTAREA_CONTROL_NAME = "LabelChangesetCommentTextArea";
    }
}
