using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticGui;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Settings;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.WebApi;

namespace Unity.PlasticSCM.Editor.Topbar
{
    internal static class TopbarButtons
    {
        internal static void DoTopbarButtons(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            bool isGluonMode,
            bool isCloudOrganization,
            bool isUnityOrganization,
            bool isUGOSubscription,
            string packageName,
            PackageInfo.VersionData versionData)
        {
            if (isGluonMode)
            {
                if (GUILayout.Button(
                        PlasticLocalization.Name.Configure.GetString(),
                        EditorStyles.toolbarButton))
                {
                    LaunchTool.OpenWorkspaceConfiguration(
                        showDownloadPlasticExeWindow, processExecutor, wkInfo, isGluonMode);
                }
            }
            else
            {
                DrawStaticElement.Empty();
            }

            if (!isGluonMode)
            {
                if (DrawToolbarButton(
                        Images.GetBranchExplorerIcon(),
                        PlasticLocalization.Name.BranchExplorerMenu.GetString()))
                {
#if UNITY_2022_1_OR_NEWER
                    ShowWindow.BranchExplorer();
#else
                    LaunchTool.OpenBranchExplorer(
                        showDownloadPlasticExeWindow, processExecutor, wkInfo, isGluonMode);
#endif
                }
            }
            else
            {
                DrawStaticElement.Empty();
            }

            if (isCloudOrganization)
            {
                if (DrawToolbarButton(
                    Images.GetInviteUsersIcon(),
                    isUnityOrganization
                        ? PlasticLocalization.Name.InviteMembersToProject.GetString()
                        : PlasticLocalization.Name.InviteMembersToOrganization.GetString()))
                {
                    InviteMembers(repSpec);
                }
            }
            else
            {
                DrawStaticElement.Empty();
            }

            if (isCloudOrganization && isUGOSubscription)
            {
                if (DrawToolbarTextButton(PlasticLocalization.Name.UpgradePlan.GetString()))
                {
                    OpenDevOpsUpgradePlanUrl();
                }
            }
            else
            {
                DrawStaticElement.Empty();
            }

            if (DrawToolbarButton(
                    GetSettingsMenuIcon(PackageInfo.Data),
                    GetSettingsMenuTooltip(PackageInfo.Data)))
            {
                ShowSettingsContextMenu(
                    showDownloadPlasticExeWindow,
                    processExecutor,
                    wkInfo,
                    repSpec,
                    isGluonMode,
                    isCloudOrganization,
                    packageName,
                    versionData);
            }
        }

        static bool DrawToolbarButton(Texture icon, string tooltip)
        {
            return GUILayout.Button(
                new GUIContent(icon, tooltip),
                EditorStyles.toolbarButton,
                GUILayout.Width(UnityConstants.TOOLBAR_ICON_BUTTON_WIDTH));
        }

        static bool DrawToolbarTextButton(string text)
        {
            return GUILayout.Button(
                new GUIContent(text, string.Empty),
                EditorStyles.toolbarButton);
        }

        static void InviteMembers(RepositorySpec repSpec)
        {
            string organizationName = ServerOrganizationParser.GetOrganizationFromServer(repSpec.Server);

            CurrentUserAdminCheckResponse response = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(50);
            waiter.Execute(
                /*threadOperationDelegate*/
                delegate
                {
                    string authToken = AuthToken.GetForServer(repSpec.Server);

                    if (string.IsNullOrEmpty(authToken))
                    {
                        return;
                    }

                    response = WebRestApiClient.PlasticScm.IsUserAdmin(organizationName, authToken);
                },
                /*afterOperationDelegate*/
                delegate
                {
                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.LogException("IsUserAdmin", waiter.Exception);

                        OpenUnityDashboardInviteUsersUrl(repSpec);
                        return;
                    }

                    if (response == null)
                    {
                        mLog.DebugFormat(
                            "Error checking if the user is the organization admin for {0}",
                            organizationName);

                        OpenUnityDashboardInviteUsersUrl(repSpec);
                        return;
                    }

                    if (response.Error != null)
                    {
                        mLog.DebugFormat(
                          "Error checking if the user is the organization admin: {0}",
                          string.Format("Unable to get IsUserAdminResponse: {0} [code {1}]",
                              response.Error.Message,
                              response.Error.ErrorCode));

                        OpenUnityDashboardInviteUsersUrl(repSpec);
                        return;
                    }

                    if (!response.IsCurrentUserAdmin)
                    {
                        GuiMessage.ShowInformation(
                            PlasticLocalization.GetString(PlasticLocalization.Name.InviteMembersTitle),
                            PlasticLocalization.GetString(PlasticLocalization.Name.InviteMembersToOrganizationNotAdminError));

                        return;
                    }

                    OpenUnityDashboardInviteUsersUrl(repSpec);
                });
        }

