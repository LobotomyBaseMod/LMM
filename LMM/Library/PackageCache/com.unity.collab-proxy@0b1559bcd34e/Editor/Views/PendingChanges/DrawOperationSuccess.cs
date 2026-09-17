using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common.Threading;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.StatusBar;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.WebApi;

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal interface IDrawOperationSuccess
    {
        void InStatusBar(WindowStatusBar windowStatusBar);
        void InEmptyState(Rect rect);
    }

    internal class NotifySuccessForCreatedChangeset : IDrawOperationSuccess
    {
        internal NotifySuccessForCreatedChangeset(
            CreatedChangesetData createdChangesetData,
            Action openLink,
            Action copyLink,
            Action repaint)
        {
            mCreatedChangesetData = createdChangesetData;
            mOpenLink = openLink;
            mCopyLink = copyLink;
            mRepaint = repaint;
            mEmptyStatePanel = new CreatedChangesetEmptyStatePanel(repaint);
        }

        void IDrawOperationSuccess.InStatusBar(WindowStatusBar WindowStatusBar)
        {
            INotificationContent notificationContent =
                new PendingChangesStatusSuccessNotificationContent(
                    mCreatedChangesetData,
                    mOpenLink,
                    mCopyLink);

            WindowStatusBar.Notify(
                notificationContent,
                MessageType.None,
                Images.GetStepOkIcon());
        }

        void IDrawOperationSuccess.InEmptyState(Rect rect)
        {
            if (!mCanInviteMembersFromPendingChangesAlreadyCalculated &&
                mCreatedChangesetData.OperationType == CreatedChangesetData.Type.Checkin)
            {
                EnableInviteMembersIfFirstCheckinAndAdmin(mCreatedChangesetData.RepositorySpec.Server);
                mCanInviteMembersFromPendingChangesAlreadyCalculated = true;
            }

            mEmptyStatePanel.UpdateContent(
                mCreatedChangesetData,
                mOpenLink,
                mCopyLink,
                mCanInviteMembersFromPendingChanges);
            mEmptyStatePanel.OnGUI(rect);
        }

        void EnableInviteMembersIfFirstCheckinAndAdmin(string server)
        {
            if (!PlasticGui.Plastic.API.IsCloud(server))
                return;

            bool isFirstCheckin = !BoolSetting.Load(
                UnityConstants.FIRST_CHECKIN_SUBMITTED, false);

            if (!isFirstCheckin)
                return;

            BoolSetting.Save(true, UnityConstants.FIRST_CHECKIN_SUBMITTED);

            string organizationName = ServerOrganizationParser.GetOrganizationFromServer(server);

            CurrentUserAdminCheckResponse response = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(50);
            waiter.Execute(
                /*threadOperationDelegate*/
                delegate
                {
                    string authToken = AuthToken.GetForServer(server);

                    if (string.IsNullOrEmpty(authToken))
                        return;

                    response = WebRestApiClient.PlasticScm.IsUserAdmin(organizationName, authToken);
                },
                /*afterOperationDelegate*/
                delegate
                {
                    if (response == null || !response.IsCurrentUserAdmin)
                        return;

                    mCanInviteMembersFromPendingChanges = true;

                    mRepaint();
                });
        }

        bool mCanInviteMembersFromPendingChangesAlreadyCalculated;
        bool mCanInviteMembersFromPendingChanges;

        readonly Action mRepaint;
        readonly Action mCopyLink;
        readonly Action mOpenLink;
        readonly CreatedChangesetData mCreatedChangesetData;
        readonly CreatedChangesetEmptyStatePanel mEmptyStatePanel;
    }

    internal class NotifySuccessForUndo : IDrawOperationSuccess
    {
        internal NotifySuccessForUndo(Action repaint)
        {
            mEmptyStatePanel = new EmptyStatePanel(repaint);
        }

        void IDrawOperationSuccess.InStatusBar(WindowStatusBar windowStatusBar)
        {
            INotificationContent notificationContent = new GUIContentNotification(
                PlasticLocalization.Name.UndoCompleted.GetString());

            windowStatusBar.Notify(
                notificationContent,
                MessageType.None,
                Images.GetStepOkIcon());
        }

        void IDrawOperationSuccess.InEmptyState(Rect rect)
        {
            mEmptyStatePanel.UpdateContent(
                PlasticLocalization.Name.UndoCompleted.GetString(),
                bDrawOkIcon: true);
            mEmptyStatePanel.OnGUI(rect);
        }

        readonly EmptyStatePanel mEmptyStatePanel;
    }
}
