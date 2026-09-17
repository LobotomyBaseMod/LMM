using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Branches.Dialogs
{
    internal class DeleteBranchDialog : PlasticDialog
    {
        internal static bool ConfirmDelete(
            EditorWindow parentWindow,
            IList<BranchInfo> branches)
        {
            DeleteBranchDialog dialog = Create(branches);

            return dialog.RunModal(parentWindow) == ResponseType.Ok;
        }

        protected override string GetExplanation()
        {
            return mMessage;
        }

        protected override string GetTitle()
        {
            return mTitle;
        }

        protected override void DoOkButton()
        {
            GUI.enabled = IsDeleteButtonEnabled();

            if (NormalButton(PlasticLocalization.Name.DeleteButton.GetString()))
                OkButtonAction();

            GUI.enabled = true;
        }

        protected override void DoButtonsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                mConfirmDelete = EntryBuilder.CreateToggleEntry(
                    PlasticLocalization.Name.ConfirmationCheckBox.GetString(),
                    mConfirmDelete);

                GUILayout.Space(10);

                DoButtonsWithPlatformOrdering(DoOkButton, DoCloseButton, DoCancelButton);
            }
        }

        internal override void OkButtonAction()
        {
            if (!IsDeleteButtonEnabled())
                return;

            base.OkButtonAction();
        }

        bool IsDeleteButtonEnabled()
        {
            return mConfirmDelete;
        }

        static DeleteBranchDialog Create(IList<BranchInfo> branches)
        {
            var instance = CreateInstance<DeleteBranchDialog>();
            instance.mMessage = DeleteObjectsAlert.BuildDeleteBranchConfirmationMessage(branches);
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            instance.mCancelButtonText = PlasticLocalization.Name.NoButton.GetString();
            instance.mNumberOfBranches = branches.Count;
            instance.mTitle = PlasticLocalization.Name.ConfirmDeleteTitle.GetString();
            return instance;
        }

        const int MAX_ITEMS_TO_SHOW = 10;

        string mMessage;
        string mTitle;
        int mNumberOfBranches;
        bool mConfirmDelete;
    }
}
