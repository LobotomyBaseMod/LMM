using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive
{
    internal static class CloudDriveMenuItem
    {
        internal static void AddMenuItem()
        {
            HandleMenuItem.AddMenuItem(
                MENU_ITEM_NAME,
                MENU_ITEM_PRIORITY,
                ShowCloudDriveWindow,
                validate: null);
        }

        internal static void RemoveMenuItem()
        {
            HandleMenuItem.RemoveMenuItem(MENU_ITEM_NAME);
        }

        static void ShowCloudDriveWindow()
        {
            ShowWindow.CloudDrive();
        }

        const string MENU_ITEM_NAME = "Window/" + UnityConstants.CloudDrive.WINDOW_TITLE;

        const int MENU_ITEM_PRIORITY =
        // Display the menu item in alphabetical order,
#if UNITY_6000_1_OR_NEWER
        // before Window/UI Toolkit
        3019;
#else
        // after Window/Search and before Window/Asset Store
        1300;
#endif
    }
}
