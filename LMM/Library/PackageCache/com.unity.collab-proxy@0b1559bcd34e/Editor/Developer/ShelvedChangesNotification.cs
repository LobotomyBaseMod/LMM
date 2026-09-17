using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Topbar;
using PlasticGui.WorkspaceWindow.Merge;
using Unity.PlasticSCM.Editor.Topbar;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Developer
{
    internal class ShelvedChangesNotification :
        NotificationsArea.IShelvedChangesNotification,
        CheckShelvedChanges.IUpdateShelvedChangesNotification
    {
        internal ShelvedChangesNotification(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ViewSwitcher viewSwitcher,
            UVCSWindow uvcsWindow)
        {
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mViewSwitcher = viewSwitcher;
            mUVCSWindow = uvcsWindow;
        }

        bool NotificationsArea.IShelvedChangesNotification.HasNotification
        {
            get { return mHasNotification; }
        }

        void NotificationsArea.IShelvedChangesNotification.SetWorkspaceWindow(
            WorkspaceWindow workspaceWindow)
        {
            mWorkspaceWindow = workspaceWindow;
        }

        void NotificationsArea.IShelvedChangesNotification.SetShelvedChangesUpdater(
            IShelvedChangesUpdater shelvedChangesUpdater)
        {
            mShelvedChangesUpdater = shelvedChangesUpdater;
        }

        void NotificationsArea.IShelvedChangesNotification.OnGUI()
        {
            Texture2D icon = Images.GetInfoBellNotificationIcon();

            NotificationsArea.DrawIcon(
                icon,
                UnityConstants.SHELVED_CHANGES_NOTIFICATION_ICON_SIZE,
                1);

            GUILayout.Space(1);

            NotificationsArea.DrawNotification(
                new GUIContentNotification(new GUIContent(
                    PlasticLocalization.Name.ShelvedChanges.GetString(),
                    PlasticLocalization.Name.ShelvedChangesExplanation.GetString())));

            GUILayout.Space(3);

            GenericMenu discardShelveDropdownMenu = new GenericMenu();
            discardShelveDropdownMenu.AddItem(
                new GUIContent(PlasticLocalization.Name.DiscardShelvedChanges.GetString()),
                false,
                () =>
                {
                    ShelvedChangesNotificationPanelOperations.DiscardShelvedChanges(
                        mWkInfo,
                        mShelveInfo,
                        this,
                        mShelvedChangesUpdater,
                        mViewSwitcher,
                        mWorkspaceWindow);
                });

            DrawActionButtonWithMenu.ForTopbar(
                PlasticLocalization.Name.ViewButton.GetString(),
                PlasticLocalization.Name.ViewShelvedChangesButtonExplanation.GetString(),
                () =>
                {
                    if (mShelveInfo == null || mViewSwitcher == null)
                        return;

                    ((IMergeViewLauncher)mViewSwitcher).MergeFrom(
                        mRepSpec,
                        mShelveInfo,
                        EnumMergeType.ChangesetCherryPick,
                        showDiscardChangesButton: true);
                },
                discardShelveDropdownMenu);
        }

        void CheckShelvedChanges.IUpdateShelvedChangesNotification.Hide(
            WorkspaceInfo wkInfo)
        {
            if (!wkInfo.Equals(mWkInfo))
                return;

            mShelveInfo = null;

            mHasNotification = false;

            mUVCSWindow.Repaint();
        }

        void CheckShelvedChanges.IUpdateShelvedChangesNotification.Show(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ChangesetInfo shelveInfo)
        {
            if (!wkInfo.Equals(mWkInfo))
                return;

            mShelveInfo = shelveInfo;

            mHasNotification = true;

            mUVCSWindow.Repaint();
        }

        bool mHasNotification;
        ChangesetInfo mShelveInfo;

        WorkspaceWindow mWorkspaceWindow;
        IShelvedChangesUpdater mShelvedChangesUpdater;

        readonly WorkspaceInfo mWkInfo;
        readonly RepositorySpec mRepSpec;
        readonly ViewSwitcher mViewSwitcher;
        readonly UVCSWindow mUVCSWindow;
    }
}
