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
using PlasticGui.WorkspaceWindow.Servers;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.Views.CreateWorkspace.Dialogs
{
    internal class CreateRepositoryDialog :
        PlasticDialog,
        KnownServersListOperations.IKnownServersList
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 600, 340);
            }
        }

        internal static RepositoryCreationData CreateRepository(
            EditorWindow parentWindow,
            IPlasticWebRestApi plasticWebRestApi,
            IPlasticAPI plasticApi,
            string proposedRepositoryName,
            string proposedServer,
            string lastUsedRepServer,
            string clientConfServer)
        {
            string server = CreateRepositoryDialogUserAssistant.GetProposedServer(
                proposedServer, lastUsedRepServer, clientConfServer);

            CreateRepositoryDialog dialog = Create(
                plasticWebRestApi,
                plasticApi,
                proposedRepositoryName,
                server);

            ResponseType dialogResult = dialog.RunModal(parentWindow);
            bool dialogResultOk = dialogResult == ResponseType.Ok;

            RepositoryCreationData result = dialogResultOk ? dialog.BuildCreationData() : new RepositoryCreationData();
            result.Result = dialogResultOk;

            return result;
        }

        protected override void DoComponentsArea()
        {
            if (Event.current.type == EventType.Layout)
                mProgressControls.ProgressData.CopyInto(mProgressData);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.RepositoryNameShortLabel.GetString(),
                    GUILayout.Width(140));

                Rect nameRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                GUI.SetNextControlName(REPONAME_CONTROL_NAME);
                mRepositoryName = EditorGUI.TextField(nameRect, mRepositoryName);

                if (!mFocusIsAreadySet)
                {
                    mFocusIsAreadySet = true;
                    GUI.FocusControl(REPONAME_CONTROL_NAME);
                }
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.RepositoryServerOrOrganizationLabel.GetString(),
                    GUILayout.Width(140));

                Rect serverRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mSelectedServer = DropDownTextField.DoDropDownTextField(
                    mSelectedServer,
                    PlasticLocalization.Name.RepositoryServerOrOrganizationLabel.GetString(),
                    mKnownServers,
                    OnServerSelected,
                    serverRect);
            }

            if (!OrganizationsInformation.IsUnityOrganization(mSelectedServer) ||
                !mKnownServers.Contains(mSelectedServer))
            {
                mSelectedProject = null;
                mCurrentServerProjects = null;
                return;
            }

            GUILayout.Space(5);

            DoOrganizationProjectsDropdown();
            DoCreateOrganizationProjectLink();
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.NewRepositoryTitle.GetString();
        }

        protected override string GetExplanation()
        {
            return PlasticLocalization.Name.NewRepositoryExplanation.GetString() + "\n\n" +
                   PlasticLocalization.Name.CreateRepositoryDialogDetailedExplanation.GetString();
        }

        void KnownServersListOperations.IKnownServersList.FillValues(List<string> knownServers)
        {
            // Filter out local server if there is no local installation
            // TODO Remove if a unified solution is applied to the unityplastic library
            if (knownServers.Contains(LocalOnlyServer.Alias) && !IsExeAvailable.ForLocalServer())
            {
                knownServers.Remove(LocalOnlyServer.Alias);
            }

            mKnownServers = knownServers.Select(ResolveServer.ToDisplayString).ToList();
            mKnownServers.Sort();

            OnServerSelected(mSelectedServer);
        }

        void DoOrganizationProjectsDropdown()
        {
            if (mIsLoadingProjects || mSelectedProject == null)
            {
                GUI.enabled = false;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.OrganizationProjectLabel.GetString(),
                    GUILayout.Width(140));

                Rect projectRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mSelectedProject = DropDownTextField.DoDropDownTextField(
                    mSelectedProject,
                    PlasticLocalization.Name.RepositoryServerOrOrganizationLabel.GetString(),
                mCurrentServerProjects,
                OnProjectSelected,
                    projectRect);
            }

            GUI.enabled = true;
        }

        void DoCreateOrganizationProjectLink()
        {
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    PlasticLocalization.Name.CreateOrganizationProjectLabel.GetString(),
                    UnityStyles.LinkLabel,
                    GUILayout.Height(20)))
            {
                string resolvedServer = OrganizationsInformation.TryResolveServerFromInput(mSelectedServer);

                if (!string.IsNullOrEmpty(resolvedServer))
                {
                    string organizationName = ServerOrganizationParser.GetOrganizationFromServer(resolvedServer);
                    Application.OpenURL(UnityUrl.UnityDashboard.UnityOrganizations.GetProjectsUrl(organizationName));
                }
            }

            GUILayout.EndHorizontal();
        }

        void OnServerSelected(object server)
        {
            ((IProgressControls) mProgressControls).HideProgress();
            mProgressControls.ProgressData.StatusMessage = string.Empty;
            mProgressControls.ProgressData.StatusType = MessageType.None;

            mIsLoadingProjects = false;

            if (server == null || string.IsNullOrEmpty(server.ToString()))
            {
                mSelectedServer = null;
                mSelectedProject = null;
                mCurrentServerProjects = null;

                return;
            }

            mSelectedServer = server.ToString();

            // We need to ensure it is a known server because the dropdown is editable
            if (OrganizationsInformation.IsUnityOrganization(mSelectedServer) && mKnownServers.Contains(mSelectedServer))
            {
                LoadServerProjects(mSelectedServer);
            }
            else
            {
                mSelectedProject = null;
            }

            Repaint();
        }

        void OnFocus()
        {
            OnServerSelected(mSelectedServer);
        }

        void OnProjectSelected(object project)
        {
            mSelectedProject = project.ToString();

            Repaint();
        }

        void LoadServerProjects(string server)
        {
            mIsLoadingProjects = true;
            mCurrentServerProjects = null;

            ((IProgressControls) mProgressControls).ShowProgress(
                PlasticLocalization.Name.RetrievingServerProjects.GetString());

            List<string> serverProjects = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    string serverName = ResolveServer.FromUserInput(server, CmConnection.Get().UnityOrgResolver);

                    serverProjects = GetProjectNames.FromOrganization(mPlasticApi, serverName);
                },
                /*afterOperationDelegate*/ delegate
                {
                    mIsLoadingProjects = false;

                    if (waiter.Exception != null)
                    {
                        ((IProgressControls) mProgressControls).ShowError(
                            PlasticLocalization.Name.ErrorRetrievingServerProjects.GetString());
                    }

                    mCurrentServerProjects = serverProjects;

                    if (mCurrentServerProjects == null || mCurrentServerProjects.Count == 0)
                    {
                        mSelectedProject = null;
                        mProposedProject = null;

                        ((IProgressControls) mProgressControls).ShowError(
                            PlasticLocalization.Name.NoServerProjectsFound.GetString());

                        return;
                    }

                    if (!string.IsNullOrEmpty(mProposedProject))
                    {
                        mSelectedProject = mProposedProject;
                        mProposedProject = null;
                    }

                    if (string.IsNullOrEmpty(mSelectedProject) || !mCurrentServerProjects.Contains(mSelectedProject))
                    {
                        mSelectedProject = mCurrentServerProjects.First();
                    }

                    ((IProgressControls) mProgressControls).HideProgress();
                });
        }

        protected override void DoOkButton()
        {
            if (mIsLoadingProjects ||
                (OrganizationsInformation.IsUnityOrganization(mSelectedServer) && string.IsNullOrEmpty(mSelectedProject)))
            {
                GUI.enabled = false;
            }

            if (NormalButton(PlasticLocalization.Name.OkButton.GetString()))
            {
                OkButtonAction();
            }

            GUI.enabled = true;
        }

        internal override void OkButtonAction()
        {
            // If the validation goes OK, this method closes the dialog
            RepositoryCreationValidation.AsyncValidation(
                BuildCreationData(),
                this,
                mProgressControls);
        }

        void OnEnterKeyAction()
        {
            if (!OrganizationsInformation.IsUnityOrganization(mSelectedServer))
            {
                OkButtonAction();
                return;
            }

            if (mKnownServers.Contains(mSelectedServer))
            {
                if (string.IsNullOrEmpty(mSelectedProject))
                {
                    OnServerSelected(mSelectedServer);
                }
                else
                {
                    OkButtonAction();
                }
            }
        }

        RepositoryCreationData BuildCreationData()
        {
            string resolvedServer = OrganizationsInformation.TryResolveServerFromInput(mSelectedServer);

            string repositoryName = mSelectedProject != null ?
                CloudProjectRepository.BuildFullyQualifiedName(mSelectedProject, mRepositoryName) :
                mRepositoryName;

            return new RepositoryCreationData(
                repositoryName,
                resolvedServer != null ? resolvedServer : mSelectedServer);
        }

        static CreateRepositoryDialog Create(
            IPlasticWebRestApi plasticWebRestApi,
            IPlasticAPI plasticApi,
            string proposedRepositoryName,
            string proposedServer)
        {
            var instance = CreateInstance<CreateRepositoryDialog>();
            instance.mEnterKeyAction = instance.OnEnterKeyAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            instance.mPlasticWebRestApi = plasticWebRestApi;
            instance.mPlasticApi = plasticApi;
            instance.BuildComponents(proposedRepositoryName, proposedServer);
            return instance;
        }

        void BuildComponents(string proposedRepositoryName, string proposedServer)
        {
            mSelectedServer = ResolveServer.ToDisplayString(proposedServer);
            mRepositoryName = proposedRepositoryName;

            if (OrganizationsInformation.IsUnityOrganization(proposedServer))
            {
                string[] repositoryNameParts = proposedRepositoryName.
                    Split(CloudProjectRepository.Separator);

                if (repositoryNameParts.Length > 1)
                {
                    mProposedProject = repositoryNameParts[0].Trim();
                    mRepositoryName = repositoryNameParts[repositoryNameParts.Length - 1].Trim();
                }
            }

            KnownServersListOperations.GetCombinedServers(
                true,
                GetExtraServers(proposedServer),
                mProgressControls,
                this,
                mPlasticWebRestApi,
                CmConnection.Get().GetProfileManager());
        }

        static List<string> GetExtraServers(string proposedServer)
        {
            List<string> result = new List<string>();
            if (!string.IsNullOrEmpty(proposedServer))
                result.Add(proposedServer);

            return result;
        }

        IPlasticAPI mPlasticApi;
        IPlasticWebRestApi mPlasticWebRestApi;
        bool mFocusIsAreadySet;

        string mRepositoryName;
        string mSelectedServer;
        string mSelectedProject;
        string mProposedProject;
        bool mIsLoadingProjects;
        List<string> mCurrentServerProjects;

        List<string> mKnownServers = new List<string>();

        ProgressControlsForDialogs.Data mProgressData = new ProgressControlsForDialogs.Data();

        const float ENTRY_WIDTH = 400;
        const float ENTRY_X = 175f;
        const string REPONAME_CONTROL_NAME = "CreateRepositoryDialog.RepositoryName";
    }
}
