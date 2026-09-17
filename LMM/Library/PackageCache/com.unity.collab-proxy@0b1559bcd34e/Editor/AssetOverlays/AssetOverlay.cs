using System;

using PlasticGui;

namespace Unity.PlasticSCM.Editor.AssetsOverlays
{
    internal static class AssetOverlay
    {
        internal static string GetStatusString(AssetStatus assetStatus)
        {
            if (ClassifyAssetStatus.IsPrivate(assetStatus))
                return PlasticLocalization.Name.Private.GetString();

            if (ClassifyAssetStatus.IsIgnored(assetStatus))
                return PlasticLocalization.Name.StatusIgnored.GetString();

            if (ClassifyAssetStatus.IsAdded(assetStatus))
                return PlasticLocalization.Name.StatusAdded.GetString();

            if (ClassifyAssetStatus.IsConflicted(assetStatus))
                return PlasticLocalization.Name.StatusConflicted.GetString();

            if (ClassifyAssetStatus.IsDeletedOnServer(assetStatus))
                return PlasticLocalization.Name.StatusDeletedOnServer.GetString();

            if (ClassifyAssetStatus.IsLockedRemote(assetStatus))
                return PlasticLocalization.Name.StatusLockedRemote.GetString();

            if (ClassifyAssetStatus.IsOutOfDate(assetStatus))
                return PlasticLocalization.Name.StatusOutOfDate.GetString();

            if (ClassifyAssetStatus.IsLocked(assetStatus))
                return PlasticLocalization.Name.StatusLockedByMe.GetString();

            if (ClassifyAssetStatus.IsRetained(assetStatus))
                return PlasticLocalization.Name.StatusRetained.GetString();

            if (ClassifyAssetStatus.IsCheckedOut(assetStatus))
                return PlasticLocalization.Name.StatusCheckout.GetString();

            if (ClassifyAssetStatus.IsChanged(assetStatus))
                return PlasticLocalization.Name.ChangedStatus.GetString();

            if (ClassifyAssetStatus.ContainsChanges(assetStatus))
                return PlasticLocalization.Name.ContainsChanges.GetString();

            if (ClassifyAssetStatus.IsControlled(assetStatus))
                return PlasticLocalization.Name.Controlled.GetString();

            return string.Empty;
        }

        internal static string GetTooltipText(
            AssetStatus statusValue,
            LockStatusData lockStatusData)
        {
            string statusText = GetStatusString(statusValue);

            if (lockStatusData == null)
                return statusText;

            // example:
            // Changed by:
            // * dani_pen@hotmail.com
            // * workspace wkLocal"

            char bulletCharacter = '\u25cf';

            string line1 = ClassifyAssetStatus.IsLocked(statusValue) ?
                statusText + ":" :
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.AssetOverlayTooltipStatus,
                    statusText);

            string line2 = string.Format("{0} {1}",
                bulletCharacter,
                lockStatusData.LockedBy);

            string line3 = string.Format("{0} {1}",
                bulletCharacter,
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.AssetOverlayTooltipOn,
                    lockStatusData.HolderBranchName));

            return string.Format(
                "{0}" + Environment.NewLine +
                "{1}" + Environment.NewLine +
                "{2}",
                line1,
                line2,
                line3);
        }
    }
}
