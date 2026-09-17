using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Labels;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Changesets;

namespace Unity.PlasticSCM.Editor.Views.Labels.Dialogs
{
    class CreateLabelDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 710, baseRect.y);
            }
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.CreateLabelTitle.GetString();
        }

        protected override string GetExplanation()
        {
            if (mShowChangesetExplorer)
                return PlasticLocalization.Name.SelectChangesetBelow.GetString();

            return PlasticLocalization.Name.CreateLabelExplanation.GetString();
        }

        protected override void DoComponentsArea()
        {
            if (mShowChangesetExplorer)
            {
                // HACK: GetTitle is not called once the dialog is shown
                titleContent.text = PlasticLocalization.Name.AvailableChangesets.GetString();
                mChangesetExplorerView.OnGUI();

                return;
            }

            titleContent.text = PlasticLocalization.Name.CreateLabelTitle.GetString();
            mCreateLabelView.OnGUI();
        }

        internal static LabelCreationData CreateLabel(
            EditorWindow parentWindow,
            WorkspaceInfo wkInfo)
        {
            RepositorySpec repSpec = PlasticGui.Plastic.API.GetRepositorySpec(wkInfo);
            long currentChangesetId = PlasticGui.Plastic.API.GetCurrentChangesetOnWorkspace(wkInfo);

            CreateLabelDialog dialog = Create(wkInfo, repSpec, currentChangesetId);

            ResponseType dialogResult = dialog.RunModal(parentWindow);
            LabelCreationData result = mCreateLabelView.BuildCreationData();
            result.Result = dialogResult == ResponseType.Ok;
            return result;
        }

        static CreateLabelDialog Create(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            long changesetId)
        {
            CreateLabelDialog instance = CreateInstance<CreateLabelDialog>();
            instance.IsResizable = false;

            mCreateLabelView = new CreateLabelView(instance, repSpec);

            mChangesetExplorerView = new ChangesetExplorerView(
                instance, wkInfo, instance.mProgressControls);

            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CloseButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.CreateButton.GetString();

            instance.ToggleChangesetExplorer(false);
            instance.SetChangesetId(changesetId);

            return instance;
        }

        internal override void OkButtonAction()
        {
            if (mShowChangesetExplorer)
            {
                ChangesetInfo changesetInfo =
                    ChangesetsSelection.GetSelectedChangeset(mChangesetExplorerView.Table);

                if (changesetInfo == null)
                    return;

                SetChangesetId(changesetInfo.ChangesetId);

                ToggleChangesetExplorer(false);
                return;
            }

            LabelCreationValidation.AsyncValidation(
                mCreateLabelView.BuildCreationData(), this, mProgressControls);
        }

        internal void ToggleChangesetExplorer(bool show)
        {
            mShowChangesetExplorer = show;

            mOkButtonText = mShowChangesetExplorer ?
                PlasticLocalization.Name.OkButton.GetString() :
                PlasticLocalization.Name.CreateButton.GetString();
            mCancelButtonText = mShowChangesetExplorer ?
                PlasticLocalization.Name.BackButton.GetString() :
                PlasticLocalization.Name.CancelButton.GetString();
        }

        internal override void CancelButtonAction()
        {
            if (mShowChangesetExplorer)
            {
                ToggleChangesetExplorer(false);
                return;
            }

            base.CancelButtonAction();
        }

        internal void SetChangesetId(long changesetId)
        {
            mCreateLabelView.SetChangesetId(changesetId);
        }

        static CreateLabelView mCreateLabelView;
        static ChangesetExplorerView mChangesetExplorerView;

        static bool mShowChangesetExplorer;
    }
}
