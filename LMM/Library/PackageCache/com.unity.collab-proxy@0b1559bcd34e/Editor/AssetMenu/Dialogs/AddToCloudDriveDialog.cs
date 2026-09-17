using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.CloudDrive.Workspaces;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.CloudDrive;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.AssetMenu.Dialogs
{
    internal class AddToCloudDriveDialog :
        PlasticDialog,
        FillOrganizationsAndProjects.INotify,
        FillCloudWorkspaces.IAddToCloudDriveDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 600, 350);
            }
        }

        internal static void ShowDialog(
            string[] assetPaths,
            IPlasticWebRestApi restApi,
            IPlasticAPI plasticApi,
            EditorWindow parentWindow)
        {
            AddToCloudDriveDialog dialog = Create(assetPaths, plasticApi);

            dialog.InitializeProposedOrganizationProject(restApi);

            FillOrganizationsAndProjects.LoadOrganizations(
                restApi,
                plasticApi,
                dialog.mProgressControls,
                dialog);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            if (dialogResult != ResponseType.Ok)
                return;

            CloudDriveWindow cloudDriveWindow = ShowWindow.CloudDrive();

            cloudDriveWindow.CopyPaths(
                dialog.mSelectedOrganization,
                dialog.mSelectedProject,
                dialog.mSelectedCloudDrive.WorkspaceInfo,
                assetPaths,
                dialog.mCloudDriveRelativePath);
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.AddToUnityCloudDriveTitle.GetString();
        }

        protected override string GetExplanation()
        {
            return PlasticLocalization.Name.AddToCloudDriveDescription.GetString();
        }

        protected override void DoComponentsArea()
        {
            bool isOperationRunning = mProgressControls.ProgressData.IsWaitingAsyncResult;

            GUI.enabled = !isOperationRunning;

            EntryBuilder.CreateComboBoxEntry(
                PlasticLocalization.Name.OrganizationLabel.GetString(),
                mSelectedOrganization,
                mOrganizations,
                OnOrganizationSelected,
                ENTRY_WIDTH,
                ENTRY_X);

            GUILayout.Space(5);

            if (CloudServer.IsUnityOrganization(mSelectedOrganization))
            {
                EntryBuilder.CreateComboBoxEntry(
                    PlasticLocalization.Name.ProjectLabel.GetString(),
                    mSelectedProject,
                    mProjects,
                    OnProjectSelected,
                    ENTRY_WIDTH,
                    ENTRY_X);

                GUILayout.Space(5);
            }

            string selectedDriveName = mSelectedCloudDrive != null ?
                mSelectedCloudDrive.RepositoryInfo.Name.GetLastPartFromSeparator('/') : string.Empty;
            List<string> driveNames = mCloudDrives.Select(
                workspace => workspace.RepositoryInfo.Name.GetLastPartFromSeparator('/')).ToList();

            EntryBuilder.CreateComboBoxEntry(
                PlasticLocalization.Name.CloudDriveLabel.GetString(),
                selectedDriveName,
                driveNames,
                OnDriveSelected,
                ENTRY_WIDTH,
                ENTRY_X);

            GUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                mCloudDriveRelativePath = EntryBuilder.CreateTextEntry(
                    PlasticLocalization.Name.RelativePathLabel.GetString(),
                    mCloudDriveRelativePath,
                    ENTRY_WIDTH - BROWSE_BUTTON_WIDTH,
                    ENTRY_X);

                Rect browseButtonRect = new Rect(
                    ENTRY_X + ENTRY_WIDTH - BROWSE_BUTTON_WIDTH + BUTTON_MARGIN,
                    GUILayoutUtility.GetLastRect().y,
                    BROWSE_BUTTON_WIDTH - BUTTON_MARGIN,
                    20);

                if (GUI.Button(browseButtonRect, "..."))
                    DoBrowseForPath();
            }

            GUI.enabled = true;
        }

        void FillOrganizationsAndProjects.INotify.OrganizationsRetrieved(List<string> organizations)
        {
            mOrganizations = organizations;

            mSelectedOrganization = GetDefaultValue(mProposedOrganization, mOrganizations);

            if (mSelectedOrganization == null)
                return;

            OnOrganizationSelected(mSelectedOrganization);

            Repaint();
        }

        void FillOrganizationsAndProjects.INotify.ProjectsRetrieved(List<string> projects)
        {
            mProjects = projects;

            mSelectedProject = GetDefaultValue(mProposedProject, mProjects);

            if (mSelectedProject == null)
                return;

            OnProjectSelected(mSelectedProject);

            Repaint();
        }

        void FillCloudWorkspaces.IAddToCloudDriveDialog.CloudDrivesRetrieved(
            List<CloudDriveWorkspace> workspaces)
        {
            mCloudDrives = workspaces;

            if (mCloudDrives.Count > 0)
                mSelectedCloudDrive = mCloudDrives[0];

            Repaint();
        }

        static AddToCloudDriveDialog Create(string[] assetPaths, IPlasticAPI plasticApi)
        {
            var instance = CreateInstance<AddToCloudDriveDialog>();
            instance.IsResizable = false;
            instance.mPlasticApi = plasticApi;

            instance.mCloudDriveRelativePath = GetProposedCloudDriveRelativePath(assetPaths);

            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;

            return instance;
        }

        void OnOrganizationSelected(object organization)
        {
            mSelectedOrganization = organization != null ? organization.ToString() : null;
            mProposedOrganization = mSelectedOrganization;
            mSelectedProject = null;
            mSelectedCloudDrive = null;

            mProjects.Clear();
            mCloudDrives.Clear();

            if (!CloudServer.IsUnityOrganization(mSelectedOrganization))
            {
                OnProjectSelected(string.Empty);
                return;
            }

            FillOrganizationsAndProjects.LoadProjects(
                mSelectedOrganization, mPlasticApi, mProgressControls, this);
        }

        void OnProjectSelected(object project)
        {
            mSelectedProject = project != null ? project.ToString() : null;
            mProposedProject = mSelectedProject;
            mSelectedCloudDrive = null;

            mCloudDrives.Clear();

            if (string.IsNullOrEmpty(mSelectedOrganization))
                return;

            FillCloudWorkspaces.LoadWorkspaces(
                mSelectedOrganization, mSelectedProject, this, mProgressControls);
        }

        void OnDriveSelected(object selectedDrive)
        {
            string driveName = selectedDrive != null ? selectedDrive.ToString() : null;
            mSelectedCloudDrive = mCloudDrives.FirstOrDefault(
                wkInfo =>
                    wkInfo.RepositoryInfo.Name.GetLastPartFromSeparator('/') == driveName);

            Repaint();
        }

        void DoBrowseForPath()
        {
            if (mSelectedCloudDrive == null)
            {
                ((IProgressControls)mProgressControls).ShowError(
                    PlasticLocalization.Name.SelectCloudDriveFirst.GetString());
                return;
            }

            string workspacePath = AssetsPath.GetFullPath.ForPath(mSelectedCloudDrive.WorkspaceInfo.ClientPath);

            string selectedPath = EditorUtility.SaveFolderPanel(
                PlasticLocalization.Name.SelectDestinationPath.GetString(),
                workspacePath,
                "");

            if (string.IsNullOrEmpty(selectedPath))
                return;

            string selectedFullPath = AssetsPath.GetFullPath.ForPath(selectedPath);

            if (string.IsNullOrEmpty(selectedFullPath))
                return;

            if (!selectedFullPath.StartsWith(workspacePath))
            {
                ((IProgressControls)mProgressControls).ShowError(
                    PlasticLocalization.Name.PathMustBeWithinWorkspace.GetString());
                return;
            }

            ((IProgressControls)mProgressControls).HideProgress();

            if (selectedFullPath == workspacePath)
            {
                mCloudDriveRelativePath = "/";
                return;
            }

            mCloudDriveRelativePath = selectedFullPath.Substring(workspacePath.Length).Replace('\\', '/');
        }

        protected override void DoOkButton()
        {
            bool isValid = mSelectedCloudDrive != null && mCloudDriveRelativePath.StartsWith("/");

            if (!isValid || mProgressControls.ProgressData.IsWaitingAsyncResult)
                GUI.enabled = false;

            if (NormalButton(PlasticLocalization.Name.AddToCloudDriveButton.GetString()))
            {
                OkButtonAction();
            }

            GUI.enabled = true;
        }

        static string GetProposedCloudDriveRelativePath(string[] assetPaths)
        {
            if (assetPaths.Length == 0)
                return string.Empty;

            string commonRoot = Path.GetDirectoryName(
                assetPaths[0].TrimEnd(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            for (int i = 1; i < assetPaths.Length; i++)
            {
                commonRoot = GetCommonRoot(
                    commonRoot,
                    Path.GetDirectoryName(
                        assetPaths[i].TrimEnd(
                            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            }

            return '/' + commonRoot.Replace('\\', '/');
        }

        static string GetCommonRoot(string path1, string path2)
        {
            string[] path1Parts = path1.Split(Path.DirectorySeparatorChar);
            string[] path2Parts = path2.Split(Path.DirectorySeparatorChar);

            int commonPartsCount = Mathf.Min(path1Parts.Length, path2Parts.Length);

            for (int i = 0; i < commonPartsCount; i++)
            {
                if (path1Parts[i] != path2Parts[i])
                {
                    commonPartsCount = i;
                    break;
                }
            }

            return string.Join(
                Path.DirectorySeparatorChar.ToString(),
                path1Parts,
                0,
                commonPartsCount);
        }

        static string GetDefaultValue(string proposedValue, List<string> values)
        {
            if (values.Count == 0)
                return null;

            if (!string.IsNullOrEmpty(proposedValue) && values.Contains(proposedValue))
                return proposedValue;

            return values[0];
        }

        void InitializeProposedOrganizationProject(IPlasticWebRestApi restApi)
        {
            GetProposedOrganizationProject.Values proposedOrganizationProject =
                GetProposedOrganizationProject.FromCloudProjectSettings();

            mProposedOrganization = proposedOrganizationProject != null ?
                proposedOrganizationProject.Organization :
                GetDefaultServer.FromConfig(restApi);

            mProposedProject = proposedOrganizationProject != null ?
                proposedOrganizationProject.Project :
                Application.productName;
        }

        List<string> mOrganizations = new List<string>();
        List<string> mProjects = new List<string>();
        List<CloudDriveWorkspace> mCloudDrives = new List<CloudDriveWorkspace>();

        string mProposedOrganization;
        string mProposedProject;
        string mSelectedOrganization;
        string mSelectedProject;
        CloudDriveWorkspace mSelectedCloudDrive;
        string mCloudDriveRelativePath;

        IPlasticAPI mPlasticApi;

        const float ENTRY_WIDTH = 400;
        const float ENTRY_X = 120f;
        const float BROWSE_BUTTON_WIDTH = 30;
        const float BUTTON_MARGIN = 5;
    }
}
