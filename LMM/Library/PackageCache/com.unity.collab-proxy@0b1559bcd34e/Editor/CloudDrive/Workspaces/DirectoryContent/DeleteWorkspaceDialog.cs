using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal class DeleteWorkspaceDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 500, 200);
            }
        }

        internal static bool UserConfirmsDelete(
            string workspaceName,
            EditorWindow parentWindow)
        {
            DeleteWorkspaceDialog dialog = Create(workspaceName);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            return dialogResult == ResponseType.Ok;
        }

        static DeleteWorkspaceDialog Create(string workspaceName)
        {
            var instance = CreateInstance<DeleteWorkspaceDialog>();

            instance.mWorkspaceName = workspaceName;
            instance.mConfirmationText = string.Empty;

            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;

            return instance;
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.DeleteCloudDriveTitle.GetString();
        }

        protected override void DoComponentsArea()
        {
            DoWarningArea();

            GUILayout.Space(10f);

            DoConfirmationArea();
        }

        void DoWarningArea()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(
                    string.Format(PlasticLocalization.Name.DeleteCloudDriveWarningText.GetString(), mWorkspaceName),
                    EditorStyles.wordWrappedLabel);
            }
        }

        void DoConfirmationArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string confirmText = PlasticLocalization.Name.DeleteCloudDriveConfirmText.GetString();
                EditorGUILayout.LabelField(confirmText, GUILayout.Width(220));

                GUILayout.Space(10f);

                GUI.SetNextControlName(CONFIRMATION_TEXTAREA_NAME);

                mConfirmationText = GUILayout.TextField(
                    mConfirmationText,
                    GUILayout.ExpandWidth(true));

                FocusTextAreaIfNeeded(CONFIRMATION_TEXTAREA_NAME);
            }
        }

        protected override void DoOkButton()
        {
            using (new GuiEnabled(mWorkspaceName == mConfirmationText))
            {
                if (!NormalButton(PlasticLocalization.Name.DeleteButton.GetString()))
                    return;

                OkButtonAction();
            }
        }

        internal override void OkButtonAction()
        {
            if (mWorkspaceName != mConfirmationText)
                return;

            base.OkButtonAction();
        }

        string mWorkspaceName;
        string mConfirmationText;

        const string CONFIRMATION_TEXTAREA_NAME = "confirmation_textarea";
    }
}
