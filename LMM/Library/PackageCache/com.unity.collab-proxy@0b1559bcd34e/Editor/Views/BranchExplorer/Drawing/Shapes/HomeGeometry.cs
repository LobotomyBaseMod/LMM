using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal static class HomeGeometry
    {
        internal const float WIDTH = 16f;
        internal const float HEIGHT = 16f;

        internal static void Draw(Painter2D painter, float offsetX, float offsetY, float scale = 1)
        {
            painter.BeginPath();

            // Start at right eave
            painter.MoveTo(Scale(14f, 9f, offsetX, offsetY, scale));

            // Right wall top
            painter.LineTo(Scale(13f, 9f, offsetX, offsetY, scale));

            // Right wall down
            painter.LineTo(Scale(13f, 13f, offsetX, offsetY, scale));

            // Bottom right corner curve
            painter.BezierCurveTo(
                Scale(13f, 13.5523f, offsetX, offsetY, scale),
                Scale(12.5523f, 14f, offsetX, offsetY, scale),
                Scale(12f, 14f, offsetX, offsetY, scale));

            // Bottom to door right
            painter.LineTo(Scale(9f, 14f, offsetX, offsetY, scale));

            // Door right side up
            painter.LineTo(Scale(9f, 10f, offsetX, offsetY, scale));

            // Door top
            painter.LineTo(Scale(7f, 10f, offsetX, offsetY, scale));

            // Door left side down
            painter.LineTo(Scale(7f, 14f, offsetX, offsetY, scale));

            // Bottom to left wall
            painter.LineTo(Scale(4f, 14f, offsetX, offsetY, scale));

            // Bottom left corner curve
            painter.BezierCurveTo(
                Scale(3.44772f, 14f, offsetX, offsetY, scale),
                Scale(3f, 13.5523f, offsetX, offsetY, scale),
                Scale(3f, 13f, offsetX, offsetY, scale));

            // Left wall up
            painter.LineTo(Scale(3f, 9f, offsetX, offsetY, scale));

            // Left eave
            painter.LineTo(Scale(2f, 9f, offsetX, offsetY, scale));

            // Left side of roof to top
            painter.LineTo(Scale(8.04688f, 2f, offsetX, offsetY, scale));

            // Right side of roof back to start
            painter.LineTo(Scale(14f, 9f, offsetX, offsetY, scale));

            painter.ClosePath();
        }

        static Vector2 Scale(float x, float y, float offsetX, float offsetY, float scale)
        {
            return new Vector2(offsetX + x * scale, offsetY + y * scale);
        }
    }
}
