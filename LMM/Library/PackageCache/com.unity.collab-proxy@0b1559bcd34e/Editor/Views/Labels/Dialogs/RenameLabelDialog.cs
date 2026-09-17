using UnityEditor;

using Codice.CM.Common;

using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Labels;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Labels.Dialogs
{
    internal class RenameLabelDialog
    {
        internal static LabelRenameData GetLabelRenameData(
            RepositorySpec repSpec,
            MarkerInfo labelInfo,
            EditorWindow parentWindow)
        {
            InputTextDialogResult dialogResult = InputTextDialog.GetInputText(
                PlasticLocalization.Name.RenameLabelTitle.GetString(),
                null,
                PlasticLocalization.GetString(PlasticLocalization.Name.NewName),
                labelInfo.Name,
                PlasticLocalization.GetString(PlasticLocalization.Name.RenameButton),
                (text, closer, progressControls) =>
                {
                    LabelRenameData data = new LabelRenameData(
                        repSpec, labelInfo, text);
                    LabelRenameValidation.AsyncValidation(data, closer, progressControls);
                },
                parentWindow,
                width: 500);

            LabelRenameData result = new LabelRenameData(
                repSpec, labelInfo, dialogResult.Text);

            result.Result = dialogResult.Result;
            return result;
        }
    }
}
