using UnityEditor;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Attributes;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Attributes
{
    internal class AttributeDataDialog : PlasticDialog
    {
        internal static ApplyAttributeData BuildForApplyAttribute(
            RepositorySpec repSpec, IWorkspaceWindow workspaceWindow, EditorWindow parentWindow)
        {
            AttributeDataDialog dialog = Create(null, repSpec, workspaceWindow);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            ApplyAttributeData result = new ApplyAttributeData(
                dialog.mApplyAttributeView.SelectedAttributeName,
                dialog.mApplyAttributeView.SelectedAttributeValue);

            result.Result = dialogResult == ResponseType.Ok;
            return result;
        }

        internal static ApplyAttributeData BuildForEditAttributeRealization(
            AttributeRealizationInfo attribute,
            RepositorySpec repSpec,
            IWorkspaceWindow workspaceWindow,
            EditorWindow parentWindow)
        {
            AttributeDataDialog dialog = Create(attribute, repSpec, workspaceWindow);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            ApplyAttributeData result = new ApplyAttributeData(
                dialog.mApplyAttributeView.SelectedAttributeName,
                dialog.mApplyAttributeView.SelectedAttributeValue);

            result.Result = dialogResult == ResponseType.Ok;
            return result;
        }

        static AttributeDataDialog Create(
            AttributeRealizationInfo attribute,
            RepositorySpec repSpec,
            IWorkspaceWindow workspaceWindow)
        {
            var instance = CreateInstance<AttributeDataDialog>();

            instance.mRepSpec = repSpec;
            instance.mIsEditingAttribute = attribute != null;

            instance.mApplyAttributeView = new ApplyAttributeView(
                repSpec, instance.mProgressControls, instance);

            if (attribute != null)
            {
                instance.mApplyAttributeView.SetAttributeToEdit(attribute);
                return instance;
            }

            instance.mCreateAttributeView = new CreateAttributeView(
                repSpec,
                workspaceWindow,
                instance.mApplyAttributeView,
                instance.mProgressControls,
                instance);

            ApplyAttributeDialogOperations.LoadAttributes(
                repSpec, instance.mProgressControls, instance.mApplyAttributeView);

            instance.ToggleCreateAttributeView(false);

            return instance;
        }

        protected override string GetTitle()
        {
            if (mShowCreateAttributeView)
                return PlasticLocalization.Name.CreateAttribute.GetString();

            if (mIsEditingAttribute)
                return PlasticLocalization.Name.EditAttributeTitle.GetString();

            return PlasticLocalization.Name.ApplyAttribute.GetString();
        }

        protected override string GetExplanation()
        {
            if (mShowCreateAttributeView)
                return PlasticLocalization.Name.CreateAttributeExplanation.GetString(mRepSpec);

            return PlasticLocalization.Name.ApplyAttributeExplanation.GetString();
        }

        internal override void OkButtonAction()
        {
            if (mShowCreateAttributeView)
            {
                mCreateAttributeView.OkButtonAction();
                return;
            }

            mApplyAttributeView.OkButtonAction();
        }

        internal override void CancelButtonAction()
        {
            if (mShowCreateAttributeView)
            {
                ToggleCreateAttributeView(false);
                return;
            }

            base.CancelButtonAction();
        }

        internal void ToggleCreateAttributeView(bool show)
        {
            mShowCreateAttributeView = show;

            mOkButtonText = mShowCreateAttributeView ?
                PlasticLocalization.Name.CreateButton.GetString() :
                PlasticLocalization.Name.ApplyButton.GetString();
            mCancelButtonText = mShowCreateAttributeView ?
                PlasticLocalization.Name.BackButton.GetString() :
                PlasticLocalization.Name.CancelButton.GetString();

            UnityInternals.UnityEditor.EditorGUI.activeEditor?.EndEditing();
        }

        protected override void DoComponentsArea()
        {
            // HACK: GetTitle is not called once the dialog is shown
            titleContent.text = GetTitle();

            if (mShowCreateAttributeView)
            {
                mCreateAttributeView.OnGUI();
                return;
            }

            mApplyAttributeView.OnGUI();
        }

        ApplyAttributeView mApplyAttributeView;
        CreateAttributeView mCreateAttributeView;

        RepositorySpec mRepSpec;

        bool mIsEditingAttribute;
        bool mShowCreateAttributeView;
    }
}
