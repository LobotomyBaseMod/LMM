using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Labels;
using Unity.PlasticSCM.Editor.UI;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.Labels.Dialogs
{
    internal class CreateLabelView : IPlasticDialogCloser
    {
        internal CreateLabelView(CreateLabelDialog parentWindow, RepositorySpec repSpec)
        {
            mParentWindow = parentWindow;
            mRepositorySpec = repSpec;
            mNewLabelName = "";
            mComment = "";
            mLabelAllXlinkedRepositories = false;
            mSwitchToLabel = false;
        }

        internal void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.LabelNameEntry.GetString(),
                    GUILayout.Width(120));

                Rect nameRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                GUI.SetNextControlName(NAME_FIELD_CONTROL_NAME);
                mNewLabelName = GUI.TextField(nameRect, mNewLabelName);

                if (!mWasNameFieldFocused)
                {
                    UnityEditor.EditorGUI.FocusTextInControl(NAME_FIELD_CONTROL_NAME);
                    mWasNameFieldFocused = true;
                }
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(100)))
                {
                    GUILayout.Space(49);
                    GUILayout.Label(
                        PlasticLocalization.Name.CommentsEntry.GetString(),
                        GUILayout.Width(120));
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

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.ChangesetToLabelEntry.GetString(),
                    GUILayout.Width(120));

                GUILayout.Label(mChangesetId, GUILayout.Width(450));

                if (GUILayout.Button(
                        PlasticLocalization.Name.ChooseMessage.GetString(), EditorStyles.miniButton))
                {
                    DoChooseButton();
                }
            }

            GUILayout.Space(15);

            mLabelAllXlinkedRepositories = GUILayout.Toggle(mLabelAllXlinkedRepositories,
                PlasticLocalization.Name.LabelAllXlinksCheckButton.GetString());

            GUILayout.Space(5);

            mSwitchToLabel = GUILayout.Toggle(mSwitchToLabel,
                PlasticLocalization.Name.SwitchToLabelCheckButton.GetString());
        }

        void DoChooseButton()
        {
            mParentWindow.ToggleChangesetExplorer(true);
        }

        internal LabelCreationData BuildCreationData()
        {
            mResultData = new LabelCreationData(
                mRepositorySpec,
                long.Parse(mChangesetId),
                mNewLabelName,
                mComment,
                mSwitchToLabel,
                mLabelAllXlinkedRepositories,
                (mResultData != null) ? mResultData.XlinksToLabel : null);

            return mResultData;
        }

        void IPlasticDialogCloser.CloseDialog()
        {
            mParentWindow.ToggleChangesetExplorer(false);
        }

        internal void SetChangesetId(long changesetId)
        {
            mChangesetId = changesetId.ToString();
        }

        LabelCreationData mResultData = null;

        string mNewLabelName;
        string mComment;
        string mChangesetId;

        bool mLabelAllXlinkedRepositories;
        bool mSwitchToLabel;
        bool mWasNameFieldFocused;

        Vector2 mScrollPosition;
        readonly CreateLabelDialog mParentWindow;
        readonly RepositorySpec mRepositorySpec;

        const string NAME_FIELD_CONTROL_NAME = "CreateLabelNameField";
        const string COMMENT_TEXTAREA_CONTROL_NAME = "CreateLabelCommentTextArea";
    }
}
