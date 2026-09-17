using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Zoom
{
    internal static class EdgePoint
    {
        internal static Vector2 GetEdgePoint(
            Vector2 point,
            float width,
            float height,
            float zoomLevel)
        {
            float x = point.x;
            float y = point.y;

            // Create a 50 pixel margin on the edges of the target so that if the mouse is inside
            // that band, then that edge stays pinned on screen and doesn't slide off the
            // edge as we zoom in

            if (point.x < EDGE_POINT_PIXELS / zoomLevel)
                x = 0;
            if (point.y < EDGE_POINT_PIXELS / zoomLevel)
                y = 0;
            if (point.x + EDGE_POINT_PIXELS / zoomLevel > width)
                x = width;
            if (point.y + EDGE_POINT_PIXELS / zoomLevel > height)
                y = height;

            return new Vector2(x, y);
        }

        const float EDGE_POINT_PIXELS = 50;
    }
}
