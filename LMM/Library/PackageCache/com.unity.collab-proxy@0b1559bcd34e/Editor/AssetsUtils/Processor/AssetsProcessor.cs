using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;

namespace Unity.PlasticSCM.Editor.AssetUtils.Processor
{
    internal static class AssetsProcessors
    {
        internal static void Enable(
            string wkPath,
            UVCSAssetsProcessor uvcsAssetsProcessor,
            IAssetStatusCache assetStatusCache)
        {
            UVCSAssetPostprocessor.Enable(wkPath, uvcsAssetsProcessor);
            UVCSAssetModificationProcessor.Enable(wkPath, assetStatusCache);
        }

        internal static void Disable()
        {
            UVCSAssetPostprocessor.Disable();
            UVCSAssetModificationProcessor.Disable();
        }
    }
}
