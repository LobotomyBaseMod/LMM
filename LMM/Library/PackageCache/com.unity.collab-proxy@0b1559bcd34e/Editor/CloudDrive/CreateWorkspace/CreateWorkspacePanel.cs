using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.CloudDrive.Workspaces;
using Unity.PlasticSCM.Editor.CloudDrive.ShareWorkspace;
using PlasticGui.WorkspaceWindow.Home;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace
{
    internal class CreateWorkspacePanel :
        FillCreateWorkspaceView.ICreateWorkspaceView,
        FillOrganizationsAndProjects.INotify
    {
        internal CreateWorkspacePanel(
            string proposedOrganization,
            string proposedProject,
            IPlasticWebRestApi restApi,
            IPlasticAPI plasticApi,
            EditorWindow parentWindow,
            IProgressControls progressControls)
        {
            mProposedOrganization = proposedOrganization;
            mProposedProject = proposedProject;
            mPlasticApi = plasticApi;
            mParentWindow = parentWindow;
            mProgressControls = progressControls;

            BuildComponents(mProgressControls, parentWindow.Repaint);

            FillOrganizationsAndProjects.LoadOrganizations(
                restApi, plasticApi, mProgressControls, this);
        }

        internal WorkspaceCreationData BuildCreationData()
        {
            string name = CloudServer.IsUnityOrganization(mSelectedOrganization) ?
                CloudProjectRepository.BuildFullyQualifiedName(
                    mSelectedProject, mWorkspaceName) :
                mWorkspaceName;

            return new WorkspaceCreationData(
                mSelectedOrganization,
                name,
                mShareWorkspacePanel.GetCollaboratorsToAdd());
        }

        internal bool IsInputValid()
        {
            return IsInputValid(mSelectedOrganization, mSelectedProject, mWorkspaceName);
        }

        internal void OnGUI(bool isOperationRunning = false)
        {
            DoTitleArea(mExplanation);

            DoEntriesArea(isOperationRunning);

            GUILayout.Space(5);

            mShareWorkspacePanel.OnGUI(isOperationRunning);
        }

        void FillOrganizationsAndProjects.INotify.OrganizationsRetrieved(
            List<string> organizations)
        {
            mOrganizations = organizations;

            string organizationToSelect = GetDefaultValue(mProposedOrganization, mOrganizations);

            if (organizationToSelect == null)
                return;

            OnOrganizationSelected(organizationToSelect);
        }

        void FillOrganizationsAndProjects.INotify.ProjectsRetrieved(
            List<string> projects)
        {
            mProjects = projects;

            mSelectedProject = GetDefaultValue(mProposedProject, mProjects);

            OnProjectSelected(mSelectedProject);
        }

        void FillCreateWorkspaceView.ICreateWorkspaceView.WorkspaceNameRetrieved(
            string workspaceName)
        {
            mWorkspaceName = workspaceName;

            mShareWorkspacePanel.Refresh(mSelectedOrganization, mSelectedProject);
        }

        void OnOrganizationSelected(object organization)
        {
            mSelectedOrganization = organization.ToString();
            mSelectedProject = null;

            mProjects.Clear();

            mExplanation = GetExplanationText(mSelectedOrganization);

            FillOrganizationsAndProjects.LoadProjects(
                mSelectedOrganization, mPlasticApi, mProgressControls, this);

            mParentWindow.Repaint();
        }

        void OnProjectSelected(object project)
        {
            mSelectedProject = project == null ? null : project.ToString();

            mShareWorkspacePanel.Refresh(mSelectedOrganization, mSelectedProject);

            FillCreateWorkspaceView.LoadWorkspaceName(
                mSelectedOrganization,
                mWorkspaceName ?? mProposedWorkspaceName,
                mPlasticApi,
                mProgressControls,
                this);

            mParentWindow.Repaint();
        }

        void DoEntriesArea(bool isOperationRunning)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinHeight(108));

            GUI.enabled = !isOperationRunning;

            EntryBuilder.CreateComboBoxEntry(
                PlasticLocalization.Name.OrganizationLabel.GetString(),
                mSelectedOrganization,
                mOrganizations,
                OnOrganizationSelected,
                ENTRY_WIDTH,
                ENTRY_X);

            GUILayout.Space(5);

            if (mSelectedOrganization == null ||
                CloudServer.IsUnityOrganization(mSelectedOrganization))
            {
                EntryBuilder.CreateComboBoxEntry(
                    PlasticLocalization.Name.OrganizationProjectLabel.GetString(),
                    mSelectedProject,
                    mProjects,
                    OnProjectSelected,
                    ENTRY_WIDTH,
                    ENTRY_X);

                DoCreateOrganizationProjectLink(
                    mSelectedOrganization, isOperationRunning);
            }

            GUILayout.Space(5);

            mWorkspaceName = EntryBuilder.CreateTextEntry(
                PlasticLocalization.Name.CloudWorkspaceNameEntry.GetString(),
                mWorkspaceName,
                WKNAME_CONTROL_NAME,
                ENTRY_WIDTH,
                ENTRY_X);

            InputValidationResult inputValidationResult =
                ValidateWorkspaceNameInput(mWorkspaceName);

            if (!inputValidationResult.IsValid)
                DoWarningLabel(inputValidationResult.ErrorMessage, ENTRY_X);

            FocusWorkspaceNameEntryIfNeeded();

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }


        void FocusWorkspaceNameEntryIfNeeded()
        {
            if (mWasWorkspaceNameFocused)
                return;

            EditorGUI.FocusTextInControl(WKNAME_CONTROL_NAME);

            mWasWorkspaceNameFocused = true;
        }

        static void OpenOrganizationProjectUrl(string selectedOrganization)
        {
            string solvedServer = ResolveServer.FromUserInput(
                selectedOrganization, CmConnection.Get().UnityOrgResolver);

            string genesisOrgId;
            if (!CloudServer.TryGetGenesisOrgId(solvedServer, out genesisOrgId))
                return;

            Application.OpenURL(UnityUrl.UnityDashboard.
                UnityOrganizations.GetProjectsUrl(genesisOrgId));
        }

        static void DoTitleArea(string explanation)
        {
            GUILayout.Label(PlasticLocalization.Name.CreateCloudWorkspaceTitle.GetString(),
                UnityStyles.Dialog.Title);

            GUILayout.Space(5);

            GUILayout.Label(explanation, UnityStyles.Paragraph);

            GUILayout.Space(10);
        }

        static void DoCreateOrganizationProjectLink(
            string selectedOrganization,
            bool isOperationRunning)
        {
            string buttonText = PlasticLocalization.Name.
                CreateOrganizationProjectLabel.GetString();

            if (DoButton(
                    buttonText,
                    UnityStyles.LinkLabel,
                    !isOperationRunning,
                    ENTRY_WIDTH,
                    ENTRY_X + ENTRY_WIDTH - PROJECT_LINK_WIDTH))
            {
                OpenOrganizationProjectUrl(selectedOrganization);
            }
        }

        static bool DoButton(
            string text,
            GUIStyle style,
            bool isEnabled,
            float width,
            float x = -1,
            float y = -1)
        {
            using (new GuiEnabled(isEnabled))
            {
                GUIContent buttonContent = new GUIContent(text);

                Rect rect = GUILayoutUtility.GetRect(
                    buttonContent, style,
                    GUILayout.MinWidth(width),
                    GUILayout.MaxWidth(width));

                if (x != -1)
                    rect.x = x;

                if (y != -1)
                    rect.y = y;

                bool result = GUI.Button(rect, buttonContent, style);

                return result;
            }
        }

        static void DoWarningLabel(
            string text,
            float x)
        {
            Rect rect = GUILayoutUtility.GetRect(
                new GUIContent(text),
                EditorStyles.label);
            rect.x = x;

            GUI.Label(rect,
                new GUIContent(text, Images.GetWarnIcon()),
                UnityStyles.HeaderWarningLabel);
        }

        static InputValidationResult ValidateWorkspaceNameInput(string workspaceName)
        {
            InputValidationResult result = new InputValidationResult();

            result.IsValid = InputValidator.IsValidRepositoryName(
                workspaceName,
                PlasticLocalization.Name.CloudWorkspaceNameEmpty.GetString(),
                out result.ErrorMessage);

            return result;
        }

        static string GetDefaultValue(string proposedValue, List<string> values)
        {
            if (values.Count == 0)
                return null;

            if (proposedValue != null && values.Contains(proposedValue))
                return proposedValue;

            return values[0];
        }

        static string GetExplanationText(string selectedOrganization)
        {
            return PlasticLocalization.Name.CreateCloudWorkspaceExplanation.GetString(
                CloudServer.IsUnityOrganization(selectedOrganization) ?
                    PlasticLocalization.Name.CreateCloudWorkspaceExplanationSelectForUnityOrg.GetString() :
                    PlasticLocalization.Name.CreateCloudWorkspaceExplanationSelectForCloudOrg.GetString());
        }

        static bool IsInputValid(
            string selectedOrganization,
            string selectedProject,
            string workspaceName)
        {
            if (string.IsNullOrEmpty(selectedOrganization))
                return false;

            if (CloudServer.IsUnityOrganization(selectedOrganization) &&
                string.IsNullOrEmpty(selectedProject))
                return false;

            return ValidateWorkspaceNameInput(workspaceName).IsValid;
        }

        void BuildComponents(IProgressControls progressControls, Action repaintAction)
        {
            mExplanation = PlasticLocalization.Name.CreateCloudWorkspaceExplanation.GetString(
                PlasticLocalization.Name.CreateCloudWorkspaceExplanationSelectForUnityOrg.GetString());

            CalculateProposedOrganizationProjectIfNeeded();

            mProposedWorkspaceName = Application.productName;

            mShareWorkspacePanel = new ShareWorkspacePanel(null, progressControls, repaintAction);
        }

        void CalculateProposedOrganizationProjectIfNeeded()
        {
            if (!string.IsNullOrEmpty(mProposedOrganization) ||
                !string.IsNullOrEmpty(mProposedProject))
                return;

            GetProposedOrganizationProject.Values proposedOrganizationProject =
                    GetProposedOrganizationProject.FromCloudProjectSettings();

            if (proposedOrganizationProject == null)
                return;

            mProposedOrganization = proposedOrganizationProject.Organization;
            mProposedProject = proposedOrganizationProject.Project;
        }

        class InputValidationResult
        {
            internal string ErrorMessage;
            internal bool IsValid;
        }

        bool mWasWorkspaceNameFocused;
        string mExplanation;
        string mProposedOrganization;
        string mProposedProject;
        string mProposedWorkspaceName;
        string mSelectedOrganization;
        string mSelectedProject;
        string mWorkspaceName;

        List<string> mOrganizations = new List<string>();
        List<string> mProjects = new List<string>();

        ShareWorkspacePanel mShareWorkspacePanel;

        readonly IProgressControls mProgressControls;
        readonly EditorWindow mParentWindow;
        readonly IPlasticAPI mPlasticApi;

        const float PROJECT_LINK_WIDTH = 192;
        const float ENTRY_WIDTH = 400;
        const float ENTRY_X = 175f;
        const float LAYOUT_MAX_WIDTH = 800;

        const string WKNAME_CONTROL_NAME = "CreateWorkspaceView.WorkspaceNameTextField";
    }
}
