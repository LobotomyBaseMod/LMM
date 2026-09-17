using System.Linq;

using UnityEditor;

using PlasticGui;
using Unity.PlasticSCM.Editor.AssetMenu.Dialogs;
using Unity.PlasticSCM.Editor.CloudDrive;
using Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.AssetMenu
{
    internal static class ProjectViewCloudDriveAssetMenu
    {
        internal static void AddMenuItem()
        {
            HandleMenuItem.AddMenuItem(
                PlasticLocalization.Name.AddToUnityCloudDriveMenu.GetString(),
                ProjectViewUVCSAssetMenu.BASE_MENU_ITEM_PRIORITY,
                AddToCloudDrive,
                ValidateAddToCloudDrive);

            HandleMenuItem.UpdateAllMenus();
        }

        internal static void RemoveMenuItem()
        {
            HandleMenuItem.RemoveMenuItem(
                PlasticLocalization.Name.AddToUnityCloudDriveMenu.GetString());
        }

        static void AddToCloudDrive()
        {
            if (PlasticGuiConfig.Get().Configuration.ShowCloudDriveWelcomeView ||
                UnityConfigurationChecker.NeedsConfiguration())
            {
                ShowWindow.CloudDrive();
                return;
            }

            if (!CloudDriveWindow.HasCloudDriveWorkspaces())
            {
                CreateWorkspaceDialog.CreateWorkspace(
                    PlasticGui.Plastic.WebRestAPI,
                    PlasticGui.Plastic.API,
                    EditorWindow.focusedWindow,
                    (createdWorkspace) => { ShowAddToCloudDriveDialog(); });
                return;
            }

            ShowAddToCloudDriveDialog();
        }

        static void ShowAddToCloudDriveDialog()
        {
            string[] selectedPaths = GetSelectedAssetPaths();

            if (selectedPaths.Length == 0)
                return;

            AddToCloudDriveDialog.ShowDialog(
                selectedPaths,
                PlasticGui.Plastic.WebRestAPI,
                PlasticGui.Plastic.API,
                EditorWindow.focusedWindow);
        }

        static bool ValidateAddToCloudDrive()
        {
            return Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;
        }

        static string[] GetSelectedAssetPaths()
        {
            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
                return new string[0];

            return Selection.assetGUIDs
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => !string.IsNullOrEmpty(path))
                .ToArray();
        }
    }
}
