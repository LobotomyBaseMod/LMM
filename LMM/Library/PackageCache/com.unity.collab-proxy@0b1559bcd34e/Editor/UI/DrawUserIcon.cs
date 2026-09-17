using System;

using Codice.Client.Common;

using Unity.PlasticSCM.Editor.UI.Avatar;

using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class DrawUserIcon
    {
        internal static void ForPendingChangesTab(
            ResolvedUser currentUser,
            Action avatarLoadedAction)
        {
            Rect rect = GUILayoutUtility.GetRect(28f, 28f, GUILayout.ExpandWidth(false));

            if (currentUser == null)
            {
                GUI.DrawTexture(rect, Images.GetEmptyGravatar());
                return;
            }

            GUI.Label(rect, new GUIContent(
                GetAvatar.ForEmail(currentUser.Name, avatarLoadedAction),
                currentUser.Name));
        }
    }
}
