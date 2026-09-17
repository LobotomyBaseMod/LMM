using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Topbar
{
    internal class Topbar
    {
        internal void Initialize(
            WorkspaceWindow workspaceWindow,
            NotificationsArea.IIncomingChangesNotification incomingChangesNotification,
            NotificationsArea.IShelvedChangesNotification shelvedChangesNotification)
        {
            mNotificationsArea = new NotificationsArea(
                workspaceWindow, incomingChangesNotification, shelvedChangesNotification);
        }

        internal void OnGUI(
            WorkspaceInfo workspaceInfo,
            RepositorySpec repSpec,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            string workingObjectName,
            string workingObjectFullSpec,
            string workingObjectComment,
            string workingObjectType,
            bool isGluonMode,
            bool isCloudOrganization,
            bool isUnityOrganization,
            bool isUGOSubscription,
            string packageName,
            PackageInfo.VersionData versionData)
        {
            // top separator
            Rect result = GUILayoutUtility.GetRect(
                1,
                1,
                GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(result, UnityStyles.Colors.BarBorder);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Space(5);

            Breadcrumb.DoBreadcrumb(
                workingObjectName, workingObjectFullSpec, workingObjectComment, workingObjectType);

            if (mNotificationsArea != null)
            {
                GUILayout.Space(10);
                mNotificationsArea.OnGUI();
            }

            GUILayout.FlexibleSpace();

            TopbarButtons.DoTopbarButtons(
                workspaceInfo,
                repSpec,
                showDownloadPlasticExeWindow,
                processExecutor,
                isGluonMode,
                isCloudOrganization,
                isUnityOrganization,
                isUGOSubscription,
                packageName,
                versionData);

            EditorGUILayout.EndHorizontal();
        }

        NotificationsArea mNotificationsArea;
    }
}
