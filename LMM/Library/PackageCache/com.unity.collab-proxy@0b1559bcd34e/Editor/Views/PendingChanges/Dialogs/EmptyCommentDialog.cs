using UnityEditor;

using Codice.Client.Common;

using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs
{
    internal class EmptyCommentDialog
    {
        internal static bool ShouldContinueWithCheckin(
            EditorWindow parentWindow,
            WorkspaceInfo wkInfo)
        {
            return CanProceedWithEmptyComment(false, parentWindow, wkInfo);
        }

        internal static bool ShouldContinueWithShelve(
            EditorWindow parentWindow,
            WorkspaceInfo wkInfo)
        {
            return CanProceedWithEmptyComment(true, parentWindow, wkInfo);
        }

        static bool CanProceedWithEmptyComment(
            bool bShelve,
            EditorWindow parentWindow,
            WorkspaceInfo wkInfo)
        {
            bool checkBoxValue;
            GuiMessage.GuiMessageResponseButton result = DialogWithCheckBox.Show(
                bShelve ?
                    PlasticLocalization.Name.NoShelveCommentTitle.GetString() :
                    PlasticLocalization.Name.NoCheckinCommentTitle.GetString(),
                bShelve ?
                    PlasticLocalization.Name.NoShelveCommentMessage.GetString() :
                    PlasticLocalization.Name.NoCheckinCommentMessage.GetString(),
                bShelve ?
                    PlasticLocalization.Name.SkipAndShelve.GetString() :
                    PlasticLocalization.Name.SkipAndCheckin.GetString(),
                string.Empty,
                PlasticLocalization.Name.AddComment.GetString(),
                GuiMessage.GuiMessageType.Informational,
                new MultiLinkLabelData(PlasticLocalization.Name.DoNotShowMessageAgain.GetString()),
                parentWindow,
                out checkBoxValue);

            if (result == GuiMessage.GuiMessageResponseButton.Neutral ||
                result == GuiMessage.GuiMessageResponseButton.None)
                return false;

            if (checkBoxValue)
            {
                TrackFeatureUseEvent.For(
                    PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                    TrackFeatureUseEvent.Features.EmptyComment.PendingChangesCheckinDialogDoNotShowMessageAgain);

                if (bShelve)
                    PlasticGuiConfig.Get().Configuration.ShowEmptyShelveCommentWarning = false;
                else
                    PlasticGuiConfig.Get().Configuration.ShowEmptyCommentWarning = false;
                PlasticGuiConfig.Get().Save();
            }

            if (result == GuiMessage.GuiMessageResponseButton.Positive)
            {
                TrackFeatureUseEvent.For(
                    PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                    TrackFeatureUseEvent.Features.EmptyComment.PendingChangesCheckinDialogCheckinAnyway);
                return true;
            }

            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                TrackFeatureUseEvent.Features.EmptyComment.PendingChangesCheckinDialogCancel);
            return false;
        }
    }
}
