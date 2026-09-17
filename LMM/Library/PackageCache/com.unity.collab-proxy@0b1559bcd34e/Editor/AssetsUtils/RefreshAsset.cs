using UnityEditor;
using UnityEditor.PackageManager;

using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.AssetUtils
{
    internal static class RefreshAsset
    {
        internal static void BeforeLongAssetOperation()
        {
            AssetDatabase.DisallowAutoRefresh();
        }

        internal static void AfterLongAssetOperation(IAssetStatusCache assetStatusCache)
        {
            AfterLongAssetOperation(assetStatusCache, true);
        }

        internal static void AfterLongAssetOperation(
            IAssetStatusCache assetStatusCache,
            bool isPackagesReimportNeeded)
        {
            AssetDatabase.AllowAutoRefresh();

            if (isPackagesReimportNeeded)
            {
                UnityAssetDatabaseAndPackageManagerAsync(assetStatusCache);
                return;
            }

            RefreshUnityAssetDatabase(assetStatusCache);
            }

        internal static void UnityAssetDatabase(IAssetStatusCache assetStatusCache)
        {
            RefreshUnityAssetDatabase(assetStatusCache);
        }

        internal static void UnityAssetDatabaseAndPackageManagerAsync(
            IAssetStatusCache assetStatusCache)
        {
            mLog.Debug("Triggering async Package Manager Resolve for domain reload");

            // Client.Resolve() will resolve any pending packages added or removed from the project
            // VCS-1004718 - This is important so the domain gets reloaded first if needed
            Client.Resolve();

            new DelayedActionBySecondsRunner(
                    () => RefreshUnityAssetDatabase(assetStatusCache),
                    UnityConstants.REFRESH_ASSET_DATABASE_DELAYED_INTERVAL)
                .Run();
        }

        internal static void VersionControlCache(IAssetStatusCache assetStatusCache)
        {
            ClearVersionControlCaches(assetStatusCache);

            RepaintEditor.AllWindowsWithOverlays();
        }

        static void ClearVersionControlCaches(IAssetStatusCache assetStatusCache)
        {
            UnityEditor.VersionControl.Provider.ClearCache();

            if (assetStatusCache != null)
                assetStatusCache.Clear();
        }

        static void RefreshUnityAssetDatabase(IAssetStatusCache assetStatusCache)
        {
            mLog.Debug("Refreshing Unity AssetDatabase");

            AssetDatabase.Refresh(ImportAssetOptions.Default);

            ClearVersionControlCaches(assetStatusCache);

            UVCSAssetPostprocessor.SetIsRepaintNeededAfterAssetDatabaseRefresh();
        }

        static readonly ILog mLog = PlasticApp.GetLogger("RefreshAsset");
    }
}
