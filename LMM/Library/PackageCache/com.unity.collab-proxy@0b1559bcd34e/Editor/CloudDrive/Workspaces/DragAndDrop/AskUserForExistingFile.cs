using UnityEditor;

using Codice.Client.Common;
using PlasticGui;
using PlasticGui.CloudDrive;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DragAndDrop
{
    internal class AskUserForExistingFile : CopyPathsOperation.IAskUserForExistingFile
    {
        internal AskUserForExistingFile(EditorWindow parentWindow)
        {
            mParentWindow = parentWindow;
        }

        CopyPathsOperation.CopyAction CopyPathsOperation.IAskUserForExistingFile.Ask(
            string filePath,
            bool showApplyToAllCheckbox,
            out bool applyToAll)
        {
            GuiMessage.GuiMessageResponseButton response = DialogWithCheckBox.Show(
                PlasticLocalization.Name.AskUserForExistingFileTitle.GetString(),
                PlasticLocalization.Name.AskUserForExistingFileMessage.GetString(
                    AssetsPath.GetFullPath.ForPath(filePath)),
                PlasticLocalization.Name.ReplaceButton.GetString(),
                PlasticLocalization.Name.KeepBothButton.GetString(),
                PlasticLocalization.Name.SkipButton.GetString(),
                GuiMessage.GuiMessageType.Warning,
                showApplyToAllCheckbox ?
                    new MultiLinkLabelData(PlasticLocalization.Name.ApplyToAllFollowingFiles.GetString()) :
                    null,
                mParentWindow,
                out applyToAll);

            switch (response)
            {
                case GuiMessage.GuiMessageResponseButton.Positive:
                    return CopyPathsOperation.CopyAction.Replace;

                case GuiMessage.GuiMessageResponseButton.Neutral:
                    return CopyPathsOperation.CopyAction.KeepBoth;

                case GuiMessage.GuiMessageResponseButton.Negative:
                    return CopyPathsOperation.CopyAction.Skip;

                case GuiMessage.GuiMessageResponseButton.None:
                default:
                    return CopyPathsOperation.CopyAction.Cancel;
            }
        }

        readonly EditorWindow mParentWindow;
    }
}
