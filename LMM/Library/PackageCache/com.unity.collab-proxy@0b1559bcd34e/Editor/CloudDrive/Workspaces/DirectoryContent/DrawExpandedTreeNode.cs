using System;
using System.IO;

using UnityEngine;

using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal static class DrawExpandedTreeNode
    {
        internal const float ICON_SIZE = 66;
        internal const float PADDING = 5;

        internal static void Draw(
            Rect rect,
            ExpandedTreeNode node,
            bool isSelected,
            bool hasFocus,
            bool isDragTarget)
        {
            DrawIcon(
                new Rect(
                    rect.x + (rect.width - ICON_SIZE) / 2,
                    rect.y + PADDING,
                    ICON_SIZE,
                    ICON_SIZE),
                GetIcon(node),
                isSelected);

            string fileName = Path.GetFileName(node.GetFullPath());

            DrawLabel(
                new Rect(
                    rect.x + PADDING,
                    rect.y + PADDING + ICON_SIZE,
                    rect.width - 2 * PADDING,
                    rect.height - ICON_SIZE - 2 * PADDING),
                ExpandedTreeNode.IsDirectory(node) ? fileName : GetFileNameWithoutExtension(fileName),
                fileName,
                isSelected,
                hasFocus,
                isDragTarget);
        }

        internal static Texture GetIcon(ExpandedTreeNode node)
        {
            if (ExpandedTreeNode.IsDirectory(node))
                return Images.GetFolderIcon();

            return Images.GetFileIcon(node.GetFullPath());
        }

        static void DrawIcon(Rect rect, Texture icon, bool isSelected)
        {
            Color originalColor = GUI.color;

            try
            {
                if (isSelected)
                    GUI.color *= UnityStyles.Colors.SelectedIcon;

                GUI.DrawTexture(rect, icon);
            }
            finally
            {
                GUI.color = originalColor;
            }
        }

        static void DrawLabel(
            Rect rect,
            string text,
            string tooltip,
            bool isSelected,
            bool hasFocus,
            bool isDragTarget)
        {
            GUIStyle normalStyle = UnityStyles.CloudDrive.ItemsListLabel;
            GUIStyle focusedStyle = UnityStyles.CloudDrive.ItemsListLabelFocused;

            float textWidth;
            string cutText = CutText(
                text, normalStyle, rect.width,
                out textWidth);

            Rect labelRect = new Rect(
                rect.x + (rect.width - textWidth) / 2,
                rect.y + (rect.height - normalStyle.lineHeight) / 2,
                textWidth,
                normalStyle.lineHeight);

            if (isSelected)
            {
                Rect selectionRect = new Rect(
                    labelRect.x - 1, labelRect.y - 1,
                    labelRect.width + 2, labelRect.height + 2);

                Color rectColor = hasFocus ?
                    UnityStyles.Colors.SelectedFocusedTextBackground :
                    UnityStyles.Colors.SelectedUnfocusedTextBackground;

                Texture2D selectionTexture = CreateRoundedRectTexture(
                    selectionRect.size,
                    radius: 1,
                    borderColor: rectColor,
                    fillColor: rectColor,
                    thickness: 1);

                GUI.DrawTexture(selectionRect, selectionTexture);
            }

            if (isDragTarget)
            {
                Rect dragTargetRect = new Rect(
                    rect.x, rect.y - 2,
                    rect.width, rect.height + 4);

                Texture2D dragTexture = CreateRoundedRectTexture(
                    dragTargetRect.size,
                    radius: 6,
                    borderColor: UnityStyles.Colors.SelectedFocusedDropLineBackground,
                    fillColor: UnityStyles.Colors.SelectedFocusedDropLabelBackground,
                    thickness: 2);

                GUI.DrawTexture(dragTargetRect, dragTexture);
            }

            GUI.Label(
                labelRect,
                new GUIContent(cutText, cutText == tooltip ? null : tooltip),
                isSelected && hasFocus ? focusedStyle : normalStyle);
        }

        static string CutText(
            string text,
            GUIStyle style,
            float availableWidth,
            out float usedWidth)
        {
            usedWidth = 0;

            availableWidth -= style.margin.horizontal + style.padding.horizontal;

            float fullTextWidth = style.CalcSize(
                new GUIContent(text)).x;

            if (fullTextWidth <= availableWidth)
            {
                usedWidth = fullTextWidth;
                return text;
            }

            string resultText = string.Empty;

            foreach (char c in text)
            {
                float currentWidth = style.CalcSize(
                    new GUIContent(resultText + c + ELLIPSIS)).x;

                if (currentWidth > availableWidth)
                    return resultText + ELLIPSIS;

                usedWidth = currentWidth;
                resultText += c;
            }

            return resultText + ELLIPSIS;
        }

        static string GetFileNameWithoutExtension(string fileName)
        {
            if (fileName.LastIndexOf(".") <= 0)
                return fileName;

            if (MetaPath.IsMetaPath(fileName))
                fileName = MetaPath.GetPathFromMetaPath(fileName);

            if (fileName.LastIndexOf(".") <= 0)
                return fileName;

            return Path.GetFileNameWithoutExtension(fileName);
        }

        static Texture2D CreateRoundedRectTexture(
            Vector2 rectSize,
            float radius,
            Color borderColor,
            Color fillColor,
            float thickness)
        {
            int width = (int)rectSize.x;
            int height = (int)rectSize.y;

            radius = Math.Min(radius, Math.Min(width, height) / 2);
            thickness = Math.Min(radius, thickness);

            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, false);

            Color32[] pixels = new Color32[width * height];

            double outerRadiusSquared = Math.Pow(radius, 2);
            double innerRadiusSquared = Math.Pow(radius - thickness, 2);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIdx = y * width + x;

                    double distanceSquared;
                    if (InCorner(x, y, width, height, radius, out distanceSquared))
                    {
                        if (distanceSquared > outerRadiusSquared)
                            continue;

                        pixels[pixelIdx] =
                            distanceSquared >= innerRadiusSquared ?
                                borderColor : fillColor;
                        continue;
                    }

                    pixels[pixelIdx] =
                        IsBorder(x, y, width, height, radius, thickness) ?
                        borderColor : fillColor;
                }
            }

            result.SetPixels32(pixels);
            result.Apply();
            return result;
        }

        static bool InCorner(
            int x,
            int y,
            float width,
            float height,
            float radius,
            out double distanceSquared)
        {
            bool inTop = y < radius;
            bool inBottom = y >= height - radius;
            bool inLeft = x < radius;
            bool inRight = x >= width - radius;

            if ((!inTop && !inBottom) || (!inLeft && !inRight))
            {
                distanceSquared = 0;
                return false;
            }

            float centerX = inLeft ? radius : width - radius - 1;
            float centerY = inTop ? radius : height - radius - 1;

            float deltaX = x - centerX;
            float deltaY = y - centerY;

            distanceSquared = Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2);
            return true;
        }

        static bool IsBorder(int x, int y, float width, float height, float radius, float thickness)
        {
            if (y < thickness && x >= radius && x < width - radius) // top border
                return true;

            if (y >= height - thickness && x >= radius && x < width - radius) // bottom border
                return true;

            if (x < thickness && y >= radius && y < height - radius) // left border
                return true;

            if (x >= width - thickness && y >= radius && y < height - radius) // right border
                return true;

            return false;
        }

        const string ELLIPSIS = "...";
    }
}
