using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.AssetMenu;
using Unity.PlasticSCM.Editor.AssetsOverlays;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;

namespace Unity.PlasticSCM.Editor
{
    internal static class HierarchyExtensions
    {
        internal static void Enable(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            IAssetStatusCache assetStatusCache)
        {
            DrawHierarchyOverlay.Enable(wkInfo.ClientPath, assetStatusCache);
            HierarchyViewAssetMenu.Enable(wkInfo, plasticApi, assetStatusCache);
        }

        internal static void Disable()
        {
            DrawHierarchyOverlay.Disable();
            HierarchyViewAssetMenu.Disable();
        }
    }
}
