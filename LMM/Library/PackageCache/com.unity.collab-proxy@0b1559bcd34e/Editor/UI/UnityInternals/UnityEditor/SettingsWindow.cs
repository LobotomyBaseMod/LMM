using UnityEditor;

using UnityEditorWindow = UnityEditor.EditorWindow;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal class SettingsWindow
    {
        internal object InternalObject { get; }

        internal SettingsWindow(object settingsWindow)
        {
            InternalObject = settingsWindow;
        }

        internal static SettingsWindow Show(SettingsScope scopes, string settingsPath)
        {
            return ShowInternal(scopes, settingsPath);
        }

        internal SettingsProvider GetCurrentProvider()
        {
            return GetCurrentProviderInternal(this);
        }

        internal void Close()
        {
            ((UnityEditorWindow)InternalObject).Close();
        }

        internal delegate SettingsWindow ShowDelegate(SettingsScope scopes, string settingsPath);
        internal static ShowDelegate ShowInternal { get; set; }

        internal delegate SettingsProvider GetCurrentProviderDelegate(SettingsWindow settingsWindow);
        internal static GetCurrentProviderDelegate GetCurrentProviderInternal { get; set; }
    }

}
