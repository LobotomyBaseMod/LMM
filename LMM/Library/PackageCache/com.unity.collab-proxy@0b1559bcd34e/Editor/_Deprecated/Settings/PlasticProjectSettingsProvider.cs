using System;
using System.ComponentModel;

using UnityEditor;

namespace Unity.PlasticSCM.Editor.Settings
{
    // Placeholder. This isn't a public API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("PlasticProjectSettingsProvider is deprecated and will be removed in a future release", false)]
    public class PlasticProjectSettingsProvider : SettingsProvider
    {
        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PlasticProjectSettingsProvider(
            string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope)
        {
        }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return null;
        }
    }
}
