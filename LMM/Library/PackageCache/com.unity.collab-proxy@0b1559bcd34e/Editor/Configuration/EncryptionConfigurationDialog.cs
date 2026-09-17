using UnityEditor;
using UnityEngine;

using Codice.Utils;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Home;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Configuration
{
    internal class EncryptionConfigurationDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 650, 425);
            }
        }

        internal static EncryptionConfigurationDialogData RequestEncryptionPassword(
            string server,
            EditorWindow parentWindow)
        {
            EncryptionConfigurationDialog dialog = Create(server);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            EncryptionConfigurationDialogData result =
                dialog.BuildEncryptionConfigurationData();

            result.Result = dialogResult == ResponseType.Ok;
            return result;
        }

        protected override void DoComponentsArea()
        {
            DoPasswordArea();

            Paragraph(PlasticLocalization.Name.EncryptionConfigurationRemarks.GetString(mOrganizationInfo.DisplayName));
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.EncryptionConfiguration.GetString();
        }

        protected override string GetExplanation()
        {
            return PlasticLocalization.Name.EncryptionConfigurationExplanation.GetString(mOrganizationInfo.DisplayName);
        }

        EncryptionConfigurationDialogData BuildEncryptionConfigurationData()
        {
            return new EncryptionConfigurationDialogData(
                CryptoServices.GetEncryptedPassword(mPassword.Trim()));
        }

        void DoPasswordArea()
        {
            Paragraph(PlasticLocalization.Name.EncryptionConfigurationEnterPassword.GetString());

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.Password.GetString(),
                    GUILayout.Width(120));

                Rect passwordRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mPassword = EditorGUI.PasswordField(passwordRect, mPassword);
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.RetypePassword.GetString(),
                    GUILayout.Width(120));

                Rect retypePasswordRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mRetypePassword = EditorGUI.PasswordField(retypePasswordRect, mRetypePassword);
            }

            GUILayout.Space(18f);
        }

        internal override void OkButtonAction()
        {
            if (IsValidPassword(
                    mPassword.Trim(), mRetypePassword.Trim(),
                    out mProgressControls.ProgressData.StatusMessage))
            {
                mProgressControls.ProgressData.StatusMessage = string.Empty;
                base.OkButtonAction();
                return;
            }

            mPassword = string.Empty;
            mRetypePassword = string.Empty;
        }

        static bool IsValidPassword(
            string password, string retypePassword,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = PlasticLocalization.Name.InvalidEmptyPassword.GetString();
                return false;
            }

            if (!password.Equals(retypePassword))
            {
                errorMessage = PlasticLocalization.Name.PasswordDoesntMatch.GetString();
                return false;
            }

            return true;
        }

        static EncryptionConfigurationDialog Create(string server)
        {
            var instance = CreateInstance<EncryptionConfigurationDialog>();
            instance.mOrganizationInfo = OrganizationsInformation.FromServer(server);
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        string mPassword = string.Empty;
        string mRetypePassword = string.Empty;

        OrganizationInfo mOrganizationInfo;
    }
}

