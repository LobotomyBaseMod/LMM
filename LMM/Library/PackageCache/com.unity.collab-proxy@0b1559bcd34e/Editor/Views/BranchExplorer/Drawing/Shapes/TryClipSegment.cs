using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal static class TryClipSegment
    {
        internal static bool ToRect(
            Vector2 start,
            Vector2 end,
            Rect rect,
            out Vector2 clippedStart,
            out Vector2 clippedEnd)
        {
            const int Left = 1;
            const int Right = 2;
            const int Bottom = 4;
            const int Top = 8;

            float xMin = rect.xMin;
            float xMax = rect.xMax;
            float yMin = rect.yMin;
            float yMax = rect.yMax;

            Vector2 p0 = start;
            Vector2 p1 = end;

            int outCode0 = ComputeOutCode(p0, xMin, xMax, yMin, yMax, Left, Right, Bottom, Top);
            int outCode1 = ComputeOutCode(p1, xMin, xMax, yMin, yMax, Left, Right, Bottom, Top);

            bool accept = false;

            while (true)
            {
                if ((outCode0 | outCode1) == 0)
                {
                    accept = true;
                    break;
                }

                if ((outCode0 & outCode1) != 0)
                    break;

                int outCodeOut = outCode0 != 0 ? outCode0 : outCode1;
                float x = 0f;
                float y = 0f;

                if ((outCodeOut & Top) != 0)
                {
                    x = p0.x + (p1.x - p0.x) * (yMax - p0.y) / (p1.y - p0.y);
                    y = yMax;
                }
                else if ((outCodeOut & Bottom) != 0)
                {
                    x = p0.x + (p1.x - p0.x) * (yMin - p0.y) / (p1.y - p0.y);
                    y = yMin;
                }
                else if ((outCodeOut & Right) != 0)
                {
                    y = p0.y + (p1.y - p0.y) * (xMax - p0.x) / (p1.x - p0.x);
                    x = xMax;
                }
                else if ((outCodeOut & Left) != 0)
                {
                    y = p0.y + (p1.y - p0.y) * (xMin - p0.x) / (p1.x - p0.x);
                    x = xMin;
                }

                if (outCodeOut == outCode0)
                {
                    p0 = new Vector2(x, y);
                    outCode0 = ComputeOutCode(p0, xMin, xMax, yMin, yMax, Left, Right, Bottom, Top);
                }
                else
                {
                    p1 = new Vector2(x, y);
                    outCode1 = ComputeOutCode(p1, xMin, xMax, yMin, yMax, Left, Right, Bottom, Top);
                }
            }

            clippedStart = p0;
            clippedEnd = p1;
            return accept;
        }

        static int ComputeOutCode(
            Vector2 point,
            float xMin,
            float xMax,
            float yMin,
            float yMax,
            int left,
            int right,
            int bottom,
            int top)
        {
            int code = 0;
            if (point.x < xMin)
                code |= left;
            else if (point.x > xMax)
                code |= right;

            if (point.y < yMin)
                code |= bottom;
            else if (point.y > yMax)
                code |= top;

            return code;
        }
    }
}
