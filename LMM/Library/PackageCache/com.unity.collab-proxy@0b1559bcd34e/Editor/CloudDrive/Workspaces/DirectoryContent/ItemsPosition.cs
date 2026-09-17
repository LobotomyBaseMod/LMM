using UnityEngine;

using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal static class ItemsPosition
    {
        internal static bool IsMousePositionInsideGrid(
            Vector2 mousePosition,
            Rect rect,
            bool isScrollbarVisible)
        {
            if (mousePosition.y < 0 || mousePosition.y > rect.height)
                return false;

            if (isScrollbarVisible)
                rect.width -= UnityConstants.SCROLLBAR_WIDTH;

            if (mousePosition.x < 0 || mousePosition.x > rect.width)
                return false;

            return true;
        }

        internal static bool IsMousePositionNearTopEdge(
            Vector2 mousePosition)
        {
            return mousePosition.y < 10;
        }

        internal static bool IsMousePositionNearBottomEdge(
            Vector2 mousePosition,
            Rect rect)
        {
            return mousePosition.y > rect.height - 10;
        }

        internal static int GetItemIndexFromMousePosition(
            Vector2 mousePosition,
            Vector2 itemSize,
            float scrollPosition,
            int columnsCount,
            int itemsCount)
        {
            Vector2 itemsGridContentMousePosition =
                new Vector2(
                    mousePosition.x - DrawItemsGridView.GRID_AREA_MARGIN,
                    mousePosition.y - DrawItemsGridView.GRID_AREA_MARGIN + scrollPosition);

            if (itemsGridContentMousePosition.x < 0 || itemsGridContentMousePosition.y < 0)
                return -1;

            if (itemsGridContentMousePosition.x > itemSize.x * columnsCount)
                return -1;

            int col = (int)(itemsGridContentMousePosition.x / itemSize.x);
            int row = (int)(itemsGridContentMousePosition.y / itemSize.y);

            if (row * columnsCount + col >= itemsCount)
                return -1;

            Vector2 itemMousePosition =
                new Vector2(
                    itemsGridContentMousePosition.x - (col * itemSize.x),
                    itemsGridContentMousePosition.y - (row * itemSize.y));

            if (itemMousePosition.x < DrawExpandedTreeNode.PADDING ||
                itemMousePosition.x > itemSize.x - DrawExpandedTreeNode.PADDING ||
                itemMousePosition.y < DrawExpandedTreeNode.PADDING ||
                itemMousePosition.y > itemSize.y - DrawExpandedTreeNode.PADDING)
            {
                return -1;
            }

            return row * columnsCount + col;
        }

        internal static float GetScrollPosition(
            int rowsCount,
            int columnsCount,
            int itemIndex,
            float itemHeight,
            float availableHeight,
            float scrollPosition)
        {
            int row = itemIndex / columnsCount;

            if (row == 0)
                return 0;

            if (row == rowsCount - 1)
                return itemHeight * rowsCount + 2 * DrawItemsGridView.GRID_AREA_MARGIN - availableHeight;

            float top = itemHeight * row + DrawItemsGridView.GRID_AREA_MARGIN;
            float bottom = top + itemHeight;

            float targetPosition = scrollPosition;

            if (bottom > availableHeight + scrollPosition)
                targetPosition = bottom - availableHeight;

            if (top < scrollPosition)
                targetPosition = top;

            return Mathf.Max(targetPosition, 0f);
        }
    }
}
