using System;
using System.Collections.Generic;

using UnityEngine;

using Codice.Client.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal class CreatedChangesetEmptyStatePanel : CenteredContentPanel
    {
        internal CreatedChangesetEmptyStatePanel(Action repaintAction)
            : base(repaintAction)
        {
        }

        internal void UpdateContent(
            CreatedChangesetData data,
            Action openLink,
            Action copyLink,
            bool bDrawInviteLink)
        {
            mData = data;
            mOpenLink = openLink;
            mCopyLink = copyLink;
            mbDrawInviteLink = bDrawInviteLink;
        }

        protected override void DrawGUI()
        {
            CenterContent(BuildDrawActions(
                mData, mOpenLink, mCopyLink, mbDrawInviteLink).ToArray());
        }

        static List<Action> BuildDrawActions(
            CreatedChangesetData data,
            Action openLink,
            Action copyLink,
            bool hasInviteLink)
        {
            List<Action> result = new List<Action>()
            {
                () =>
                {
                    DrawNotifySuccessData(data, openLink, copyLink);
                }
            };

            if (!hasInviteLink)
                return result;

            result.Add(
                () =>
                {
                    DrawInviteLink(data);
                });

            return result;
        }

        static void DrawNotifySuccessData(
            CreatedChangesetData data,
            Action openLink,
            Action copyLink)
        {
            if (data.OperationType == CreatedChangesetData.Type.Checkin)
            {
                DrawCheckinSuccessMessage(
                    data.CreatedChangesetId,
                    openLink,
                    copyLink);
                return;
            }

            DrawShelveSuccessMessage(
                data.CreatedChangesetId,
                openLink,
                copyLink);
        }

        static void DrawCheckinSuccessMessage(
            long changesetId,
            Action openChangesetLink,
            Action copyChangesetLink)
        {
            string text = string.Concat(
                PlasticLocalization.Name.CheckinCompleted.GetString(),
                " ",
                "{0} " + PlasticLocalization.Name.CheckinChangesetWasCreatedPart.GetString());

            string linkText =
                string.Format("{0} {1}",
                PlasticLocalization.Name.Changeset.GetString(),
                changesetId.ToString());

            DrawCreatedChangesetMessage(
                text,
                linkText,
                openChangesetLink,
                copyChangesetLink);
        }

        static void DrawShelveSuccessMessage(
            long shelvesetId,
            Action openShelveLink,
            Action copyShelveLink)
        {
            string text = PlasticLocalization.Name.ShelveCreatedMessage.GetString() + ".";
            string linkText = string.Format("{0} {1}",
                PlasticLocalization.Name.Shelve.GetString().ToLower(),
                Math.Abs(shelvesetId).ToString());

            DrawCreatedChangesetMessage(
                text,
                linkText,
                openShelveLink,
                copyShelveLink);
        }

        static void DrawCreatedChangesetMessage(
            string text,
            string actionText,
            Action openLink,
            Action copyLink)
        {
            GUILayout.Label(Images.GetStepOkIcon(), UnityStyles.EmptyState.Icon);

            GUILayout.Space(UnityConstants.EMPTY_STATE_HORIZONTAL_PADDING);

            DrawTextBlockWithLink.ForMultiLinkLabel(
                new MultiLinkLabelData(
                    text,
                    new List<string> { actionText },
                    new List<Action> { openLink }),
                UnityStyles.EmptyState.LabelForMultiLinkLabel,
                UnityStyles.EmptyState.LinkForMultiLinkLabel);

            GUILayout.Space(UnityConstants.EMPTY_STATE_HORIZONTAL_PADDING);

            if (GUILayout.Button(
                new GUIContent(
                    Images.GetClipboardIcon(),
                    PlasticLocalization.Name.DiffLinkButtonTooltip.GetString()),
                UnityStyles.EmptyState.CopyToClipboardButton))
            {
                copyLink();
            }
        }

        static void DrawInviteLink(CreatedChangesetData data)
        {
            if (GUILayout.Button(
                    new GUIContent(PlasticLocalization.Name.InviteOtherTeamMembers.GetString()),
                    UnityStyles.EmptyState.Link))
            {
                OpenInviteUsersPage.Run(
                    data.RepositorySpec,
                    UnityUrl.UnityDashboard.UnityCloudRequestSource.Editor);
            }
        }

        CreatedChangesetData mData;
        Action mOpenLink;
        Action mCopyLink;
        bool mbDrawInviteLink;
    }
}
