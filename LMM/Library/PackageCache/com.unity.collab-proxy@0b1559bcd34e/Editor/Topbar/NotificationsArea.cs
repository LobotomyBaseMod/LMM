using UnityEditor;
using UnityEngine;

using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.Developer;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Topbar
{
    internal class NotificationsArea
    {
        internal interface IIncomingChangesNotification
        {
            bool HasNotification { get; }
            void OnGUI();
            void Show(
                string infoText,
                string actionText,
                string tooltipText,
                bool hasUpdateAction,
                UVCSNotificationStatus.IncomingChangesStatus status);
            void Hide();
        }

        internal interface IShelvedChangesNotification :
            CheckShelvedChanges.IUpdateShelvedChangesNotification
        {
            bool HasNotification { get; }
            void SetWorkspaceWindow(
                WorkspaceWindow workspaceWindow);
            void SetShelvedChangesUpdater(
                IShelvedChangesUpdater shelvedChangesUpdater);
            void OnGUI();
        }

        internal class IncomingChangesNotificationData
        {
            internal string InfoText { get; private set; }
            internal string ActionText { get; private set; }
            internal string TooltipText { get; private set; }
            internal bool HasUpdateAction { get; private set; }
            internal UVCSNotificationStatus.IncomingChangesStatus Status { get; private set; }

            internal void UpdateData(
                string infoText,
                string actionText,
                string tooltipText,
                bool hasUpdateAction,
                UVCSNotificationStatus.IncomingChangesStatus status)
            {
                InfoText = infoText;
                ActionText = actionText;
                TooltipText = tooltipText;
                HasUpdateAction = hasUpdateAction;
                Status = status;
            }

            internal void Clear()
            {
                InfoText = string.Empty;
                ActionText = string.Empty;
                TooltipText = string.Empty;
                HasUpdateAction = false;
                Status = UVCSNotificationStatus.IncomingChangesStatus.None;
            }
        }

        internal NotificationsArea(
            WorkspaceWindow workspaceWindow,
            IIncomingChangesNotification incomingChangesNotification,
            IShelvedChangesNotification shelvedChangesNotification)
        {
            mIncomingChangesNotification = incomingChangesNotification;
            mShelvedChangesNotification = shelvedChangesNotification;

            if (incomingChangesNotification is IncomingChangesNotification)
                ((IncomingChangesNotification)incomingChangesNotification).SetWorkspaceWindow(workspaceWindow);

            shelvedChangesNotification.SetWorkspaceWindow(workspaceWindow);
        }

        internal void OnGUI()
        {
            if (mIncomingChangesNotification.HasNotification)
            {
                mIncomingChangesNotification.OnGUI();
            }

            if (mShelvedChangesNotification.HasNotification)
            {
                if (mIncomingChangesNotification.HasNotification)
                    EditorGUILayout.Space(10);

                mShelvedChangesNotification.OnGUI();
            }
        }

        internal static void DrawIcon(
            Texture2D icon,
            int size = UnityConstants.STATUS_BAR_ICON_SIZE,
            int marginTop = 0)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Space(marginTop);

            GUILayout.Label(
                icon,
                UnityStyles.StatusBar.Icon,
                GUILayout.Height(size),
                GUILayout.Width(size));

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        internal static void DrawNotification(INotificationContent notification)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            notification.OnGUI();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        internal static bool DrawButton(GUIContent content)
        {
            Rect rt = GUILayoutUtility.GetRect(
                content,
                UnityStyles.Topbar.Button,
                GUILayout.Width(60));

            return GUI.Button(
                rt,
                content,
                UnityStyles.Topbar.Button);
        }

        IIncomingChangesNotification mIncomingChangesNotification;
        IShelvedChangesNotification mShelvedChangesNotification;
    }
}
