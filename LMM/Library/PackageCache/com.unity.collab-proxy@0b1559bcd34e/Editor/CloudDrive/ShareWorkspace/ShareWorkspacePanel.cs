using System;
using System.Collections.Generic;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Common;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.CloudDrive.ShareWorkspace
{
    internal class ShareWorkspacePanel
    {
        internal ShareWorkspacePanel(
            WorkspaceInfo wkInfo,
            IProgressControls progressControls,
            Action repaintAction)
        {
            BuildComponents(wkInfo, progressControls, repaintAction);
        }

        internal List<SecurityMember> GetCollaboratorsToAdd()
        {
            return mCollaboratorsListView.GetCollaboratorsToAdd();
        }

        internal List<SecurityMember> GetCollaboratorsToRemove()
        {
            return mCollaboratorsListView.GetCollaboratorsToRemove();
        }

        internal void Refresh(string organization, string project)
        {
            mOrganization = organization;
            mProject = project;

            mCollaboratorsListView.Refresh(
                organization,
                GetProjectGuid.ForProject(project, organization));
        }

        internal void OnGUI(bool isOperationRunning)
        {
            GUILayout.Label(
                PlasticLocalization.Name.ShareDriveTitle.GetString(),
                UnityStyles.Dialog.Title);

            GUILayout.Space(5);

            GUILayout.Label(
                PlasticLocalization.Name.ShareDriveExplanation.GetString(),
                UnityStyles.Paragraph);

            GUILayout.Space(10);

            DrawFilterCollaboratorsArea();

            GUILayout.Space(10);

            Rect treeRect = GUILayoutUtility.GetRect(
                0,
                0,
                GUILayout.MaxWidth(LAYOUT_MAX_WIDTH),
                GUILayout.MaxHeight(350));
            mCollaboratorsListView.OnGUI(treeRect);

            GUILayout.Space(5);

            DrawManageUsersLink(isOperationRunning);

            GUILayout.Space(5);
        }

        void DrawFilterCollaboratorsArea()
        {
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(LAYOUT_MAX_WIDTH));
            GUILayout.FlexibleSpace();

            DrawSearchField.For(
                mSearchField,
                mCollaboratorsListView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            GUILayout.EndHorizontal();
        }

        void DrawManageUsersLink(bool isOperationRunning)
        {
            GUILayout.BeginHorizontal();

            using (new GuiEnabled(!isOperationRunning))
            {
                GUIContent buttonContent = new GUIContent(
                    PlasticLocalization.Name.ManageUsersDashboardLink.GetString());

                Rect rect = GUILayoutUtility.GetRect(
                    buttonContent, UnityStyles.LinkLabel,
                    GUILayout.MinWidth(ENTRY_WIDTH),
                    GUILayout.MaxWidth(ENTRY_WIDTH));

                if (GUI.Button(rect, buttonContent, UnityStyles.LinkLabel))
                {
                    OpenManageUsersUrl(mOrganization, mProject);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static void OpenManageUsersUrl(
            string selectedOrganization,
            string selectedProject)
        {
            string solvedServer = ResolveServer.FromUserInput(
                selectedOrganization, CmConnection.Get().UnityOrgResolver);

            if (!CloudServer.IsUnityOrganization(selectedOrganization))
            {
                Application.OpenURL(
                    UnityUrl.UnityDashboard.Plastic.GetForManageUsersLegacyOrg(
                        CloudServer.GetOrganizationName(selectedOrganization)));
                return;
            }

            string genesisOrgId;
            if (!CloudServer.TryGetGenesisOrgId(solvedServer, out genesisOrgId))
                return;

            Application.OpenURL(
                UnityUrl.UnityDashboard.UnityOrganizations.GetManageMembersUrl(
                    genesisOrgId,
                    GetProjectGuid.ForProject(selectedProject, selectedOrganization)));
        }

        void BuildComponents(
            WorkspaceInfo wkInfo,
            IProgressControls progressControls,
            Action repaintAction)
        {
            CollaboratorsListViewHeaderState collaboratorsListHeaderState =
                CollaboratorsListViewHeaderState.GetDefault();

            TreeHeaderSettings.Load(
                collaboratorsListHeaderState,
                UnityConstants.CloudDrive.COLLABORATORS_TABLE_SETTINGS_NAME,
                (int)CollaboratorsListColumn.User,
                true);

            mSearchField = new SearchField();
            mCollaboratorsListView = new CollaboratorsListView(
                wkInfo, progressControls, collaboratorsListHeaderState, repaintAction);
            mCollaboratorsListView.Reload();
        }

        string mOrganization;
        string mProject;

        SearchField mSearchField;
        CollaboratorsListView mCollaboratorsListView;

        const float ENTRY_WIDTH = 400;
        const float LAYOUT_MAX_WIDTH = 800;
    }
}
