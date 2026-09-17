using System;
using System.ComponentModel;

using UnityEngine;

namespace Unity.PlasticSCM.Editor
{
    // Placeholder. This isn't a public API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("PlasticPlugin is deprecated and will be removed in a future release", false)]
    public static class PlasticPlugin
    {
        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable 0067 // The event 'PlasticPlugin.OnNotificationUpdated' is never used
        public static event Action OnNotificationUpdated;
#pragma warning restore 0067

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void OpenPlasticWindowDisablingOfflineModeIfNeeded()
        {
        }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Texture GetPluginStatusIcon()
        {
            return null;
        }
    }
}
