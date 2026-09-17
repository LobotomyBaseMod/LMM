using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.ShareWorkspace
{
    internal class ShareWorkspaceDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 750, 450);
            }
        }

        internal static bool ShareWorkspace(
            WorkspaceInfo workspaceInfo,
            EditorWindow parentWindow,
            out List<SecurityMember> collaboratorsToAdd,
            out List<SecurityMember> collaboratorsToRemove)
        {
            ShareWorkspaceDialog dialog = Create(workspaceInfo, parentWindow.Repaint);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            collaboratorsToAdd = mShareWorkspacePanel.GetCollaboratorsToAdd();
            collaboratorsToRemove = mShareWorkspacePanel.GetCollaboratorsToRemove();
            return dialogResult == ResponseType.Ok;
        }

        static ShareWorkspaceDialog Create(WorkspaceInfo workspaceInfo, Action repaintAction)
        {
            var instance = CreateInstance<ShareWorkspaceDialog>();
            instance.IsResizable = false;

            mShareWorkspacePanel = new ShareWorkspacePanel(
                workspaceInfo, instance.mProgressControls, repaintAction);

            RepositorySpec repSpec = PlasticGui.Plastic.API.GetRepositorySpec(workspaceInfo);

            mShareWorkspacePanel.Refresh(
               repSpec.Server,
               CloudProjectRepository.GetProjectName(repSpec.Name));

            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.ShareButton.GetString();
            return instance;
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.ShareWorkspaceDialogTitle.GetString();
        }

        protected override void DoComponentsArea()
        {
            mShareWorkspacePanel.OnGUI(mProgressControls.ProgressData.IsWaitingAsyncResult);
        }

        internal override void OkButtonAction()
        {
            if (mShareWorkspacePanel.GetCollaboratorsToAdd().Count == 0 &&
                mShareWorkspacePanel.GetCollaboratorsToRemove().Count == 0)
            {
                ((IProgressControls)mProgressControls).ShowError(
                    PlasticLocalization.Name.CollaboratorsUnchanged.GetString());
                return;
            }

            base.OkButtonAction();
        }

        static ShareWorkspacePanel mShareWorkspacePanel;
    }
}
