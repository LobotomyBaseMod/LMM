using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class GetOverlayRect
    {
        internal static Rect ForPendingChanges(Rect iconRect)
        {
            return new Rect(
                iconRect.x + 6f,
                iconRect.y + 8f,
                UnityConstants.OVERLAY_STATUS_ICON_SIZE,
                UnityConstants.OVERLAY_STATUS_ICON_SIZE);
        }

        internal static Rect ForSelectionRect(Rect selectionRect)
        {
            // smallest size (16px height), e.g.
            // - treeView in project view
            // - tree view in hierarchy view
            if (Mathf.Approximately(selectionRect.height, 16f))
                return GetForSmallItems(selectionRect);

            // larger items, e.g. grid view in project view
            return GetForOtherSizes(selectionRect);
        }

        static Rect GetForSmallItems(
                    Rect selectionRect)
        {
            Rect result =  new Rect(
                selectionRect.x + 4f,
                selectionRect.y + 4f,
                UnityConstants.OVERLAY_STATUS_ICON_SIZE,
                UnityConstants.OVERLAY_STATUS_ICON_SIZE);

            if (Mathf.Approximately(selectionRect.x, 14))
            {
                // In the Project window grid view at min size,
                // the items have an extra 3px margin to the left.
                // We can detect that case because the x position is always 14 there.
                // Compensate for that margin
                result.x += 3;
            }

            return result;
        }

        static Rect GetForOtherSizes(
            Rect selectionRect)
        {
            int sizeToCalculateRatio = 32;
            float iconOffset = 20f;

            float widthRatio = selectionRect.width /
                               sizeToCalculateRatio;
            float heightRatio = selectionRect.height /
                                sizeToCalculateRatio;

            return new Rect(
               selectionRect.x + (iconOffset * widthRatio) - 1f,
               selectionRect.y + (iconOffset * heightRatio) - 13f,
               UnityConstants.OVERLAY_STATUS_ICON_SIZE * widthRatio,
               UnityConstants.OVERLAY_STATUS_ICON_SIZE * heightRatio);
        }
    }
}
