using System.Collections.Generic;
using System.Collections.ObjectModel;
using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using System.Linq;

using UnityEditor;

using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.UI;

using AssetOverlays = Unity.PlasticSCM.Editor.AssetsOverlays;

namespace Unity.PlasticSCM.Editor.AssetUtils.Processor
{
    class UVCSAssetModificationProcessor : AssetModificationProcessor
    {
        internal static bool IsManualCheckoutEnabled { get; private set; }

        static UVCSAssetModificationProcessor()
        {
            IsManualCheckoutEnabled = BoolSetting.Load(
                UnityConstants.IS_MANUAL_CHECKOUT_ENABLED_KEY_NAME, false);
        }

        internal static void Enable(
            string wkPath,
            IAssetStatusCache assetStatusCache)
        {
            mLog.Debug("Enable");

            mWkPath = wkPath;
            mAssetStatusCache = assetStatusCache;

            mIsEnabled = true;
        }

        internal static void Disable()
        {
            mLog.Debug("Disable");

            mIsEnabled = false;

            ModifiedAssets.Clear();

            mWkPath = null;
            mAssetStatusCache = null;
        }

        internal static void ToggleManualCheckoutPreference(RepositorySpec repSpec)
        {
            SetManualCheckoutPreference(!IsManualCheckoutEnabled);

            TrackFeatureUseEvent.For(
                repSpec,
                IsManualCheckoutEnabled ?
                    TrackFeatureUseEvent.Features.UnityPackage.EnableManualCheckout :
                    TrackFeatureUseEvent.Features.UnityPackage.DisableManualCheckout);
        }

        internal static void SetManualCheckoutPreference(bool isEnabled)
        {
            if (IsManualCheckoutEnabled == isEnabled)
                return;

            IsManualCheckoutEnabled = isEnabled;

            mLog.DebugFormat("{0} Manual Checkout", IsManualCheckoutEnabled ? "Enable" : "Disable");

            BoolSetting.Save(
                IsManualCheckoutEnabled, UnityConstants.IS_MANUAL_CHECKOUT_ENABLED_KEY_NAME);
        }

        internal static ReadOnlyCollection<string> GetModifiedAssetsToProcess()
        {
            return ModifiedAssets.ToList().AsReadOnly();
        }

        internal static string[] ExtractModifiedAssetsToProcess()
        {
            string[] result = ModifiedAssets.ToArray();

            ModifiedAssets.Clear();

            return result;
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (!mIsEnabled)
                return paths;

            foreach (string path in paths)
            {
                ModifiedAssets.Add(path);
            }

            return paths;
        }

        // If IsOpenForEdit returns false, the preCheckoutCallback is invoked
        // to perform the checkout operation and make the asset editable.
        static bool IsOpenForEdit(string assetPath, out string message)
        {
            message = string.Empty;

            if (!mIsEnabled)
                return true;

            if (!IsManualCheckoutEnabled)
                return true;

            if (assetPath.StartsWith("ProjectSettings/"))
                return true;

            string assetFullPath = AssetsPath.GetFullPathUnderWorkspace.
                ForAsset(mWkPath, assetPath);

            if (assetFullPath == null)
                return true;

            if (MetaPath.IsMetaPath(assetFullPath))
                assetFullPath = MetaPath.GetPathFromMetaPath(assetFullPath);

            AssetOverlays.AssetStatus status = mAssetStatusCache.
                GetStatus(assetFullPath);

            if (AssetOverlays.ClassifyAssetStatus.IsAdded(status) ||
                AssetOverlays.ClassifyAssetStatus.IsCheckedOut(status))
                return true;

            return !AssetOverlays.ClassifyAssetStatus.IsControlled(status);
        }

        // We need to process the modified assets to perform their check-out.
        // To do this, we must verify their content, date, and size to determine if they
        // have actually changed. This requires the changes to be written to disk first.
        // To ensure this, we store the modified files in this array and process them
        // when they are reloaded in UVCSAssetPostprocessor.OnPostprocessAllAssets.
        static readonly HashSet<string> ModifiedAssets = new HashSet<string>();

        static IAssetStatusCache mAssetStatusCache;
        static string mWkPath;
        static bool mIsEnabled;

        static readonly ILog mLog = PlasticApp.GetLogger("UVCSAssetModificationProcessor");
    }
}
