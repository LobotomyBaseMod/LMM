using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.CloudDrive.Workspaces;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace
{
    class CreateWorkspaceDialog : PlasticDialog, IPlasticDialogCloser
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 850, 560);
            }
        }

        internal static WorkspaceCreationData CreateWorkspace(
            string proposedOrganization,
            string proposedProject,
            IPlasticWebRestApi restApi,
            IPlasticAPI plasticApi,
            EditorWindow parentWindow)
        {
            CreateWorkspaceDialog dialog = Create(
                proposedOrganization,
                proposedProject,
                restApi,
                plasticApi,
                parentWindow,
                null);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            WorkspaceCreationData result = mCreateWorkspacePanel.BuildCreationData();
            result.Result = dialogResult == ResponseType.Ok;
            return result;
        }

        internal static void CreateWorkspace(
            IPlasticWebRestApi restApi,
            IPlasticAPI plasticApi,
            EditorWindow parentWindow,
            Action<WorkspaceInfo> successOperationDelegate)
        {
            CreateWorkspaceDialog dialog = Create(
                string.Empty,
                string.Empty,
                restApi,
                plasticApi,
                parentWindow,
                successOperationDelegate);

            dialog.RunModal(parentWindow);
        }

        void IPlasticDialogCloser.CloseDialog()
        {
            if (mSuccessOperationDelegate == null)
            {
                OkButtonAction();
                return;
            }

            WorkspaceCreationData wkCreationData = mCreateWorkspacePanel.BuildCreationData();
            wkCreationData.Result = true;

            CreateWorkspaceOperation.CreateWorkspace(
                wkCreationData,
                PlasticGui.Plastic.API,
                mProgressControls,
                (createdWorkspace) =>
                {
                    OkButtonAction();
                    mSuccessOperationDelegate(createdWorkspace);
                });
        }

        static CreateWorkspaceDialog Create(
            string proposedOrganization,
            string proposedProject,
            IPlasticWebRestApi restApi,
            IPlasticAPI plasticApi,
            EditorWindow parentWindow,
            Action<WorkspaceInfo> successOperationDelegate)
        {
            var instance = CreateInstance<CreateWorkspaceDialog>();
            instance.IsResizable = false;

            mCreateWorkspacePanel = new CreateWorkspacePanel(
                proposedOrganization,
                proposedProject,
                restApi,
                plasticApi,
                parentWindow,
                instance.mProgressControls);

            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            instance.mTitle = PlasticLocalization.Name.CreateCloudWorkspaceTitle.GetString();
            instance.mSuccessOperationDelegate = successOperationDelegate;
            return instance;
        }

        protected override string GetTitle()
        {
            return mTitle;
        }

        protected override void DoComponentsArea()
        {
            bool isOperationRunning = mProgressControls.ProgressData.IsWaitingAsyncResult;

            mCreateWorkspacePanel.OnGUI(isOperationRunning);
        }

        protected override void DoOkButton()
        {
            if (mProgressControls.ProgressData.IsWaitingAsyncResult)
                GUI.enabled = false;

            if (NormalButton(PlasticLocalization.Name.CreateButton.GetString()))
            {
                OkButtonAction();
            }

            GUI.enabled = true;
        }

        internal override void OkButtonAction()
        {
            WorkspaceCreationValidation.AsyncValidation(
                mCreateWorkspacePanel.BuildCreationData(), this, mProgressControls);
        }

        string mTitle;
        Action<WorkspaceInfo> mSuccessOperationDelegate;

        static CreateWorkspacePanel mCreateWorkspacePanel;
    }
}
