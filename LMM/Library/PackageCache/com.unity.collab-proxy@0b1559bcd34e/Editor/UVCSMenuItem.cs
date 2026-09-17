using UnityEditor;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal static class UVCSMenuItem
    {
#if UNITY_6000_1_OR_NEWER
        [MenuItem(MENU_ITEM_NAME, false, 0)]
#else
        // Display the menu item in alphabetical order,
        // after Window/Search and before Window/Asset Store
        [MenuItem(MENU_ITEM_NAME, false, 1301)]
#endif
        static void ShowUVCSWindow()
        {
            SwitchUVCSPlugin.OnIfNeeded(UVCSPlugin.Instance);
        }

        [MenuItem(MENU_ITEM_NAME, true)]
        static bool ValidateMenu()
        {
            return !VCSBuiltInPlugin.IsAnyProviderEnabled();
        }

        const string MENU_ITEM_NAME =
#if UNITY_6000_1_OR_NEWER
            // The Window menu was refactored in Unity 6000.1.0a4 to host both UVCS & External providers (Perforce)
            "Window/Version Control/" + UnityConstants.UVCS_WINDOW_TITLE;
#else
            "Window/" + UnityConstants.UVCS_WINDOW_TITLE;
#endif
    }
}
