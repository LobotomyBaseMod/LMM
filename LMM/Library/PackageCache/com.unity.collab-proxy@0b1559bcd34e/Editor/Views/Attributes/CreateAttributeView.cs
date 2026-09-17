using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Attributes;
using PlasticGui.WorkspaceWindow.QueryViews.Attributes;
using Unity.PlasticSCM.Editor.UI;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.Attributes
{
    internal class CreateAttributeView : IPlasticDialogCloser
    {
        internal CreateAttributeView(
            RepositorySpec repSpec,
            IWorkspaceWindow workspaceWindow,
            ApplyAttributeDialogOperations.IApplyAttributeDialog applyAttributeView,
            IProgressControls progressControls,
            AttributeDataDialog parentWindow)
        {
            mRepSpec = repSpec;
            mWorkspaceWindow = workspaceWindow;
            mApplyAttributeView = applyAttributeView;
            mProgressControls = progressControls;
            mParentWindow = parentWindow;
        }

        void IPlasticDialogCloser.CloseDialog()
        {
            mParentWindow.ToggleCreateAttributeView(false);

            ApplyAttributeDialogOperations.CreateAttribute(
                BuildCreationData(),
                mWorkspaceWindow,
                mProgressControls,
                mApplyAttributeView);
        }

        internal void OkButtonAction()
        {
            AttributeDataValidation.AsyncValidation(
                BuildCreationData(),
                this,
                mProgressControls);
        }

        AttributeData BuildCreationData()
        {
            return new AttributeData(
                mRepSpec,
                null,
                mAttributeName,
                mComments);
        }

        internal void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(
                        PlasticLocalization.Name.AttributeNameLabel.GetString(),
                        GUILayout.Width(HORIZONTAL_ALIGNMENT));

                    Rect nameRect = GUILayoutUtility.GetRect(
                        new GUIContent(string.Empty),
                        EditorStyles.textField,
                        GUILayout.Width(ENTRIES_WIDTH));

                    GUI.SetNextControlName(NAME_FIELD_CONTROL_NAME);
                    mAttributeName = UnityEditor.EditorGUI.TextField(nameRect, mAttributeName);

                    if (!mWasNameFieldFocused)
                    {
                        UnityEditor.EditorGUI.FocusTextInControl(NAME_FIELD_CONTROL_NAME);
                        mWasNameFieldFocused = true;
                    }
                }

                GUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(HORIZONTAL_ALIGNMENT)))
                    {
                        GUILayout.Space(49);
                        GUILayout.Label(
                            PlasticLocalization.Name.AttributeCommentLabel.GetString(),
                            GUILayout.Width(HORIZONTAL_ALIGNMENT));
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        Rect commentRect = GUILayoutUtility.GetRect(
                            new GUIContent(string.Empty),
                            EditorStyles.textArea,
                            GUILayout.Height(COMMENTS_HEIGHT),
                            GUILayout.Width(ENTRIES_WIDTH));

                        mComments = EditorGUI.ScrollableTextAreaInternal(
                            commentRect,
                            mComments,
                            ref mScrollPosition,
                            EditorStyles.textArea);

                        GUILayout.Space(5);

                        GUILayout.Label(
                            PlasticLocalization.Name.AttributeHelpLabel.GetString(),
                            UnityStyles.Dialog.MiniLabelText);
                    }
                }
            }
        }

        string mAttributeName = string.Empty;
        string mComments = string.Empty;
        Vector2 mScrollPosition;
        bool mWasNameFieldFocused;

        readonly RepositorySpec mRepSpec;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly ApplyAttributeDialogOperations.IApplyAttributeDialog mApplyAttributeView;
        readonly IProgressControls mProgressControls;
        readonly AttributeDataDialog mParentWindow;

        const int HORIZONTAL_ALIGNMENT = 100;
        const int COMMENTS_HEIGHT = 200;
        const int ENTRIES_WIDTH = 350;
        const string NAME_FIELD_CONTROL_NAME = "CreateAttributeNameField";
    }
}
