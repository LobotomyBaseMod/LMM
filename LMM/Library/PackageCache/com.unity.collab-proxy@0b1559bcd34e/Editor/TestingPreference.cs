using UnityEditor;

namespace Unity.PlasticSCM.Editor
{
    internal static class TestingPreference
    {
        internal static bool IsShowCloudDriveWelcomeViewEnabled()
        {
            return SessionState.GetBool(SHOW_CLOUD_DRIVE_WELCOME_VIEW_KEY_NAME, false);
        }

        internal static bool IsShowUVCSWelcomeViewEnabled()
        {
            return SessionState.GetBool(SHOW_UVCS_WELCOME_VIEW_KEY_NAME, false);
        }

        internal static void SetShowCloudDriveWelcomeView(bool isEnabled)
        {
            SessionState.SetBool(SHOW_CLOUD_DRIVE_WELCOME_VIEW_KEY_NAME, isEnabled);
        }

        internal static void SetShowUVCSWelcomeView(bool isEnabled)
        {
            SessionState.SetBool(SHOW_UVCS_WELCOME_VIEW_KEY_NAME, isEnabled);
        }

        const string SHOW_UVCS_WELCOME_VIEW_KEY_NAME = "ShowUVCSWelcomeView";
        const string SHOW_CLOUD_DRIVE_WELCOME_VIEW_KEY_NAME = "ShowCloudDriveWelcomeView";
    }
}
