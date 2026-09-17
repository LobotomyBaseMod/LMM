using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.StatusBar
{
    internal class WindowStatusBar
    {
        internal void Notify(INotificationContent content, MessageType type, Texture2D image)
        {
            mNotification = new Notification(
                content,
                type,
                image);
            mDelayedNotificationClearAction.Run();
        }

        internal NotificationBar NotificationBar { get; private set; }

        internal WindowStatusBar()
        {
            mDelayedNotificationClearAction = new DelayedActionBySecondsRunner(
                DelayedClearNotification,
                UnityConstants.NOTIFICATION_CLEAR_INTERVAL);

            NotificationBar = new NotificationBar();
        }

        void DelayedClearNotification()
        {
            mNotification = null;
        }

        internal void OnGUI()
        {
            if (NotificationBar.HasNotification &&
                NotificationBar.IsVisible)
            {
                DrawBar(NotificationBar.OnGUI);
            }

            DrawBar(DoContentArea);

            Rect lastRect = GUILayoutUtility.GetLastRect();

            if (MouseEntered(mIsMouseOver, lastRect))
            {
                mIsMouseOver = true;
                mDelayedNotificationClearAction.Pause();
            }

            if (MouseExited(mIsMouseOver, lastRect))
            {
                mIsMouseOver = false;
                mDelayedNotificationClearAction.Resume();
            }
        }

        void DrawBar(Action doContentArea)
        {
            using (new EditorGUILayout.VerticalScope(
                UnityStyles.StatusBar.Bar,
                GUILayout.Height(UnityConstants.STATUS_BAR_HEIGHT)))
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.HorizontalScope())
                {
                    doContentArea();
                }

                GUILayout.FlexibleSpace();
            }
        }

        void DoContentArea()
        {
            if (NotificationBar.HasNotification)
            {
                DrawNotificationAvailablePanel(NotificationBar);
            }

            if (mNotification != null)
                DrawNotification(mNotification);

            GUILayout.FlexibleSpace();
        }

        static void DrawNotification(INotificationContent notification)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            notification.OnGUI();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        static void DrawNotificationAvailablePanel(
            NotificationBar notificationBar)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(PlasticLocalization.GetString(
                    notificationBar.IsVisible ?
                        PlasticLocalization.Name.HideNotification :
                        PlasticLocalization.Name.ShowNotification)))
            {
                notificationBar.SetVisibility(!notificationBar.IsVisible);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        static void DrawNotification(Notification notification)
        {
            DrawIcon(notification.Image);
            DrawNotification(notification.Content);
        }

        static void DrawIcon(Texture2D icon, int size = UnityConstants.STATUS_BAR_ICON_SIZE)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.Label(
                icon,
                UnityStyles.StatusBar.Icon,
                GUILayout.Height(size),
                GUILayout.Width(size));

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        static bool MouseEntered(
            bool isMouseOver,
            Rect lastRect)
        {
            bool isInside = lastRect.Contains(Event.current.mousePosition);
            return isInside && !isMouseOver;
        }

        static bool MouseExited(
            bool isMouseOver,
            Rect lastRect)
        {
            bool isInside = lastRect.Contains(Event.current.mousePosition);
            return !isInside && isMouseOver;
        }

        class Notification
        {
            internal INotificationContent Content { get; private set; }
            internal MessageType MessageType { get; private set; }
            internal Texture2D Image { get; private set; }

            internal Notification(INotificationContent content, MessageType messageType, Texture2D image)
            {
                Content = content;
                MessageType = messageType;
                Image = image;
            }
        }

        Notification mNotification;
        bool mIsMouseOver = false;

        readonly DelayedActionBySecondsRunner mDelayedNotificationClearAction;
    }
}
