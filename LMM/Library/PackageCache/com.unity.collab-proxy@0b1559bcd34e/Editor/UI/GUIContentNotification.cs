using UnityEngine;

using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class GUIContentNotification : INotificationContent
    {
        internal GUIContentNotification(string content) : this(new GUIContent(content)) { }

        internal GUIContentNotification(GUIContent content)
        {
            mGUIContent = content;
        }

        void INotificationContent.OnGUI()
        {
            GUILayout.Label(
                mGUIContent,
                UnityStyles.StatusBar.NotificationLabel);
        }

        readonly GUIContent mGUIContent;
    }
}
