using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticGui;
using Unity.PlasticSCM.Editor.CloudDrive;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.WebApi;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class AutoLogin
    {
        internal interface IWelcomeView
        {
            State AutoLoginState { get; set; }
            void OnUserClosedConfigurationWindow();
        }

        internal enum State : byte
        {
            Off = 0,
            Started = 1,
            Running = 2,
            ResponseInit = 3,
            ResponseEnd = 4,
            ResponseSuccess = 5,
            ErrorNoToken = 20,
            ErrorTokenException = 21,
            ErrorResponseNull = 22,
            ErrorResponseError = 23,
            ErrorTokenEmpty = 24,
        }

        internal bool Run(IWelcomeView welcomeView)
        {
            mLog.Debug("Run");

            if (welcomeView == null)
                welcomeView = GetWelcomeView();

            mWelcomeView = welcomeView;

            if (string.IsNullOrEmpty(CloudProjectSettings.accessToken))
            {
                mWelcomeView.AutoLoginState = AutoLogin.State.ErrorNoToken;
                return false;
            }

            mWelcomeView.AutoLoginState = AutoLogin.State.Running;

            ExchangeTokensAndJoinOrganizationInThreadWaiter(CloudProjectSettings.accessToken);

            return true;
        }

        void ExchangeTokensAndJoinOrganizationInThreadWaiter(string unityAccessToken)
        {
            int ini = Environment.TickCount;

            TokenExchangeResponse tokenExchangeResponse = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
            /*threadOperationDelegate*/ delegate
            {
                mWelcomeView.AutoLoginState = AutoLogin.State.ResponseInit;
                tokenExchangeResponse = WebRestApiClient.PlasticScm.TokenExchange(unityAccessToken);
            },
            /*afterOperationDelegate*/ delegate
            {
                mLog.DebugFormat(
                    "TokenExchange time {0} ms",
                    Environment.TickCount - ini);

                if (waiter.Exception != null)
                {
                    mWelcomeView.AutoLoginState = AutoLogin.State.ErrorTokenException;
                    ExceptionsHandler.LogException(
                        "TokenExchangeSetting",
                        waiter.Exception);
                    Debug.LogWarning(waiter.Exception.Message);
                    return;
                }

                if (tokenExchangeResponse == null)
                {
                    mWelcomeView.AutoLoginState = AutoLogin.State.ErrorResponseNull;
                    var warning = PlasticLocalization.GetString(PlasticLocalization.Name.TokenExchangeResponseNull);
                    mLog.Warn(warning);
                    Debug.LogWarning(warning);
                    return;
                }

                if (tokenExchangeResponse.Error != null)
                {
                    mWelcomeView.AutoLoginState = AutoLogin.State.ErrorResponseError;
                    var warning = string.Format(
                        PlasticLocalization.GetString(PlasticLocalization.Name.TokenExchangeResponseError),
                        tokenExchangeResponse.Error.Message, tokenExchangeResponse.Error.ErrorCode);
                    mLog.ErrorFormat(warning);
                    Debug.LogWarning(warning);
                    return;
                }

                if (string.IsNullOrEmpty(tokenExchangeResponse.AccessToken))
                {
                    mWelcomeView.AutoLoginState = AutoLogin.State.ErrorTokenEmpty;
                    var warning = string.Format(
                        PlasticLocalization.GetString(PlasticLocalization.Name.TokenExchangeAccessEmpty),
                        tokenExchangeResponse.User);
                    mLog.InfoFormat(warning);
                    Debug.LogWarning(warning);
                    return;
                }

                mWelcomeView.AutoLoginState = AutoLogin.State.ResponseEnd;

                Credentials credentials = new Credentials(
                    new SEID(tokenExchangeResponse.User, false, tokenExchangeResponse.AccessToken),
                    SEIDWorkingMode.SSOWorkingMode);

                if (TryAutoJoinCloudProjectOrganization(credentials))
                    return;

                ShowOrganizationsPanel(credentials);
            });
        }

        bool TryAutoJoinCloudProjectOrganization(Credentials credentials)
        {
            // CloudProjectSettings.organizationKey is not available in 2021.3
#if UNITY_2022_1_OR_NEWER
            if (string.IsNullOrEmpty(CloudProjectSettings.organizationKey) ||
                string.IsNullOrEmpty(CloudProjectSettings.organizationName))
                return false;

            // Update the cache with the organization info from the Cloud project settings,
            // so if not it won't be available when creating the workspace.
            CmConnection.Get().UnityOrgCache.AddEntry(
                CloudProjectSettings.organizationKey,
                CloudProjectSettings.organizationName);

            string serverFromHubOrganization = CloudServer.BuildFullyQualifiedName(
                CloudProjectSettings.organizationKey, CloudServer.DefaultAlias);

            mLog.DebugFormat(
                "Auto-joining organization '{0}' from Unity Cloud project settings",
                serverFromHubOrganization);

            ClientConfiguration.Save(
                serverFromHubOrganization,
                credentials.Mode,
                credentials.User.Data,
                credentials.User.Password);

            if (mWelcomeView == null)
                mWelcomeView = GetWelcomeView();

            mWelcomeView.AutoLoginState = AutoLogin.State.ResponseSuccess;
            mWelcomeView.OnUserClosedConfigurationWindow();
            return true;
#else
            return false;
#endif
        }

        void ShowOrganizationsPanel(Credentials credentials)
        {
            if (mWelcomeView == null)
                mWelcomeView = GetWelcomeView();

            mWelcomeView.AutoLoginState = AutoLogin.State.ResponseSuccess;

            CloudEditionWelcomeWindow.ShowWindow(
                PlasticGui.Plastic.WebRestAPI, mWelcomeView, true);

            mCloudEditionWelcomeWindow = CloudEditionWelcomeWindow.GetWelcomeWindow();

            mCloudEditionWelcomeWindow.GetOrganizations(credentials);

            mCloudEditionWelcomeWindow.Focus();
        }

        static IWelcomeView GetWelcomeView()
        {
            var uvcsWindows = Resources.FindObjectsOfTypeAll<UVCSWindow>();
            UVCSWindow uvcsWindow = uvcsWindows.Length > 0 ? uvcsWindows[0] : null;

            if (uvcsWindow != null)
                return uvcsWindow.GetWelcomeView();

            var cloudDriveWindows = Resources.FindObjectsOfTypeAll<CloudDriveWindow>();
            CloudDriveWindow cloudDriveWindow = cloudDriveWindows.Length > 0 ? cloudDriveWindows[0] : null;

            if (cloudDriveWindow != null)
                return cloudDriveWindow.GetWelcomeView();

            return ShowWindow.UVCS().GetWelcomeView();
        }

        IWelcomeView mWelcomeView;
        CloudEditionWelcomeWindow mCloudEditionWelcomeWindow;

        static readonly ILog mLog = PlasticApp.GetLogger("AutoLogin");
    }
}
