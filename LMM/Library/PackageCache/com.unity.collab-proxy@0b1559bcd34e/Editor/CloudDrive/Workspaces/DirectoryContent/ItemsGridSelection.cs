using System.Collections.Generic;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal static class ItemsGridSelection
    {
        internal static List<string> GetItemsPathsToSelect(
            bool isItemsDirPathChanging,
            ItemsGridView itemsGridView)
        {
            if (isItemsDirPathChanging)
                return new List<string>();

            return itemsGridView.GetSelectedItemsPaths();
        }

        internal static void SelectItems(
            bool isItemsDirPathChanging,
            List<string> itemsPathsToSelect,
            ItemsGridView itemsGridView)
        {
            if (isItemsDirPathChanging)
            {
                itemsGridView.ClearSelection();
                return;
            }

            itemsGridView.SelectItemsFromPaths(itemsPathsToSelect);
        }
    }
}
