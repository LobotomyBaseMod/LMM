using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.UI;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.Branches.Dialogs
{
    class CreateBranchDialog : PlasticDialog
    {
        internal BranchInfo SelectedBranchBase { get { return mSelectedBaseBranch; } }
        internal string NewBranchName { get { return mNewBranchName; } set { mNewBranchName = value; } }
        internal bool AllowToChangeBaseBranch { get { return mAllowToChangeBaseBranch; } }

        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 710, baseRect.height);
            }
        }

        protected override string GetTitle()
        {
            return mAllowToChangeBaseBranch ?
                PlasticLocalization.Name.CreateChildBranchBasedOnTitle.GetString() :
                PlasticLocalization.Name.CreateChildBranchTitle.GetString();
        }

        protected override void DoComponentsArea()
        {
            DoTopArea();

            DoFieldsArea();
        }

        internal static BranchCreationData CreateBranchFromMainOrCurrentBranch(
            EditorWindow parentWindow,
            RepositorySpec repSpec,
            BranchInfo mainBranch,
            BranchInfo currentBranch,
            string proposedBranchName)
        {
            string changesetStr = PlasticLocalization.Name.LastChangeset.GetString();

            string explanation = BranchCreationUserInfo.GetFromObjectString(
                repSpec, currentBranch, changesetStr);

            return CreateBranch(
                parentWindow,
                repSpec,
                mainBranch,
                mainBranch,
                currentBranch,
                -1,
                explanation,
                true,
                proposedBranchName);
        }

        internal static BranchCreationData CreateBranchFromLastParentBranchChangeset(
            EditorWindow parentWindow,
            RepositorySpec repSpec,
            BranchInfo parentBranchInfo,
            string proposedBranchName)
        {
            string changesetStr = PlasticLocalization.Name.LastChangeset.GetString();

            string explanation = BranchCreationUserInfo.GetFromObjectString(
                repSpec, parentBranchInfo, changesetStr);

            return CreateBranch(
                parentWindow,
                repSpec,
                parentBranchInfo,
                null,
                null,
                -1,
                explanation,
                false,
                proposedBranchName);
        }

        internal static BranchCreationData CreateBranchFromChangeset(
            EditorWindow parentWindow,
            RepositorySpec repSpec,
            ChangesetInfo changesetInfo,
            string proposedBranchName)
        {
            BranchInfo parentBranchInfo = BranchInfoCache.GetBranch(
                repSpec, changesetInfo.BranchId);

            string changesetStr = SpecPreffix.CHANGESET + changesetInfo.ChangesetId.ToString();

            string explanation = BranchCreationUserInfo.GetFromObjectString(
                repSpec, parentBranchInfo, changesetStr);

            return CreateBranch(
                parentWindow,
                repSpec,
                parentBranchInfo,
                null,
                null,
                changesetInfo.ChangesetId,
                explanation,
                false,
                proposedBranchName);
        }

        internal static BranchCreationData CreateBranchFromLabel(
            EditorWindow parentWindow,
            RepositorySpec repSpec,
            MarkerExtendedInfo labelInfo)
        {
            BranchInfo parentBranchInfo = BranchInfoCache.GetBranch(
                repSpec, labelInfo.BranchId);

            string explanation = BranchCreationUserInfo.GetFromObjectString(
                repSpec, labelInfo);

            return CreateBranch(
                parentWindow,
                repSpec,
                parentBranchInfo,
                null,
                null,
                labelInfo.Changeset,
                explanation,
                false,
                null);
        }

        internal void CheckMainBranchRadioToggle()
        {
            mIsMainBranchSelected = true;
            mIsCurrentBranchSelected = false;
            mSelectedBaseBranch = mMainBranch;
        }

        internal void CheckCurrentBranchRadioToggle()
        {
            mIsMainBranchSelected = false;
            mIsCurrentBranchSelected = true;
            mSelectedBaseBranch = mCurrentBranch;
        }

        static BranchCreationData CreateBranch(
            EditorWindow parentWindow,
            RepositorySpec repSpec,
            BranchInfo baseBranch,
            BranchInfo mainBranch,
            BranchInfo currentBranch,
            long changesetId,
            string explanation,
            bool allowToChangeBaseBranch,
            string proposedBranchName)
        {
            CreateBranchDialog dialog = Create(
                repSpec,
                baseBranch,
                mainBranch,
                currentBranch,
                changesetId,
                explanation,
                allowToChangeBaseBranch,
                proposedBranchName);
            ResponseType dialogueResult = dialog.RunModal(parentWindow);

            BranchCreationData result = dialog.BuildCreationData();
            result.Result = dialogueResult == ResponseType.Ok;
            return result;
        }

        void DoTopArea()
        {
            if (mAllowToChangeBaseBranch)
            {
                DoBranchSelectionArea();
                return;
            }

            Paragraph(string.Format("{0} {1}",
                PlasticLocalization.Name.CreateChildBranchExplanation.GetString(), mExplanation));
        }

        void DoBranchSelectionArea()
        {
            int descriptionLabelLeftMargin = 25;

            GUILayout.BeginVertical();

            if (GUILayout.Toggle(
                mIsMainBranchSelected,
                GetBranchName.FromFullBranchName(mMainBranch.Name),
                UnityStyles.Dialog.BoldRadioToggle))
            {
                mIsMainBranchSelected = true;
                mIsCurrentBranchSelected = false;
                mSelectedBaseBranch = mMainBranch;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(descriptionLabelLeftMargin);
            GUILayout.Label(
                PlasticLocalization.Name.CreateChildBranchBasedOnMainExplanation.GetString(),
                EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            if (GUILayout.Toggle(
                    mIsCurrentBranchSelected,
                    GetBranchName.FromFullBranchName(mCurrentBranch.Name),
                    UnityStyles.Dialog.BoldRadioToggle))
            {
                mIsMainBranchSelected = false;
                mIsCurrentBranchSelected = true;
                mSelectedBaseBranch = mCurrentBranch;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(descriptionLabelLeftMargin);
            GUILayout.Label(
                PlasticLocalization.Name.CreateChildBranchBasedOnCurrentBranchExplanation.GetString(),
                EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            GUILayout.EndVertical();
        }

        void DoFieldsArea()
        {
            GUILayout.BeginVertical();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.BranchNameEntry.GetString(),
                    GUILayout.Width(100));

                Rect nameRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                GUI.SetNextControlName(NAME_FIELD_CONTROL_NAME);
                mNewBranchName = UnityEditor.EditorGUI.TextField(nameRect, mNewBranchName);

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

            mSwitchToBranch = GUILayout.Toggle(
                mSwitchToBranch,
                PlasticLocalization.Name.SwitchToBranchCheckButton.GetString());

            GUILayout.Space(5);

            GUILayout.EndVertical();
        }

        Vector2 mScrollPosition;

        internal override void OkButtonAction()
        {
            BranchCreationValidation.AsyncValidation(
                BuildCreationData(), this, mProgressControls);
        }

        internal static CreateBranchDialog Create(
            RepositorySpec repSpec,
            BranchInfo baseBranch,
            BranchInfo mainBranch,
            BranchInfo currentBranch,
            long changesetId,
            string explanation,
            bool allowToChangeBaseBranch,
            string proposedBranchName)
        {
            CreateBranchDialogAssertions.AssertValidBaseBranchChangeArguments(
                mainBranch,
                currentBranch,
                allowToChangeBaseBranch);

            if (allowToChangeBaseBranch && currentBranch.IsMainBranch())
                allowToChangeBaseBranch = false;

            var instance = CreateInstance<CreateBranchDialog>();
            instance.IsResizable = false;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.AddControlConsumingEnterKey(COMMENT_TEXTAREA_CONTROL_NAME);
            instance.mEscapeKeyAction = instance.CloseButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.CreateButton.GetString();
            instance.mRepositorySpec = repSpec;
            instance.mSelectedBaseBranch = baseBranch;
            instance.mMainBranch = mainBranch;
            instance.mCurrentBranch = currentBranch;
            instance.mAllowToChangeBaseBranch = allowToChangeBaseBranch;
            instance.mNewBranchName = proposedBranchName == null ?
                string.Empty : proposedBranchName;
            instance.mComment = "";
            instance.mSwitchToBranch = true;
            instance.mExplanation = explanation;
            instance.mChangesetId = changesetId;
            return instance;
        }

        BranchCreationData BuildCreationData()
        {
            return new BranchCreationData(
                mRepositorySpec,
                mSelectedBaseBranch,
                mChangesetId,
                mNewBranchName,
                mComment,
                null,
                mSwitchToBranch);
        }

        RepositorySpec mRepositorySpec;
        BranchInfo mSelectedBaseBranch;
        BranchInfo mMainBranch;
        BranchInfo mCurrentBranch;
        bool mAllowToChangeBaseBranch;
        long mChangesetId;

        string mNewBranchName;
        string mComment;
        bool mSwitchToBranch;
        string mExplanation;

        bool mIsMainBranchSelected = true;
        bool mIsCurrentBranchSelected;
        const string NAME_FIELD_CONTROL_NAME = "CreateBranchNameField";
        const string COMMENT_TEXTAREA_CONTROL_NAME = "CreateBranchCommentTextArea";
    }
}
