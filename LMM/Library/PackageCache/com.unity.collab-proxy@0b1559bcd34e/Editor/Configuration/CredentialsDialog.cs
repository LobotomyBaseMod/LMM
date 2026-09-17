using UnityEngine;

using UnityEditor;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Codice.Client.Common.Connection;
using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Home;

namespace Unity.PlasticSCM.Editor.Configuration
{
    internal class CredentialsDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 525, 250);
            }
        }

        internal static AskCredentialsToUser.DialogData RequestCredentials(
            string server,
            SEIDWorkingMode seidWorkingMode,
            EditorWindow parentWindow)
        {
            CredentialsDialog dialog = Create(server, seidWorkingMode);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            return dialog.BuildCredentialsDialogData(dialogResult);
        }

        protected override void DoComponentsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.UserName.GetString(),
                    GUILayout.Width(80));

                Rect userRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mUser = EditorGUI.TextField(userRect, mUser);
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.Password.GetString(),
                    GUILayout.Width(80));

                Rect passwordRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mPassword = EditorGUI.PasswordField(passwordRect, mPassword);
            }

            GUILayout.Space(10);

            mSaveProfile = GUILayout.Toggle(
                mSaveProfile, PlasticLocalization.Name.RememberCredentialsAsProfile.GetString());
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.CredentialsDialogTitle.GetString();
        }

        protected override string GetExplanation()
        {
            return PlasticLocalization.Name.CredentialsDialogExplanation.GetString(mOrganizationInfo.DisplayName);
        }

        AskCredentialsToUser.DialogData BuildCredentialsDialogData(
            ResponseType dialogResult)
        {
            return dialogResult == ResponseType.Ok
                ? AskCredentialsToUser.DialogData.Success(
                    new Credentials(
                        new SEID(mUser, false, mPassword),
                        mSeidWorkingMode))
                : AskCredentialsToUser.DialogData.Failure(mSeidWorkingMode);
        }

        internal override void OkButtonAction()
        {
            CredentialsDialogValidation.Validate(mUser, mPassword, this, mProgressControls);
        }

        static CredentialsDialog Create(string server, SEIDWorkingMode seidWorkingMode)
        {
            var instance = CreateInstance<CredentialsDialog>();
            instance.mOrganizationInfo = OrganizationsInformation.FromServer(server);
            instance.mSeidWorkingMode = seidWorkingMode;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        string mUser;
        string mPassword = string.Empty;

        bool mSaveProfile;

        OrganizationInfo mOrganizationInfo;
        SEIDWorkingMode mSeidWorkingMode;
    }
}
