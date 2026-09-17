using UnityEditor;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Branches.Dialogs
{
    internal class RenameBranchDialog
    {
        internal static BranchRenameData GetBranchRenameData(
            RepositorySpec repSpec,
            BranchInfo branchInfo,
            EditorWindow parentWindow)
        {
            InputTextDialogResult dialogResult = InputTextDialog.GetInputText(
                PlasticLocalization.Name.RenameBranchTitle.GetString(),
                null,
                PlasticLocalization.GetString(PlasticLocalization.Name.NewName),
                GetShorten.BranchNameFromString(branchInfo.BranchName),
                PlasticLocalization.GetString(PlasticLocalization.Name.RenameButton),
                (text, closer, progressControls) =>
                {
                    BranchRenameData data = new BranchRenameData(
                        repSpec, branchInfo, text);
                    BranchRenameValidation.AsyncValidation(data, closer, progressControls);
                },
                parentWindow,
                width: 500);

            BranchRenameData result = new BranchRenameData(
                repSpec, branchInfo, dialogResult.Text);

            result.Result = dialogResult.Result;
            return result;
        }
    }
}
