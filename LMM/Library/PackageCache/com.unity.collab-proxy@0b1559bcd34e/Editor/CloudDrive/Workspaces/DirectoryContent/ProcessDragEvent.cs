using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal static class ProcessDragEvent
    {
        internal static void ProcessMouseDrag(
            IDragAndDrop dragAndDrop,
            EditorWindow parentWindow)
        {
            List<string> selectedPaths = dragAndDrop.GetDragSourcePaths();

            if (selectedPaths.Count == 0)
                return;

            UnityEditor.DragAndDrop.PrepareStartDrag();
            UnityEditor.DragAndDrop.paths = selectedPaths.ToArray();
            UnityEditor.DragAndDrop.objectReferences = new Object[] { parentWindow };
            UnityEditor.DragAndDrop.StartDrag(string.Empty);
        }

        internal static void ProcessDragExited(
            IItemsGridView itemsGridView,
            IDragAndDrop dragAndDrop)
        {
            itemsGridView.ClearDragTargetIndex();
            dragAndDrop.ClearDragTargetPath();
        }

        internal static void ProcessDragUpdated(
            IItemsGridView itemsGridView,
            IDragAndDrop dragAndDrop,
            EditorWindow parentWindow,
            Vector2 mousePosition,
            Rect rect,
            Vector2 itemSize,
            float scrollPosition,
            int columnsCount,
            int itemsCount,
            bool isScrollbarVisible)
        {
            string dragTargetPath;
            if (!IsValidDragTarget(
                    itemsGridView,
                    parentWindow,
                    mousePosition,
                    rect,
                    itemSize,
                    scrollPosition,
                    columnsCount,
                    itemsCount,
                    isScrollbarVisible,
                    out dragTargetPath))
            {
                itemsGridView.ClearDragTargetIndex();
                dragAndDrop.ClearDragTargetPath();
                return;
            }

            UnityEditor.DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            itemsGridView.SetDragTargetIndexFromPath(dragTargetPath);
        }

        internal static void ProcessDragPerform(
            IItemsGridView itemsGridView,
            IDragAndDrop dragAndDrop,
            EditorWindow parentWindow,
            Vector2 mousePosition,
            Rect rect,
            Vector2 itemSize,
            float scrollPosition,
            int columnsCount,
            int itemsCount,
            bool isScrollbarVisible)
        {
            string dragTargetPath;
            if (!IsValidDragTarget(
                    itemsGridView,
                    parentWindow,
                    mousePosition,
                    rect,
                    itemSize,
                    scrollPosition,
                    columnsCount,
                    itemsCount,
                    isScrollbarVisible,
                    out dragTargetPath))
            {
                itemsGridView.ClearDragTargetIndex();
                dragAndDrop.ClearDragTargetPath();
                return;
            }

            itemsGridView.SetDragTargetIndexFromPath(dragTargetPath);
            dragAndDrop.SetDragTargetPath(dragTargetPath);
            dragAndDrop.ExecuteDropAction(UnityEditor.DragAndDrop.paths);
        }

        static bool IsValidDragTarget(
            IItemsGridView itemsGridView,
            EditorWindow parentWindow,
            Vector2 mousePosition,
            Rect rect,
            Vector2 itemSize,
            float scrollPosition,
            int columnsCount,
            int itemsCount,
            bool isScrollbarVisible,
            out string dragTargetPath)
        {
            dragTargetPath = string.Empty;

            mousePosition -= rect.position;

            if (!ItemsPosition.IsMousePositionInsideGrid(mousePosition, rect, isScrollbarVisible))
                return false;

            UpdateScrollPositionIfMouseNearEdge(itemsGridView, scrollPosition, mousePosition, rect);

            if (UnityEditor.DragAndDrop.objectReferences.Length != 0 &&
                UnityEditor.DragAndDrop.objectReferences[0] == parentWindow)
            {
                return false;
            }

            dragTargetPath = itemsGridView.GetDragTargetPathFromIndex(
                ItemsPosition.GetItemIndexFromMousePosition(
                    mousePosition, itemSize, scrollPosition, columnsCount, itemsCount));

            if (IsAnyPathContainedOnDestination(UnityEditor.DragAndDrop.paths, dragTargetPath))
                return false;

            return true;
        }

        static bool IsAnyPathContainedOnDestination(
            string[] srcPaths,
            string dstPath)
        {
            foreach (string srcPath in srcPaths)
            {
                if (PathHelper.IsContainedOn(srcPath, dstPath))
                    return true;
            }

            return false;
        }

        static void UpdateScrollPositionIfMouseNearEdge(
            IItemsGridView itemsGridView,
            float scrollPosition,
            Vector2 mousePosition,
            Rect rect)
        {
            if (ItemsPosition.IsMousePositionNearTopEdge(mousePosition))
            {
                itemsGridView.UpdateScrollPosition(
                    scrollPosition - ProcessItemsGridViewEvent.VERTICAL_SCROLL_STEP);
                return;
            }

            if (ItemsPosition.IsMousePositionNearBottomEdge(mousePosition, rect))
            {
                itemsGridView.UpdateScrollPosition(
                    scrollPosition + ProcessItemsGridViewEvent.VERTICAL_SCROLL_STEP);
            }
        }
    }
}
