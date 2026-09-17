using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal static class DrawItemsGridView
    {
        internal const float GRID_AREA_MARGIN = 10;

        internal static void Draw(
            Rect rect,
            EmptyStatePanel emptyStatePanel,
            IItemsGridView itemsGridView,
            List<ExpandedTreeNode> items,
            List<int> selectedItemsIndexes,
            Vector2 itemSize,
            float scrollPosition,
            int rowsCount,
            int columnsCount,
            bool shouldScrollbarBeVisible,
            bool hasFocus,
            int dragTargetIndex,
            ref bool shouldShowEmptyState)
        {
            DrawBackground(rect);

            if (Event.current.type == EventType.Layout)
                shouldShowEmptyState = !emptyStatePanel.IsEmpty();

            if (shouldShowEmptyState)
            {
                emptyStatePanel.OnGUI(rect);
                return;
            }

            DrawGrid(
                rect,
                items,
                selectedItemsIndexes,
                itemSize,
                scrollPosition,
                columnsCount,
                hasFocus,
                dragTargetIndex);

            if (!shouldScrollbarBeVisible)
                return;

            itemsGridView.UpdateScrollPosition(
                GUI.VerticalScrollbar(
                    new Rect(
                        rect.xMax - UnityConstants.SCROLLBAR_WIDTH,
                        rect.y,
                        UnityConstants.SCROLLBAR_WIDTH,
                        rect.height),
                    scrollPosition,
                    rect.height,
                    0,
                    rowsCount * itemSize.y + GRID_AREA_MARGIN * 2));
        }

        static void DrawBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, UnityStyles.Colors.BackgroundLighter);
        }

        static void DrawGrid(
            Rect rect,
            List<ExpandedTreeNode> items,
            IList<int> selectedItemsIndexes,
            Vector2 itemSize,
            float scrollPosition,
            int columnsCount,
            bool hasFocus,
            int dragTargetIndex)
        {
            scrollPosition -= GRID_AREA_MARGIN;

            int firstVisibleRow = Math.Max(0, Mathf.FloorToInt(scrollPosition / itemSize.y));
            int lastVisibleRow = Mathf.FloorToInt((scrollPosition + rect.height) / itemSize.y);

            float offset = scrollPosition % itemSize.y;

            for (int rowIndex = firstVisibleRow; rowIndex <= lastVisibleRow; rowIndex++)
            {
                DrawRow(
                    new Rect(
                        rect.x,
                        rect.y - offset + (rowIndex - firstVisibleRow) * itemSize.y,
                        rect.width,
                        itemSize.y),
                    items,
                    selectedItemsIndexes,
                    itemSize,
                    rowIndex,
                    columnsCount,
                    hasFocus,
                    dragTargetIndex);
            }
        }

        static void DrawRow(
            Rect rect,
            List<ExpandedTreeNode> items,
            IList<int> selectedItemsIndexes,
            Vector2 itemSize,
            int rowIndex,
            int columnsCount,
            bool hasFocus,
            int dragTargetIndex)
        {
            for (int columnIndex = 0; columnIndex < columnsCount; columnIndex++)
            {
                int nodeIndex = rowIndex * columnsCount + columnIndex;

                if (nodeIndex >= items.Count)
                    return;

                DrawExpandedTreeNode.Draw(
                    new Rect(
                        rect.x + GRID_AREA_MARGIN + columnIndex * itemSize.x,
                        rect.y,
                        itemSize.x,
                        rect.height),
                    items[nodeIndex],
                    selectedItemsIndexes.Contains(nodeIndex),
                    hasFocus,
                    dragTargetIndex == nodeIndex);
            }
        }
    }
}
