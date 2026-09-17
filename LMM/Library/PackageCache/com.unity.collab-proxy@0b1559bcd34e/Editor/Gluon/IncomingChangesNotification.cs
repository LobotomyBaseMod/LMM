using UnityEngine;

using Codice.CM.Common;
using PlasticGui.Gluon;
using PlasticGui.Gluon.WorkspaceWindow;
using Unity.PlasticSCM.Editor.Topbar;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Gluon
{
    internal class IncomingChangesNotification : NotificationsArea.IIncomingChangesNotification
    {
        internal IncomingChangesNotification(
            WorkspaceInfo wkInfo,
            IGluonViewSwitcher gluonViewSwitcher)
        {
            mWkInfo = wkInfo;
            mGluonViewSwitcher = gluonViewSwitcher;
        }

        bool NotificationsArea.IIncomingChangesNotification.HasNotification
        {
            get { return mHasNotification; }
        }

        void NotificationsArea.IIncomingChangesNotification.OnGUI()
        {
            Texture2D icon = mData.Status == UVCSNotificationStatus.IncomingChangesStatus.Conflicts ?
                Images.GetConflictedIcon() :
                Images.GetOutOfSyncIcon();

            NotificationsArea.DrawIcon(
                icon,
                UnityConstants.INCOMING_CHANGES_NOTIFICATION_ICON_SIZE);

            NotificationsArea.DrawNotification(new GUIContentNotification(
                new GUIContent(mData.InfoText)));

            GUILayout.Space(3);

            if (NotificationsArea.DrawButton(new GUIContent(mData.ActionText, mData.TooltipText)))
            {
                ShowIncomingChanges.FromNotificationBar(mWkInfo, mGluonViewSwitcher);
            }
        }

        void NotificationsArea.IIncomingChangesNotification.Show(
            string infoText,
            string actionText,
            string tooltipText,
            bool hasUpdateAction,
            UVCSNotificationStatus.IncomingChangesStatus status)
        {
            mData.UpdateData(
                infoText,
                actionText,
                tooltipText,
                hasUpdateAction,
                status);

            mHasNotification = true;
        }

        void NotificationsArea.IIncomingChangesNotification.Hide()
        {
            mData.Clear();

            mHasNotification = false;
        }

        bool mHasNotification;
        NotificationsArea.IncomingChangesNotificationData mData = new NotificationsArea.IncomingChangesNotificationData();

        readonly IGluonViewSwitcher mGluonViewSwitcher;
        readonly WorkspaceInfo mWkInfo;
    }
}
