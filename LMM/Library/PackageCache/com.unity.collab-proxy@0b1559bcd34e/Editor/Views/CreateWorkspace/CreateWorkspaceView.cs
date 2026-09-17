using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Home.Repositories;
using PlasticGui.WorkspaceWindow.Home.Workspaces;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.Views.CreateWorkspace
{
    internal class CreateWorkspaceView :
        IPlasticDialogCloser,
        IWorkspacesRefreshableView
    {
        internal interface ICreateWorkspaceListener
        {
            void OnWorkspaceCreated(WorkspaceInfo wkInfo, bool isGluonMode);
        }

        internal CreateWorkspaceView(
            UVCSWindow parentWindow,
            ICreateWorkspaceListener listener,
            IPlasticWebRestApi plasticWebRestApi,
            IPlasticAPI plasticApi,
            string workspacePath)
        {
            mParentWindow = parentWindow;
            mCreateWorkspaceListener = listener;
            mWorkspacePath = workspacePath;
            mPlasticWebRestApi = plasticWebRestApi;
            mPlasticApi = plasticApi;

            mProgressControls = new ProgressControlsForViews();
            mWorkspaceOperations = new WorkspaceOperations(this, mProgressControls, null);
            mCreateWorkspaceState = CreateWorkspaceViewState.BuildForProjectDefaults();

            Initialize(plasticWebRestApi, plasticApi);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                mProgressControls.ProgressData.CopyInto(
                    mCreateWorkspaceState.ProgressData);
            }

            string repository = mCreateWorkspaceState.Repository;

            DrawCreateWorkspace.ForState(
                SelectRepository,
                CreateRepository,
                ValidateAndCreateWorkspace,
                mParentWindow,
                mPlasticWebRestApi,
                mPlasticApi,
                mDefaultServer,
                ref mCreateWorkspaceState);

            if (repository == mCreateWorkspaceState.Repository)
                return;

            SynchronizeRepositoryAndWorkspace();
        }

        void Initialize(
            IPlasticWebRestApi plasticWebRestApi,
            IPlasticAPI plasticApi)
        {
            ((IProgressControls) mProgressControls).ShowProgress(string.Empty);

            WorkspaceInfo[] allWorkspaces = null;
            IList allRepositories = null;
            string repositoryProject = null;

            string localProjectFolder = !string.IsNullOrEmpty(CloudProjectSettings.projectName)
                ? CloudProjectSettings.projectName
                : Application.productName;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    mDefaultServer = GetDefaultServer.FromConfig(plasticWebRestApi);

                    allWorkspaces = plasticApi.GetAllWorkspacesArray();
                    allRepositories = plasticApi.GetAllRepositories(mDefaultServer, true);

                    if (OrganizationsInformation.IsUnityOrganization(mDefaultServer))
                    {
                        List<string> serverProjects = GetProjectNames.
                            FromOrganization(plasticApi, mDefaultServer);

                        if (serverProjects.Count == 0)
                        {
                            return;
                        }

                        // If any of the organization projects match in name with the local project, we select it.
                        // Otherwise, we return the first project in alphabetic order
                        repositoryProject = serverProjects.Contains(localProjectFolder)
                            ? localProjectFolder
                            : serverProjects.First();
                    }
                },
                /*afterOperationDelegate*/ delegate
                {
                    ((IProgressControls) mProgressControls).HideProgress();

                    if (waiter.Exception != null)
                    {
                        DisplayException(mProgressControls, waiter.Exception);
                        return;
                    }

                    UpdateProposedRepositoryAndWorkspace(allRepositories, repositoryProject, allWorkspaces);

                    mParentWindow.Repaint();
                });
        }

        void UpdateProposedRepositoryAndWorkspace(
            IList repositories,
            string repositoryProject,
            WorkspaceInfo[] workspaces)
        {
            string serverSpecPart = string.Format("@{0}", ResolveServer.ToDisplayString(mDefaultServer));

            mCreateWorkspaceState.Repository = ValidRepositoryName.Get(
                string.Format("{0}{1}", mCreateWorkspaceState.Repository, serverSpecPart),
                repositories);

            if (repositoryProject != null)
            {
                mCreateWorkspaceState.Repository = CloudProjectRepository.BuildFullyQualifiedName(
                    repositoryProject, mCreateWorkspaceState.Repository);
            }

            string proposedWorkspaceName = mCreateWorkspaceState.Repository.Replace(serverSpecPart, string.Empty);

            mCreateWorkspaceState.WorkspaceName = CreateWorkspaceDialogUserAssistant.GetNonExistingWkNameForName(
                proposedWorkspaceName, workspaces);

            mDialogUserAssistant = CreateWorkspaceDialogUserAssistant.ForWkPathAndName(
                mWorkspacePath,
                workspaces);

            SynchronizeRepositoryAndWorkspace();
        }

        void SynchronizeRepositoryAndWorkspace()
        {
            if (mDialogUserAssistant == null)
            {
                return;
            }

            mDialogUserAssistant.RepositoryChanged(
                mCreateWorkspaceState.Repository,
                mCreateWorkspaceState.WorkspaceName,
                mWorkspacePath);

            mCreateWorkspaceState.WorkspaceName = mDialogUserAssistant.GetProposedWorkspaceName();
        }

        void SelectRepository(string repository)
        {
            mCreateWorkspaceState.Repository = repository;
            SynchronizeRepositoryAndWorkspace();
        }

        void CreateRepository(RepositoryCreationData data)
        {
            if (!data.Result)
                return;

            ((IProgressControls)mProgressControls).ShowProgress(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.CreatingRepository,
                    data.RepName));

            RepositoryInfo createdRepository = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    createdRepository = PlasticGui.Plastic.API.CreateRepository(
                        data.ServerName, data.RepName);
                },
                /*afterOperationDelegate*/ delegate
                {
                    ((IProgressControls)mProgressControls).HideProgress();

                    if (waiter.Exception != null)
                    {
                        DisplayException(mProgressControls, waiter.Exception);
                        return;
                    }

                    if (createdRepository == null)
                        return;

                    mCreateWorkspaceState.Repository = createdRepository.GetRepSpec().ToDisplayString();

                    SynchronizeRepositoryAndWorkspace();

                    mParentWindow.Repaint();
                });
        }

        void ValidateAndCreateWorkspace(CreateWorkspaceViewState state)
        {
            // We need a conversion here because the actual repSpec is used during the AsyncValidation
            // to call UpdateLastUsedRepositoryPreference. Also, it is the one to be used during wk creation
            RepositorySpec repSpec = OrganizationsInformation.TryResolveRepositorySpecFromInput(state.Repository);

            if (repSpec == null)
            {
                ((IProgressControls) mProgressControls).ShowError(
                    PlasticLocalization.Name.UnresolvedRepository.GetString(state.Repository));

                return;
            }

            mWkCreationData = BuildCreationDataFromState(state, mWorkspacePath, repSpec);

            // AsyncValidation calls IPlasticDialogCloser.CloseDialog() when the validation is ok
            WorkspaceCreationValidation.AsyncValidation(mWkCreationData, this, mProgressControls);
        }

        void IPlasticDialogCloser.CloseDialog()
        {
            ((IProgressControls) mProgressControls).ShowProgress(string.Empty);

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    RepositorySpec repSpec = new SpecGenerator().GenRepositorySpec(
                        false, mWkCreationData.Repository, CmConnection.Get().UnityOrgResolver);

                    bool repositoryExist = PlasticGui.Plastic.API.CheckRepositoryExists(
                        repSpec.Server, repSpec.Name);

                    if (!repositoryExist)
                        PlasticGui.Plastic.API.CreateRepository(repSpec.Server, repSpec.Name);
                },
                /*afterOperationDelegate*/ delegate
                {
                    ((IProgressControls)mProgressControls).HideProgress();

                    if (waiter.Exception != null)
                    {
                        DisplayException(mProgressControls, waiter.Exception);
                        return;
                    }

                    mWkCreationData.Result = true;
                    mWorkspaceOperations.CreateWorkspace(mWkCreationData);

                    // the operation calls IWorkspacesRefreshableView.RefreshAndSelect
                    // when the workspace is created
                });
        }

        void IWorkspacesRefreshableView.RefreshAndSelect(WorkspaceInfo wkInfo)
        {
            GetWindowIfOpened.BranchExplorer()?.OnWorkspaceCreated(wkInfo);

            PerformInitialCheckin.IfRepositoryIsEmpty(
                wkInfo,
                mWkCreationData.Repository,
                mWkCreationData.IsGluonWorkspace,
                PlasticGui.Plastic.API,
                mProgressControls,
                mCreateWorkspaceListener,
                mParentWindow);

            UnityCloudProjectLinkMonitor.CheckCloudProjectAlignmentAsync(wkInfo);
        }

        static WorkspaceCreationData BuildCreationDataFromState(
            CreateWorkspaceViewState state,
            string workspacePath,
            RepositorySpec repSpec)
        {
            return new WorkspaceCreationData(
                state.WorkspaceName,
                workspacePath,
                repSpec.ToString(),
                state.WorkspaceMode == CreateWorkspaceViewState.WorkspaceModes.Gluon,
                false);
        }

        static void DisplayException(
            IProgressControls progressControls,
            Exception ex)
        {
            ExceptionsHandler.LogException(
                "CreateWorkspaceView", ex);

            progressControls.ShowError(
                ExceptionsHandler.GetCorrectExceptionMessage(ex));
        }

        WorkspaceCreationData mWkCreationData;
        CreateWorkspaceViewState mCreateWorkspaceState;

        CreateWorkspaceDialogUserAssistant mDialogUserAssistant;

        string mDefaultServer;

        readonly WorkspaceOperations mWorkspaceOperations;
        readonly ProgressControlsForViews mProgressControls;
        readonly string mWorkspacePath;
        readonly ICreateWorkspaceListener mCreateWorkspaceListener;
        readonly UVCSWindow mParentWindow;
        readonly IPlasticAPI mPlasticApi;
        readonly IPlasticWebRestApi mPlasticWebRestApi;
    }
}