        static void OpenDevOpsUpgradePlanUrl()
        {
            Application.OpenURL(UnityUrl.DevOps.GetSignUp());
        }

        static Texture GetSettingsMenuIcon(PackageInfo.VersionData versionData)
        {
            if (!versionData.IsLatestVersion())
            {
                return Images.GetPackageUpdateAvailableIcon();
            }

            return Images.GetSettingsIcon();
        }

        static string GetSettingsMenuTooltip(PackageInfo.VersionData versionData)
        {
            if (!versionData.IsLatestVersion())
            {
                return PlasticLocalization.Name.UnityUpdateVersionControlPackageTooltip
                    .GetString(versionData.Version, versionData.LatestVersion);
            }

            return PlasticLocalization.Name.UnityVersionControlPackageIsUpToDateTooltip
                .GetString(versionData.Version);
        }

        static void ShowSettingsContextMenu(
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            bool isGluonMode,
            bool isCloudOrganization,
            string packageName,
            PackageInfo.VersionData versionData)
        {
            GenericMenu menu = new GenericMenu();

            string openToolText = isGluonMode ?
                PlasticLocalization.Name.OpenInGluon.GetString() :
                PlasticLocalization.Name.OpenInDesktopApp.GetString();

            menu.AddItem(
                new GUIContent(openToolText),
                false,
                () => LaunchTool.OpenGUIForMode(
                    showDownloadPlasticExeWindow,
                    processExecutor,
                    wkInfo,
                    isGluonMode));

            if (isCloudOrganization)
            {
                menu.AddItem(
                    new GUIContent(PlasticLocalization.Name.OpenInUnityCloud.GetString()),
                    false,
                    () => OpenUnityCloudRepository.Run(wkInfo));
            }

            menu.AddSeparator(string.Empty);

            menu.AddItem(
                new GUIContent(PlasticLocalization.Name.Settings.GetString()),
                false,
                OpenUVCSProjectSettings.ByDefault);

            menu.AddItem(
                new GUIContent(UVCSAssetModificationProcessor.IsManualCheckoutEnabled ?
                    PlasticLocalization.Name.DisableManualCheckout.GetString() :
                    PlasticLocalization.Name.EnableManualCheckout.GetString()),
                false,
                () => UVCSAssetModificationProcessor.ToggleManualCheckoutPreference(repSpec));

            AddUnityVersionControlPackageMenuItems(packageName, versionData, menu);

            menu.ShowAsContext();
        }

        static void AddUnityVersionControlPackageMenuItems(
            string packageName,
            PackageInfo.VersionData versionData,
            GenericMenu menu)
        {
            menu.AddSeparator(string.Empty);

            if (!versionData.IsLatestVersion())
            {
                menu.AddItem(
                    new GUIContent(
                        PlasticLocalization.Name.UnityUpdateVersionControlPackage.GetString()),
                    false,
                    () => LaunchPackageManager.AddByName(packageName, versionData.LatestVersion));
            }
            else
            {
                menu.AddDisabledItem(
                    new GUIContent(
                        PlasticLocalization.Name.UnityVersionControlPackageIsUpToDate.GetString()));
            }

            menu.AddItem(
                new GUIContent(
                    PlasticLocalization.Name.MainSidebarAboutItem.GetString()),
                false,
                () => LaunchPackageManager.Open(packageName));
        }

        static void OpenUnityDashboardInviteUsersUrl(RepositorySpec repSpec)
        {
            OpenInviteUsersPage.Run(repSpec, UnityUrl.UnityDashboard.UnityCloudRequestSource.Editor);
        }

        static readonly ILog mLog = PlasticApp.GetLogger("Topbar");
    }
}
