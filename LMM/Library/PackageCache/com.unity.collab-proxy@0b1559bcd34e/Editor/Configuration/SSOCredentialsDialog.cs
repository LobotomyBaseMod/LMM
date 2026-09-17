using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Codice.CM.Common;
using Codice.Client.Common;
using Codice.Client.Common.Authentication;
using Codice.Client.Common.Connection;
using Codice.Client.Common.WebApi.Responses;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Home;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Configuration
{
    internal class SSOCredentialsDialog :
        PlasticDialog,
        OAuthSignIn.INotify,
        GetCloudOrganizations.INotify
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 525, 450);
            }
        }

        internal static AskCredentialsToUser.DialogData RequestCredentials(
            string cloudServer,
            EditorWindow parentWindow)
        {
            SSOCredentialsDialog dialog = Create(cloudServer);
            ResponseType dialogResult = dialog.RunModal(parentWindow);

            return dialog.BuildCredentialsDialogData(dialogResult);
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.CredentialsDialogTitle.GetString();
        }

        protected override string GetExplanation()
        {
            return PlasticLocalization.Name.CredentialsDialogExplanation.GetString(mOrganizationInfo.DisplayName);
        }

        protected override void DoComponentsArea()
        {
            Paragraph("Sign in with Unity ID");
            GUILayout.Space(5);

            DoUnityIDButton();

            GUILayout.Space(25);
            Paragraph("    --or--    ");

            Paragraph("Sign in with email");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.Email.GetString(),
                    GUILayout.Width(80));

                Rect emailRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mEmail = EditorGUI.TextField(emailRect, mEmail);
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
        }

        void DoUnityIDButton()
        {
            if (NormalButton("Sign in with Unity ID"))
            {
                Guid state = Guid.NewGuid();

                OAuthSignInForUnityPackage(
                    GetAuthProviders.GetUnityIdAuthProvider(string.Empty, state),
                    GetCredentialsFromState.Build(
                        string.Empty,
                        state,
                        SEIDWorkingMode.SSOWorkingMode,
                        PlasticGui.Plastic.WebRestAPI));
            }
        }

        internal override void OkButtonAction()
        {
            mCredentials = new Credentials(
                new SEID(mEmail, false, mPassword),
                SEIDWorkingMode.LDAPWorkingMode);

            GetCloudOrganizations.GetOrganizationsInThreadWaiter(
                mCredentials.User.Data,
                mCredentials.User.Password,
                mProgressControls,
                this,
                PlasticGui.Plastic.WebRestAPI,
                CmConnection.Get());
        }

        void OAuthSignInForUnityPackage(
            AuthProvider authProvider, IGetCredentialsFromState getCredentialsFromState)
        {
            OAuthSignIn oAuthSignIn = new OAuthSignIn();

            oAuthSignIn.SignInForProviderInThreadWaiter(
                authProvider,
                string.Empty,
                mProgressControls,
                this,
                new OAuthSignIn.Browser(),
                getCredentialsFromState);
        }

        void OAuthSignIn.INotify.SignedInForCloud(
            string chosenProviderName, Credentials credentials)
        {
            mCredentials = credentials;

            GetCloudOrganizations.GetOrganizationsInThreadWaiter(
                mCredentials.User.Data,
                mCredentials.User.Password,
                mProgressControls,
                this,
                PlasticGui.Plastic.WebRestAPI,
                CmConnection.Get());
        }

        void OAuthSignIn.INotify.SignedInForOnPremise(
            string server, string proxy, Credentials credentials)
        {
            // The Plugin does not support SSO for on-premise (OIDCWorkingMode / SAMLWorkingMode)
            // as it is not prepared to show the necessary UI
        }

        void OAuthSignIn.INotify.Cancel(string errorMessage)
        {
            CancelButtonAction();
        }

        void GetCloudOrganizations.INotify.CloudOrganizationsRetrieved(
            List<string> cloudOrganizations)
        {
            if (!cloudOrganizations.Contains(mOrganizationInfo.Server))
            {
                CancelButtonAction();
                return;
            }

            ClientConfiguration.Save(
                mOrganizationInfo.Server,
                mCredentials.Mode,
                mCredentials.User.Data,
                mCredentials.User.Password);

            GetWindow<UVCSWindow>().InitializePlastic();
            OkButtonAction();
        }

        void GetCloudOrganizations.INotify.Error(ErrorResponse.ErrorFields error)
        {
            CancelButtonAction();
        }

        AskCredentialsToUser.DialogData BuildCredentialsDialogData(ResponseType dialogResult)
        {
            return dialogResult == ResponseType.Ok
                ? AskCredentialsToUser.DialogData.Success(mCredentials)
                : AskCredentialsToUser.DialogData.Failure(SEIDWorkingMode.SSOWorkingMode);
        }

        static SSOCredentialsDialog Create(string server)
        {
            var instance = CreateInstance<SSOCredentialsDialog>();
            instance.mOrganizationInfo = OrganizationsInformation.FromServer(server);
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        string mEmail;
        string mPassword = string.Empty;

        Credentials mCredentials;

        OrganizationInfo mOrganizationInfo;
    }
}
