using UnityEditor;

namespace Unity.PlasticSCM.Editor
{
    internal static class VCSBuiltInPlugin
    {
        internal static bool IsEnabled()
        {
            return GetVersionControlMode() == "PlasticSCM";
        }

        internal static void Disable()
        {
            SetVersionControlMode("Visible Meta Files");

            AssetDatabase.SaveAssets();
        }

        internal static bool IsAnyProviderEnabled()
        {
            return !IsVisibleMetaFilesMode() && !IsHiddenMetaFilesMode();
        }

        static string GetVersionControlMode()
        {
            return VersionControlSettings.mode;
        }

        static void SetVersionControlMode(string versionControl)
        {
            VersionControlSettings.mode = versionControl;
        }

        static bool IsVisibleMetaFilesMode()
        {
            return GetVersionControlMode() == "Visible Meta Files";
        }

        static bool IsHiddenMetaFilesMode()
        {
            return GetVersionControlMode() == "Hidden Meta Files";
        }
    }
}
