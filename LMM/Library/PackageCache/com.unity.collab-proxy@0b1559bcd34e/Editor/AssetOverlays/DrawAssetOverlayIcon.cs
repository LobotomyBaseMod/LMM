using UnityEngine;

using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.AssetsOverlays
{
    internal static class DrawAssetOverlayIcon
    {
        internal static void ForStatus(
            Rect selectionRect,
            AssetStatus status,
            string tooltipText)
        {
            Texture overlayIcon = GetOverlayIcon(status);

            if (overlayIcon == null)
                return;

            Rect overlayRect = GetOverlayRect.ForSelectionRect(selectionRect);

            GUI.DrawTexture(overlayRect, overlayIcon, ScaleMode.ScaleToFit);

            Rect tooltipRect = GetTooltipRect(selectionRect, overlayRect);

            GUI.Label(tooltipRect, new GUIContent(string.Empty, tooltipText));
        }

        internal static Texture GetOverlayIcon(AssetStatus assetStatus)
        {
            if (ClassifyAssetStatus.IsPrivate(assetStatus))
                return Images.GetPrivateOverlayIcon();

            if (ClassifyAssetStatus.IsIgnored(assetStatus))
                return Images.GetIgnoredOverlayIcon();

            if (ClassifyAssetStatus.IsAdded(assetStatus))
                return Images.GetAddedOverlayIcon();

            if (ClassifyAssetStatus.IsConflicted(assetStatus))
                return Images.GetConflictedOverlayIcon();

            if (ClassifyAssetStatus.IsDeletedOnServer(assetStatus))
                return Images.GetDeletedRemoteOverlayIcon();

            if (ClassifyAssetStatus.IsLockedRemote(assetStatus))
                return Images.GetLockedRemoteOverlayIcon();

            if (ClassifyAssetStatus.IsOutOfDate(assetStatus))
                return Images.GetOutOfSyncOverlayIcon();

            if (ClassifyAssetStatus.IsLocked(assetStatus))
                return Images.GetLockedLocalOverlayIcon();

            if (ClassifyAssetStatus.IsRetained(assetStatus))
                return Images.GetRetainedOverlayIcon();

            if (ClassifyAssetStatus.IsCheckedOut(assetStatus) ||
                ClassifyAssetStatus.IsChanged(assetStatus) ||
                ClassifyAssetStatus.ContainsChanges(assetStatus))
                return Images.GetCheckedOutOverlayIcon();

            return null;
        }

        static Rect Inflate(Rect rect, float width, float height)
        {
            return new Rect(
                rect.x - width,
                rect.y - height,
                rect.width + 2f * width,
                rect.height + 2f * height);
        }

        static Rect GetTooltipRect(
            Rect selectionRect,
            Rect overlayRect)
        {
            if (selectionRect.width > selectionRect.height)
            {
                return overlayRect;
            }

            return Inflate(overlayRect, 3f, 3f);
        }
    }
}
