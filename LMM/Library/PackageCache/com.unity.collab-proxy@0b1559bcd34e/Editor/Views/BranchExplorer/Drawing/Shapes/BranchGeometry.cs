using System.Collections.Generic;

using Codice.Client.BaseCommands.BranchExplorer;

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal static class BranchGeometry
    {
        const float BRANCH_RADIUS = 12f;

        internal static void Draw(
            Painter2D painter,
            Rect main,
            SubBranchContainerDrawInfo[] subrectangles,
            out List<Vector2> hitTestPolygon)
        {
            Draw(
                painter,
                main.x, main.y, main.width, main.height,
                subrectangles,
                out hitTestPolygon);
        }

        static void Draw(
            Painter2D painter,
            float x,
            float y,
            float width,
            float height,
            SubBranchContainerDrawInfo[] containers,
            out List<Vector2> hitTestPolygon)
        {
            hitTestPolygon = new List<Vector2>();

            int shapeOffset =
                (BrExDrawProperties.ChangesetDrawingHeight -
                 BrExDrawProperties.BranchHeight) / 2;

            y += shapeOffset;
            height -= shapeOffset * 2;

            List<Rect> subRectangles = GetSubRectangles(containers);
            List<Rect> rectangles = CalculateFusions(subRectangles);

            Rect branchRectangle = new Rect(x, y, width, height);
            AdjustLastRectangle(rectangles, branchRectangle);
            AdjustFirstRectangle(rectangles, branchRectangle);

            float radius = height / 2;
            float xw = x + width;
            float yh = y + height;
            float xwr = xw - radius;
            float xr = x + radius;
            float r2 = radius * 2;
            float xwr2 = xw - r2;

            float angle = 180f;
            Vector2 rightArcTargetPoint = new Vector2(xwr, yh);

            if (rectangles.Count > 0)
            {
                Rect last = rectangles[rectangles.Count - 1];
                if (last.xMax >= xwr2 + r2)
                {
                    angle = 90f;
                    rightArcTargetPoint = new Vector2(xw, y + radius);
                }
            }

            painter.BeginPath();

            // Start at top-left after left arc
            Vector2 currentPoint = new Vector2(xr, y);
            painter.MoveTo(currentPoint);
            hitTestPolygon.Add(currentPoint);

            // Top Edge
            currentPoint = new Vector2(xwr, y);
            painter.LineTo(currentPoint);
            hitTestPolygon.Add(currentPoint);

            // Right arc
            painter.PreciseArcTo(
                currentPoint,
                rightArcTargetPoint,
                new Vector2(radius, radius),
                angle,
                false,
                ArcDirection.Clockwise);

            hitTestPolygon.Add(new Vector2(xw, y)); // top-right corner
            hitTestPolygon.Add(new Vector2(xw, yh)); // bottom-right corner
            currentPoint = rightArcTargetPoint;
            hitTestPolygon.Add(currentPoint);

            // Bottom Edge
            float currentx = rightArcTargetPoint.x;
            float currenty = rightArcTargetPoint.y;

            if (rectangles != null)
            {
                for (int i = rectangles.Count - 1; i >= 0; i--)
                {
                    Rect currentRectangle = rectangles[i];
                    Vector2 end = AddBottomRectangle(
                        painter,
                        currentRectangle,
                        currentx,
                        currenty,
                        x,
                        shapeOffset,
                        ref currentPoint,
                        hitTestPolygon);
                    currentx = end.x;
                    currenty = currentRectangle.y + shapeOffset;
                }
            }

            float angle2 = 180f;

            if (rectangles.Count > 0 && rectangles[0].x <= x)
            {
                angle2 = 90f;
                Vector2 leftArcStart = new Vector2(rectangles[0].x, y + radius);
                painter.LineTo(leftArcStart);
                hitTestPolygon.Add(leftArcStart);
                currentPoint = leftArcStart;
            }
            else
            {
                Vector2 leftArcStart = new Vector2(xr, yh);
                painter.LineTo(leftArcStart);
                hitTestPolygon.Add(leftArcStart);
                currentPoint = leftArcStart;
            }

            // Left arc
            Vector2 leftArcTarget = new Vector2(xr, y);
            painter.PreciseArcTo(
                currentPoint,
                leftArcTarget,
                new Vector2(radius, radius),
                angle2,
                false,
                ArcDirection.Clockwise);

            hitTestPolygon.Add(new Vector2(x, yh)); // bottom-left corner
            hitTestPolygon.Add(new Vector2(x, y)); // top-left corner

            painter.ClosePath();
        }

        // Add a rectangle in the bottom, from right to left
        static Vector2 AddBottomRectangle(
            Painter2D painter,
            Rect r,
            float x,
            float y,
            float minx,
            float shapeOffset,
            ref Vector2 currentPoint,
            List<Vector2> hitTestPolygon)
        {
            r = new Rect(r.x, r.y + shapeOffset, r.width, r.height);

            float xTopLeft = r.x - BRANCH_RADIUS;
            float xTopRight = r.xMax + BRANCH_RADIUS;
            float xBottomLeft = r.x + BRANCH_RADIUS;
            float xBottomRight = r.xMax - BRANCH_RADIUS;

            float yTop = r.y + BRANCH_RADIUS;
            float yBottom = r.yMax - BRANCH_RADIUS;

            Vector2 arcSize = new Vector2(BRANCH_RADIUS, BRANCH_RADIUS);

            if (x > r.xMax)
            {
                Vector2 topRight = new Vector2(xTopRight, r.y);
                painter.LineTo(topRight);
                hitTestPolygon.Add(topRight);
                currentPoint = topRight;

                Vector2 arcTarget = new Vector2(r.xMax, yTop);
                painter.PreciseArcTo(
                    currentPoint,
                    arcTarget,
                    arcSize,
                    90f,
                    false,
                    ArcDirection.CounterClockwise);
                // Go directly to arc target (diagonal covers the outward-curving arc)
                hitTestPolygon.Add(arcTarget);
                currentPoint = arcTarget;
            }

            Vector2 rightBottom = new Vector2(r.xMax, yBottom);
            painter.LineTo(rightBottom);
            hitTestPolygon.Add(rightBottom);
            currentPoint = rightBottom;

            Vector2 bottomRight = new Vector2(xBottomRight, r.yMax);
            painter.PreciseArcTo(
                currentPoint,
                bottomRight,
                arcSize,
                90f,
                false,
                ArcDirection.Clockwise);
            hitTestPolygon.Add(new Vector2(r.xMax, r.yMax)); // corner point
            hitTestPolygon.Add(bottomRight);
            currentPoint = bottomRight;

            Vector2 bottomLeft = new Vector2(xBottomLeft, r.yMax);
            painter.LineTo(bottomLeft);
            hitTestPolygon.Add(bottomLeft);
            currentPoint = bottomLeft;

            Vector2 leftBottom = new Vector2(r.x, yBottom);
            painter.PreciseArcTo(
                currentPoint,
                leftBottom,
                arcSize,
                90f,
                false,
                ArcDirection.Clockwise);
            hitTestPolygon.Add(new Vector2(r.x, r.yMax)); // corner point
            hitTestPolygon.Add(leftBottom);
            currentPoint = leftBottom;

            Vector2 leftTop = new Vector2(r.x, yTop);
            painter.LineTo(leftTop);
            hitTestPolygon.Add(leftTop);
            currentPoint = leftTop;

            if (r.x > minx)
            {
                Vector2 topLeft = new Vector2(xTopLeft, r.y);
                painter.PreciseArcTo(
                    currentPoint,
                    topLeft,
                    arcSize,
                    90f,
                    false,
                    ArcDirection.CounterClockwise);
                // Go directly to arc target (diagonal covers the outward-curving arc)
                hitTestPolygon.Add(topLeft);
                currentPoint = topLeft;
            }
            else
            {
                Vector2 topLeft = new Vector2(r.x, r.y);
                painter.LineTo(topLeft);
                hitTestPolygon.Add(topLeft);
                currentPoint = topLeft;
            }

            // return where the path ends
            return new Vector2(xTopLeft, r.y);
        }

        // internal for testing
        internal static List<Rect> CalculateFusions(List<Rect> rectangles)
        {
            if (rectangles.Count < 2)
                return rectangles;

            List<Rect> result = new List<Rect>();

            for (int i = 0; i < rectangles.Count - 1; i++)
            {
                Rect current = rectangles[i];
                Rect next = rectangles[i + 1];

                if (current.xMax + (2 * BRANCH_RADIUS) >= next.x)
                {
                    return CalculateFusions(Fusion(rectangles, current, next, i, i + 1));
                }

                result.Add(current);
            }

            result.Add(rectangles[rectangles.Count - 1]);

            return result;
        }

        // internal for testing
        internal static List<Rect> Fusion(
            List<Rect> rectangles,
            Rect current,
            Rect next,
            int index1,
            int index2)
        {
            List<Rect> result = new List<Rect>();

            Rect fusioned = new Rect(
                current.x,
                current.y,
                current.width + 2 * BRANCH_RADIUS + next.width,
                Mathf.Max(current.height, next.height));

            for (int i = 0; i < rectangles.Count; i++)
            {
                if (i == index2)
                    continue;

                if (i == index1)
                {
                    result.Add(fusioned);
                }
                else
                {
                    result.Add(rectangles[i]);
                }
            }

            return result;
        }

        // internal for testing
        internal static void AdjustFirstRectangle(List<Rect> rectangles, Rect branchRectangle)
        {
            if (rectangles.Count == 0)
                return;

            Rect first = rectangles[0];

            if (branchRectangle.x + (3 * BRANCH_RADIUS) > first.x)
            {
                Rect newFirst = new Rect(
                    branchRectangle.x,
                    first.y,
                    first.width + (first.x - branchRectangle.x),
                    first.height);

                rectangles[0] = newFirst;
            }
        }

        // internal for testing
        internal static void AdjustLastRectangle(List<Rect> rectangles, Rect branchRectangle)
        {
            if (rectangles.Count == 0)
                return;

            Rect last = rectangles[rectangles.Count - 1];

            if (last.x + last.width + (3 * BRANCH_RADIUS) >= branchRectangle.x + branchRectangle.width)
            {
                Rect newLast = new Rect(
                    last.x,
                    last.y,
                    branchRectangle.xMax - last.x,
                    last.height);

                rectangles[rectangles.Count - 1] = newLast;
            }
        }

        // internal for testing
        internal static List<Rect> GetSubRectangles(SubBranchContainerDrawInfo[] containers)
        {
            List<Rect> result = new List<Rect>();

            if (containers == null)
                return result;

            foreach (SubBranchContainerDrawInfo container in containers)
            {
                result.Add(ConvertToRect(container.Bounds));
            }

            return result;
        }

        // internal for testing
        internal static Rect ConvertToRect(BrExRectangle rectangle)
        {
            return new Rect(
                rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
    }
}
