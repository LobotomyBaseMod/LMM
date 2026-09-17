using System;

using UnityEditor;

using Codice.Client.Common.Threading;
using Unity.PlasticSCM.Editor.UI;

#if !UNITY_6000_3_OR_NEWER
using SettingsWindow = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SettingsWindow;
#endif

namespace Unity.PlasticSCM.Editor.Settings
{
    internal static class OpenUVCSProjectSettings
    {
        internal static void ByDefault()
        {
            UVCSProjectSettingsProvider provider = OpenInUVCSProjectSettings();

            if (provider == null)
                return;

            provider.OpenAllFoldouts();
        }

        internal static void InDiffAndMergeFoldout()
        {
            UVCSProjectSettingsProvider provider = OpenInUVCSProjectSettings();

            if (provider == null)
                return;

            provider.OpenDiffAndMergeFoldout();
        }

        internal static void InShelveAndSwitchFoldout()
        {
            UVCSProjectSettingsProvider provider = OpenInUVCSProjectSettings();

            if (provider == null)
                return;

            provider.OpenShelveAndSwitchFoldout();
        }

        internal static void InOtherFoldout()
        {
            UVCSProjectSettingsProvider provider = OpenInUVCSProjectSettings();

            if (provider == null)
                return;

            provider.OpenOtherFoldout();
        }

        internal static UVCSProjectSettingsProvider OpenInUVCSProjectSettings()
        {
            SettingsWindow settingsWindow = OpenProjectSettingsWithUVCSSelected();
            return GetUVCSProvider(settingsWindow);
        }

        internal static SettingsWindow OpenProjectSettingsWithUVCSSelected()
        {
            return SettingsWindow.Show(SettingsScope.Project, UnityConstants.PROJECT_SETTINGS_TAB_PATH);
        }

        internal static UVCSProjectSettingsProvider GetUVCSProvider(
            SettingsWindow settingsWindow)
        {
            try
            {
                SettingsProvider provider = settingsWindow.GetCurrentProvider();
                return provider as UVCSProjectSettingsProvider;
            }
            catch (Exception ex)
            {
                ExceptionsHandler.LogException("OpenUVCSProjectSettings", ex);
                return null;
            }
        }
    }
}
