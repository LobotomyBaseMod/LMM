using UnityEngine;

using Codice.Client.BaseCommands;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal class UVCSNotificationStatus
    {
        internal IncomingChangesStatus IncomingChanges { get; set; }

        internal WorkspaceStatusResult WorkspaceStatusResult { get; set; }

        internal enum IncomingChangesStatus
        {
            None,
            Changes,
            Conflicts
        }

        internal Texture2D GetIcon()
        {
            // conflicts has precedence over everything else
            if (IncomingChanges == IncomingChangesStatus.Conflicts)
                return Images.GetPlasticNotifyConflictIcon();

            // incoming changes scenarios
            if (IncomingChanges == IncomingChangesStatus.Changes)
            {
                if (WorkspaceStatusResult != null)
                {
                    // both incoming changes and pending changes
                    return Images.GetPlasticNotifyPendingChangesAndIncomingIcon();
                }

                // incoming changes only
                return Images.GetPlasticNotifyIncomingIcon();
            }

            // pending changes only
            if (WorkspaceStatusResult != null)
                return Images.GetPlasticNotifyPendingChangesIcon();

            // default state
            return Images.GetPlasticViewIcon();
        }

        internal string GetPendingChangesInfoTooltipText()
        {
            if (WorkspaceStatusResult == null)
                return null;

            return PlasticLocalization.Name.PendingChangesInfo.GetString(
                WorkspaceStatusResult.Changes.Count);
        }

        internal void Clean()
        {
            IncomingChanges = IncomingChangesStatus.None;
            WorkspaceStatusResult = null;
        }
    }
}
