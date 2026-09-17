using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections
{
    internal static class MergeLinkGeometry
    {
        internal static void CalculateHitTestPath(
            Rect source,
            Rect destination,
            out List<Vector2> hitTestPath)
        {
            Draw(null, source, destination, null, false, default, out hitTestPath);
        }

        internal static void Draw(
            Painter2D painter,
            Rect normalizedSource,
            Rect normalizedDestination)
        {
            Draw(painter, normalizedSource, normalizedDestination, null, false, default, out _);
        }

        internal static void Draw(
            Painter2D painter,
            Rect normalizedSource,
            Rect normalizedDestination,
            float[] dashPattern)
        {
            Draw(painter, normalizedSource, normalizedDestination, dashPattern, false, default, out _);
        }

        internal static void Draw(
            Painter2D painter,
            Rect normalizedSource,
            Rect normalizedDestination,
            float[] dashPattern,
            Rect clipRect)
        {
            Draw(painter, normalizedSource, normalizedDestination, dashPattern, true, clipRect, out _);
        }

        static void Draw(
            Painter2D painter,
            Rect normalizedSource,
            Rect normalizedDestination,
            float[] dashPattern,
            bool useClip,
            Rect clipRect,
            out List<Vector2> hitTestPath)
        {
            hitTestPath = new List<Vector2>();

            if (normalizedSource.y == normalizedDestination.y)
            {
                DrawHorizontalCurve(
                    painter,
                    normalizedSource,
                    normalizedDestination,
                    dashPattern,
                    useClip,
                    clipRect,
                    hitTestPath);
                return;
            }

            if (normalizedSource.x == normalizedDestination.x)
            {
                DrawVerticalLine(
                    painter,
                    normalizedSource,
                    normalizedDestination,
                    dashPattern,
                    useClip,
                    clipRect,
                    hitTestPath);
                return;
            }

            DrawCurve(
                painter,
                normalizedSource,
                normalizedDestination,
                dashPattern,
                useClip,
                clipRect,
                hitTestPath);
        }

        static void DrawHorizontalCurve(
            Painter2D painter,
            Rect source,
            Rect destination,
            float[] dashPattern,
            bool useClip,
            Rect clipRect,
            List<Vector2> hitTestPath)
        {
            float sourceRadius = source.height / 2;
            float destRadius = destination.height / 2;

            float sourcex, sourcey, destx, desty;

            if (source.x < destination.x)
            {
                sourcex = source.x + source.width - sourceRadius;
                sourcey = source.y;

                destx = destination.x + destRadius;
                desty = destination.y;
            }
            else
            {
                sourcex = source.x + sourceRadius;
                sourcey = source.y;

                destx = destination.x + destination.width - destRadius;
                desty = destination.y;
            }

            float offsetX = sourcex - destx;
            float middle = sourcex - offsetX / 2;
            float tension = 20 + Mathf.Abs(offsetX) * 0.05f;

            float i1x = middle - 10;
            float i1y = sourcey - tension;
            float i2x = middle + 10;
            float i2y = sourcey - tension;

            float srcx, srcy, middlex, middley;

            if (offsetX < 0)
            {
                srcx = i1x;
                srcy = i1y;
                middlex = i2x;
                middley = i2y;
            }
            else
            {
                srcx = i2x;
                srcy = i2y;
                middlex = i1x;
                middley = i1y;
            }

            Vector2[] vertex = GetArrowVertex(
                middlex, middley, destx, desty, DEFAULT_HEAD_ANGLE, MERGE_LINK_HEAD_SIZE);

            float lineEndX = Mathf.Round((vertex[0].x + vertex[1].x) / 2);
            float lineEndY = Mathf.Round((vertex[0].y + vertex[1].y) / 2);

            Vector2 start = new Vector2(sourcex, sourcey);
            Vector2 cp1 = new Vector2(srcx, srcy);
            Vector2 cp2 = new Vector2(middlex, middley);
            Vector2 end = new Vector2(lineEndX, lineEndY);
            Vector2 arrowTip = new Vector2(destx, desty);

            DrawBezierCurve(painter, start, cp1, cp2, end, dashPattern, useClip, clipRect, hitTestPath);
            DrawArrowHead(painter, arrowTip, vertex, hitTestPath);
        }

        static void DrawVerticalLine(
            Painter2D painter,
            Rect source,
            Rect dest,
            float[] dashPattern,
            bool useClip,
            Rect clipRect,
            List<Vector2> hitTestPath)
        {
            float x = source.x + source.width / 2;

            float sx, sy, dx, dy;

            if (source.y < dest.y)
            {
                sx = x;
                sy = source.y + source.height;
                dx = x;
                dy = dest.y;
            }
            else
            {
                sx = x;
                sy = source.y;
                dx = x;
                dy = dest.y + dest.height;
            }

            Vector2[] vertex = GetArrowVertex(
                sx, sy, dx, dy, DEFAULT_HEAD_ANGLE, MERGE_LINK_HEAD_SIZE);

            float lineEndX = (vertex[0].x + vertex[1].x) / 2;
            float lineEndY = (vertex[0].y + vertex[1].y) / 2;

            Vector2 start = new Vector2(sx, sy);
            Vector2 end = new Vector2(lineEndX, lineEndY);
            Vector2 arrowTip = new Vector2(dx, dy);

            DrawLine(painter, start, end, dashPattern, useClip, clipRect, hitTestPath);
            DrawArrowHead(painter, arrowTip, vertex, hitTestPath);
        }

        static void DrawCurve(
            Painter2D painter,
            Rect source,
            Rect dest,
            float[] dashPattern,
            bool useClip,
            Rect clipRect,
            List<Vector2> hitTestPath)
        {
            bool offsetVertically = GetYGrowingRatio(source, dest) < MIN_VERTICAL_OFFSET_RATIO;

            if (4 * VERTICAL_SOURCE_OFFSET >= Mathf.Abs(source.y - dest.y))
            {
                // not allowed -> generates an empty rectangle
                offsetVertically = false;
            }

            float sourcex, sourcey, destx, desty, rectanglex, rectangley, rectangleWidth, rectangleHeight;

            float verticalSourceOffset = 0;
            float sourceRadius = source.height / 2;
            float destRadius = dest.height / 2;

            EllipseQuadrant quadrant;

            float vectorDestinationx;
            float factor = 13;
            float angleMultiplicity = 1;
            bool symmetricCase = false;

            if (dest.x > source.x && dest.y < source.y)
            {
                // right top
                sourcex = source.x + source.width - sourceRadius;
                sourcey = source.y;

                // left bottom
                destx = dest.x + destRadius;
                desty = dest.y + dest.height;

                rectanglex = sourcex - Mathf.Abs(destx - sourcex);
                rectangley = desty - Mathf.Abs(desty - sourcey);

                rectangleWidth = Mathf.Abs(destx - sourcex) * 2;
                rectangleHeight = Mathf.Abs(desty - sourcey) * 2;

                vectorDestinationx = rectanglex + rectangleWidth;
                angleMultiplicity = -angleMultiplicity;

                if (offsetVertically)
                {
                    verticalSourceOffset = -GetVerticalSourceOffset(source, dest);
                    rectangley = rectangley + Mathf.Abs(verticalSourceOffset);
                    rectangleHeight = rectangleHeight - 2 * Mathf.Abs(verticalSourceOffset);
                }

                quadrant = EllipseQuadrant.First;
            }
            else if (dest.x < source.x && dest.y > source.y)
            {
                // left bottom
                sourcex = source.x + sourceRadius;
                sourcey = source.y + source.height;

                // right top
                destx = dest.x + dest.width - sourceRadius;
                desty = dest.y;

                rectanglex = sourcex - Mathf.Abs(destx - sourcex);
                rectangley = sourcey;

                rectangleWidth = Mathf.Abs(destx - sourcex) * 2;
                rectangleHeight = Mathf.Abs(desty - sourcey) * 2;

                vectorDestinationx = rectanglex;

                factor = -factor;
                symmetricCase = true;

                if (offsetVertically)
                {
                    verticalSourceOffset = GetVerticalSourceOffset(source, dest);
                    rectangley = sourcey + verticalSourceOffset;
                    rectangleHeight = rectangleHeight - 2 * verticalSourceOffset;
                }

                quadrant = EllipseQuadrant.Third;
            }
            else if (dest.x > source.x && dest.y > source.y)
            {
                // right bottom
                sourcex = source.x + source.width - sourceRadius;
                sourcey = source.y + source.height;

                // left top
                destx = dest.x + destRadius;
                desty = dest.y;

                rectanglex = sourcex - Mathf.Abs(destx - sourcex);
                rectangley = sourcey;

                rectangleWidth = Mathf.Abs(destx - sourcex) * 2;
                rectangleHeight = Mathf.Abs(desty - sourcey) * 2;

                vectorDestinationx = rectanglex + rectangleWidth;

                factor = -factor;
                angleMultiplicity = -angleMultiplicity;

                if (offsetVertically)
                {
                    verticalSourceOffset = GetVerticalSourceOffset(source, dest);
                    rectangley = rectangley + verticalSourceOffset;
                    rectangleHeight = rectangleHeight - 2 * verticalSourceOffset;
                }

                quadrant = EllipseQuadrant.Fourth;
            }
            else
            {
                // left top
                sourcex = source.x + sourceRadius;
                sourcey = source.y;

                // right bottom
                destx = dest.x + dest.width - destRadius;
                desty = dest.y + dest.height;

                rectanglex = sourcex - Mathf.Abs(destx - sourcex);
                rectangley = desty - Mathf.Abs(desty - sourcey);

                rectangleWidth = Mathf.Abs(destx - sourcex) * 2;
                rectangleHeight = Mathf.Abs(desty - sourcey) * 2;

                vectorDestinationx = rectanglex;
                symmetricCase = true;

                if (offsetVertically)
                {
                    verticalSourceOffset = -GetVerticalSourceOffset(source, dest);
                    rectangley = rectangley + Mathf.Abs(verticalSourceOffset);
                    rectangleHeight = rectangleHeight - 2 * Mathf.Abs(verticalSourceOffset);
                }

                quadrant = EllipseQuadrant.Second;
            }

            float vectordestinationy = rectangley + rectangleHeight / 2;
            float vectorsourcey = ClampValue(rectangley + (rectangleHeight / 2) + factor,
                rectangley, rectangley + rectangleHeight);

            Ellipse outerEllipse = new Ellipse(rectanglex, rectangley, rectangleWidth, rectangleHeight);

            float vectorsourcex = outerEllipse.CalculateXPoint(vectorsourcey);

            if (symmetricCase)
                vectorsourcex = outerEllipse.GetSymmetricXPoint(vectorsourcex);

            Vector2[] vertex = GetArrowVertex(
                vectorsourcex, vectorsourcey, vectorDestinationx, vectordestinationy,
                DEFAULT_HEAD_ANGLE, MERGE_LINK_HEAD_SIZE);

            float lineEndX = Mathf.Round((vertex[0].x + vertex[1].x) / 2);
            float lineEndY = Mathf.Round((vertex[0].y + vertex[1].y) / 2);

            Vector2 start = new Vector2(sourcex, sourcey);
            Vector2 verticalOffsetPoint = new Vector2(sourcex, sourcey + verticalSourceOffset);
            Vector2 end = new Vector2(lineEndX, lineEndY);
            Vector2 arrowTip = new Vector2(vectorDestinationx, vectordestinationy);

            DrawEllipticalArc(
                painter,
                start,
                verticalOffsetPoint,
                end,
                offsetVertically,
                quadrant,
                dashPattern,
                useClip,
                clipRect,
                hitTestPath);

            DrawArrowHead(painter, arrowTip, vertex, hitTestPath);
        }

        static void DrawLine(
            Painter2D painter,
            Vector2 start,
            Vector2 end,
            float[] dashPattern,
            bool useClip,
            Rect clipRect,
            List<Vector2> hitTestPath)
        {
            if (painter != null)
            {
                if (useClip)
                    painter.StrokeLine(start, end, dashPattern, clipRect);
                else
                    painter.StrokeLine(start, end, dashPattern);
            }

            hitTestPath.Add(start);
            hitTestPath.Add(end);
        }

        static void DrawBezierCurve(
            Painter2D painter,
            Vector2 start,
            Vector2 controlPoint1,
            Vector2 controlPoint2,
            Vector2 end,
            float[] dashPattern,
            bool useClip,
            Rect clipRect,
            List<Vector2> hitTestPath)
        {
            if (painter != null)
            {
                if (dashPattern == null || dashPattern.Length == 0)
                {
                    painter.DrawCubicBezier(
                        start,
                        controlPoint1,
                        controlPoint2,
                        end,
                        hitTestPath);

                    painter.Stroke();
                }
                else
                {
                    Painter2DExtensions.AppendCubicBezierToPath(
                        start,
                        controlPoint1,
                        controlPoint2,
                        end,
                        hitTestPath);

                    if (useClip)
                        painter.StrokePath(hitTestPath, dashPattern, clipRect);
                    else
                        painter.StrokePath(hitTestPath, dashPattern);
                }
            }
            else
            {
                Painter2DExtensions.AppendCubicBezierToPath(
                    start,
                    controlPoint1,
                    controlPoint2,
                    end,
                    hitTestPath);
            }
        }

        static void DrawEllipticalArc(
            Painter2D painter,
            Vector2 start,
            Vector2 verticalOffsetPoint,
            Vector2 end,
            bool offsetVertically,
            EllipseQuadrant quadrant,
            float[] dashPattern,
            bool useClip,
            Rect clipRect,
            List<Vector2> hitTestPath)
        {
            hitTestPath.Add(start);

            if (offsetVertically)
            {
                hitTestPath.Add(verticalOffsetPoint);
            }

            Vector2 arcStart = offsetVertically ? verticalOffsetPoint : start;
            float ax = start.x;
            float ay = arcStart.y;
            float bx = end.x;
            float bby = end.y;

            Ellipse innerEllipse = new Ellipse(
                ax - Mathf.Abs(ax - bx), bby - Mathf.Abs(ay - bby),
                2 * Mathf.Abs(ax - bx), 2 * Mathf.Abs(ay - bby));

            Vector2 cp1, cp2;

            switch (quadrant)
            {
                case EllipseQuadrant.First:
                    cp1 = new Vector2(
                        Mathf.Round(innerEllipse.XM + innerEllipse.OX),
                        Mathf.Round(innerEllipse.YH));
                    cp2 = new Vector2(
                        Mathf.Round(innerEllipse.XW),
                        Mathf.Round(innerEllipse.YM + innerEllipse.OY));
                    break;
                case EllipseQuadrant.Second:
                    cp1 = new Vector2(
                        Mathf.Round(innerEllipse.XM - innerEllipse.OX),
                        Mathf.Round(innerEllipse.YH));
                    cp2 = new Vector2(
                        Mathf.Round(innerEllipse.X),
                        Mathf.Round(innerEllipse.YM + innerEllipse.OY));
                    break;
                case EllipseQuadrant.Third:
                    cp1 = new Vector2(
                        Mathf.Round(innerEllipse.XM - innerEllipse.OX),
                        Mathf.Round(innerEllipse.Y));
                    cp2 = new Vector2(
                        Mathf.Round(innerEllipse.X),
                        Mathf.Round(innerEllipse.YM - innerEllipse.OY));
                    break;
                case EllipseQuadrant.Fourth:
                default:
                    cp1 = new Vector2(
                        Mathf.Round(innerEllipse.XM + innerEllipse.OX),
                        Mathf.Round(innerEllipse.Y));
                    cp2 = new Vector2(
                        Mathf.Round(innerEllipse.XW),
                        Mathf.Round(innerEllipse.YM - innerEllipse.OY));
                    break;
            }

            if (painter != null)
            {
                if (dashPattern == null || dashPattern.Length == 0)
                {
                    painter.BeginPath();
                    painter.MoveTo(start);

                    if (offsetVertically)
                    {
                        painter.LineTo(verticalOffsetPoint);
                    }

                    painter.AppendCubicBezier(
                        arcStart,
                        cp1,
                        cp2,
                        end,
                        hitTestPath,
                        false);

                    painter.Stroke();
                }
                else
                {
                    Painter2DExtensions.AppendCubicBezierToPath(
                        arcStart,
                        cp1,
                        cp2,
                        end,
                        hitTestPath);

                    if (useClip)
                        painter.StrokePath(hitTestPath, dashPattern, clipRect);
                    else
                        painter.StrokePath(hitTestPath, dashPattern);
                }
            }
            else
            {
                Painter2DExtensions.AppendCubicBezierToPath(
                    arcStart,
                    cp1,
                    cp2,
                    end,
                    hitTestPath);
            }
        }

        static void DrawArrowHead(
            Painter2D painter,
            Vector2 tip,
            Vector2[] vertex,
            List<Vector2> hitTestPath)
        {
            if (painter != null)
            {
                Color fillColor = painter.strokeColor;
                painter.fillColor = fillColor;

                painter.BeginPath();
                painter.MoveTo(tip);
                painter.LineTo(vertex[0]);
                painter.LineTo(vertex[1]);
                painter.ClosePath();
                painter.Stroke();
                painter.Fill();
            }

            hitTestPath.Add(tip);
        }

        // internal for testing
        internal static float GetYGrowingRatio(Rect source, Rect dest)
        {
            float ydistance = Mathf.Abs(source.y - dest.y);
            float xdistance = Mathf.Abs(source.x - dest.x);

            float ratio;
            if (xdistance == 0)
            {
                ratio = float.MaxValue;
            }
            else
            {
                ratio = ydistance / xdistance;
            }

            return ratio;
        }

        // internal for testing
        internal static float GetVerticalSourceOffset(Rect source, Rect dest)
        {
            float result = VERTICAL_SOURCE_OFFSET;

            float yDistance = Mathf.Abs(source.y - dest.y);

            if (yDistance < VERTICAL_SOURCE_OFFSET)
            {
                result = yDistance / 2;
            }

            return result;
        }

        // internal for testing
        internal static float ClampValue(float value, float minValue, float maxValue)
        {
            if (value < minValue)
                return minValue;

            if (value > maxValue)
                return maxValue;

            return value;
        }

        // internal for testing
        internal static Vector2[] GetArrowVertex(
            float startx, float starty, float endx, float endy, int headAngle, int headSize)
        {
            float phi = Degrees.ToRadians(headAngle);
            float dy = endy - starty;
            float dx = endx - startx;

            float theta = Mathf.Atan2(dy, dx);
            float x, y, rho = theta + phi;

            Vector2[] vertex = new Vector2[2];

            for (int j = 0; j < 2; j++)
            {
                x = (endx - headSize * Mathf.Cos(rho));
                y = (endy - headSize * Mathf.Sin(rho));

                vertex[j] = new Vector2(x, y);

                rho = theta - phi;
            }
            return vertex;
        }

        const int VERTICAL_SOURCE_OFFSET = 10;
        const float MIN_VERTICAL_OFFSET_RATIO = 0.5f; // 0 never - 1 always
        const int MERGE_LINK_HEAD_SIZE = 4;
        const int DEFAULT_HEAD_ANGLE = 26;

        enum EllipseQuadrant
        {
            First,  // from 3 o'clock to 6 o'clock
            Second, // from 6 o'clock to 9 o'clock
            Third,  // from 9 o'clock to 12 o'clock
            Fourth  // from 12 o'clock to 3 o'clock
        }
    }
}
