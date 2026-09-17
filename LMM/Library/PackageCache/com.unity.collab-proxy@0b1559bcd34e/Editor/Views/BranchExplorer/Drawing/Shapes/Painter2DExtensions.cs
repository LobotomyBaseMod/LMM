using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{

    internal static class PolygonExtensions
    {
        internal static bool ContainsPoint(this List<Vector2> polygon, Vector2 point)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            // ray casting algorithm for point-in-polygon
            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                // check if edge crosses horizontal ray from point
                bool edgeCrossesRay = (polygon[i].y > point.y) != (polygon[j].y > point.y);

                if (edgeCrossesRay)
                {
                    float intersectionX = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                        (polygon[j].y - polygon[i].y) + polygon[i].x;

                    // if the intersection is to the right of point, toggle inside
                    if (point.x < intersectionX)
                        inside = !inside;
                }

                j = i;
            }

            return inside;
        }
    }

    internal static class PathExtensions
    {
        internal static bool IsPointNear(this List<Vector2> path, Vector2 point, float tolerance)
        {
            if (path == null || path.Count < 2)
                return false;

            for (int i = 0; i < path.Count - 1; i++)
            {
                float distance = DistanceToSegment(point, path[i], path[i + 1]);
                if (distance <= tolerance)
                    return true;
            }

            return false;
        }

        internal static Rect CalculateBoundingBox(this List<Vector2> path)
        {
            if (path == null || path.Count == 0)
                return new Rect(0, 0, 0, 0);

            float minX = path[0].x;
            float minY = path[0].y;
            float maxX = path[0].x;
            float maxY = path[0].y;

            for (int i = 1; i < path.Count; i++)
            {
                Vector2 p = path[i];

                if (p.x < minX) minX = p.x;
                else if (p.x > maxX) maxX = p.x;

                if (p.y < minY) minY = p.y;
                else if (p.y > maxY) maxY = p.y;
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        static float DistanceToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float segmentLengthSquared = segment.sqrMagnitude;

            if (segmentLengthSquared == 0)
                return Vector2.Distance(point, segmentStart);

            // project point onto the line, clamping to segment
            float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / segmentLengthSquared);
            Vector2 projection = segmentStart + t * segment;

            return Vector2.Distance(point, projection);
        }
    }

    internal static class Painter2DExtensions
    {
        internal static void DrawRect(
            this Painter2D painter,
            Rect rect)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
        }

        internal static void DrawPoligon(
            this Painter2D painter,
            List<Vector2> poligon)
        {
            painter.BeginPath();

            for (int i = 0; i < poligon.Count; i++)
            {
                if (i == 0)
                    painter.MoveTo(poligon[i]);
                else
                    painter.LineTo(poligon[i]);
            }
        }

        internal static void DrawCircle(
            this Painter2D painter,
            Vector2 center,
            float radius)
        {
            painter.BeginPath();
            painter.Arc(
                center,
                radius,
                0,
                360);
            painter.ClosePath();
        }

        internal static void StrokeLine(
            this Painter2D painter,
            Vector2 start,
            Vector2 end,
            float[] dashPattern)
        {
            StrokeLine(painter, start, end, dashPattern, false, default);
        }

        internal static void StrokeLine(
            this Painter2D painter,
            Vector2 start,
            Vector2 end,
            float[] dashPattern,
            Rect clipRect)
        {
            StrokeLine(painter, start, end, dashPattern, true, clipRect);
        }

        internal static void StrokeCircle(
            this Painter2D painter,
            Vector2 vector2,
            int changesetRadius,
            float[] dashPattern)
        {
            if (dashPattern == null || dashPattern.Length == 0)
            {
                painter.DrawCircle(vector2, changesetRadius);
                painter.Stroke();
                return;
            }

            // Calculate the circumference to determine how to distribute dashes
            float circumference = 2 * Mathf.PI * changesetRadius;

            // Calculate total pattern length
            float patternLength = 0;
            foreach (float segment in dashPattern)
            {
                if (segment > 0)
                    patternLength += segment;
            }

            if (patternLength <= 0 || circumference <= 0)
            {
                painter.DrawCircle(vector2, changesetRadius);
                painter.Stroke();
                return;
            }

            // Draw dashed circle using arc segments
            float currentAngle = 0;
            float distanceCovered = 0;
            int patternIndex = 0;
            bool isDash = true; // Start with a dash

            while (distanceCovered < circumference)
            {
                float segmentLength = dashPattern[patternIndex % dashPattern.Length];
                if (segmentLength <= 0)
                {
                    patternIndex++;
                    isDash = !isDash;
                    continue;
                }

                float angleIncrement = (segmentLength / circumference) * 360f;

                if (isDash)
                {
                    // Draw the dash as an arc
                    painter.BeginPath();
                    painter.Arc(vector2, changesetRadius, currentAngle, currentAngle + angleIncrement);
                    painter.Stroke();
                }

                currentAngle += angleIncrement;
                distanceCovered += segmentLength;
                patternIndex++;
                isDash = !isDash; // Alternate between dash and gap
            }
        }

        internal static void StrokePath(
            this Painter2D painter,
            List<Vector2> path,
            float[] dashPattern)
        {
            StrokePath(painter, path, dashPattern, false, default);
        }

        internal static void StrokePath(
            this Painter2D painter,
            List<Vector2> path,
            float[] dashPattern,
            Rect clipRect)
        {
            StrokePath(painter, path, dashPattern, true, clipRect);
        }

        static void StrokeLine(
            Painter2D painter,
            Vector2 start,
            Vector2 end,
            float[] dashPattern,
            bool useClip,
            Rect clipRect)
        {
            if (dashPattern == null || dashPattern.Length == 0)
            {
                Vector2 clippedStart = default;
                Vector2 clippedEnd = default;

                if (!useClip || TryClipSegment.ToRect(start, end, clipRect, out clippedStart, out clippedEnd))
                {
                    Vector2 from = useClip ? clippedStart : start;
                    Vector2 to = useClip ? clippedEnd : end;
                    painter.BeginPath();
                    painter.MoveTo(from);
                    painter.LineTo(to);
                    painter.Stroke();
                }
                return;
            }

            Vector2 direction = end - start;
            float totalLength = direction.magnitude;
            if (totalLength <= 0f)
                return;

            direction /= totalLength;

            float patternLength = 0f;
            foreach (float segment in dashPattern)
            {
                if (segment > 0)
                    patternLength += segment;
            }

            if (patternLength <= 0f)
            {
                Vector2 clippedStart = default;
                Vector2 clippedEnd = default;

                if (!useClip || TryClipSegment.ToRect(start, end, clipRect, out clippedStart, out clippedEnd))
                {
                    Vector2 from = useClip ? clippedStart : start;
                    Vector2 to = useClip ? clippedEnd : end;
                    painter.BeginPath();
                    painter.MoveTo(from);
                    painter.LineTo(to);
                    painter.Stroke();
                }
                return;
            }

            float distanceCovered = 0f;
            int patternIndex = 0;
            bool isDash = true;

            while (distanceCovered < totalLength)
            {
                float segmentLength = dashPattern[patternIndex % dashPattern.Length];
                if (segmentLength <= 0f)
                {
                    patternIndex++;
                    isDash = !isDash;
                    continue;
                }

                float remaining = totalLength - distanceCovered;
                float lengthToUse = Mathf.Min(segmentLength, remaining);

                if (isDash)
                {
                    Vector2 segmentStart = start + direction * distanceCovered;
                    Vector2 segmentEnd = start + direction * (distanceCovered + lengthToUse);

                    Vector2 clippedStart = default;
                    Vector2 clippedEnd = default;

                    if (!useClip || TryClipSegment.ToRect(segmentStart, segmentEnd, clipRect, out clippedStart, out clippedEnd))
                    {
                        Vector2 from = useClip ? clippedStart : segmentStart;
                        Vector2 to = useClip ? clippedEnd : segmentEnd;
                        painter.BeginPath();
                        painter.MoveTo(from);
                        painter.LineTo(to);
                        painter.Stroke();
                    }
                }

                distanceCovered += segmentLength;
                patternIndex++;
                isDash = !isDash;
            }
        }

        static void StrokePath(
            Painter2D painter,
            List<Vector2> path,
            float[] dashPattern,
            bool useClip,
            Rect clipRect)
        {
            if (path == null || path.Count < 2)
                return;

            if (dashPattern == null || dashPattern.Length == 0)
            {
                painter.BeginPath();
                painter.MoveTo(path[0]);
                for (int i = 1; i < path.Count; i++)
                    painter.LineTo(path[i]);
                painter.Stroke();
                return;
            }

            float totalLength = 0f;
            for (int i = 0; i < path.Count - 1; i++)
                totalLength += Vector2.Distance(path[i], path[i + 1]);

            if (totalLength <= 0f)
                return;

            float patternLength = 0f;
            foreach (float segment in dashPattern)
            {
                if (segment > 0)
                    patternLength += segment;
            }

            if (patternLength <= 0f)
            {
                painter.BeginPath();
                painter.MoveTo(path[0]);
                for (int i = 1; i < path.Count; i++)
                    painter.LineTo(path[i]);
                painter.Stroke();
                return;
            }

            int patternIndex = 0;
            bool isDash = true;
            float segmentRemaining = dashPattern[patternIndex % dashPattern.Length];

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 segmentStart = path[i];
                Vector2 segmentEnd = path[i + 1];
                Vector2 segmentVector = segmentEnd - segmentStart;
                float segmentLength = segmentVector.magnitude;

                if (segmentLength <= 0f)
                    continue;

                Vector2 direction = segmentVector / segmentLength;
                float distanceCovered = 0f;

                while (distanceCovered < segmentLength)
                {
                    if (segmentRemaining <= 0f)
                    {
                        patternIndex++;
                        isDash = !isDash;
                        segmentRemaining = dashPattern[patternIndex % dashPattern.Length];
                        continue;
                    }

                    float remainingInSegment = segmentLength - distanceCovered;
                    float lengthToUse = Mathf.Min(segmentRemaining, remainingInSegment);

                    if (isDash)
                    {
                        Vector2 dashStart = segmentStart + direction * distanceCovered;
                        Vector2 dashEnd = dashStart + direction * lengthToUse;

                        Vector2 clippedStart = default;
                        Vector2 clippedEnd = default;

                        if (!useClip || TryClipSegment.ToRect(dashStart, dashEnd, clipRect, out clippedStart, out clippedEnd))
                        {
                            Vector2 from = useClip ? clippedStart : dashStart;
                            Vector2 to = useClip ? clippedEnd : dashEnd;
                            painter.BeginPath();
                            painter.MoveTo(from);
                            painter.LineTo(to);
                            painter.Stroke();
                        }
                    }

                    distanceCovered += lengthToUse;
                    segmentRemaining -= lengthToUse;
                }
            }
        }

        internal static void PreciseArcTo(
            this Painter2D painter,
            Vector2 currentPoint,
            Vector2 targetPoint,
            Vector2 size,
            float rotationAngle,
            bool isLargeArc,
            ArcDirection direction)
        {
            EllipticalArc.BuildArc(
                painter,
                currentPoint,
                targetPoint,
                size,
                rotationAngle * Mathf.Deg2Rad,
                isLargeArc,
                direction == ArcDirection.Clockwise);
        }

        internal static void AppendCubicBezierToPath(
            Vector2 start,
            Vector2 controlPoint1,
            Vector2 controlPoint2,
            Vector2 end,
            List<Vector2> hitTestPath)
        {
            if (hitTestPath.Count == 0 || hitTestPath[hitTestPath.Count - 1] != start)
                hitTestPath.Add(start);

            float toleranceSqr = DEFAULT_TOLERANCE * DEFAULT_TOLERANCE;
            Stack<(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int depth)> stack =
                new Stack<(Vector2, Vector2, Vector2, Vector2, int)>();
            stack.Push((start, controlPoint1, controlPoint2, end, 0));

            while (stack.Count > 0)
            {
                var segment = stack.Pop();

                if (segment.depth >= MAX_SUBDIVISION_DEPTH ||
                    IsBezierFlatEnough(segment.p0, segment.p1, segment.p2, segment.p3, toleranceSqr))
                {
                    hitTestPath.Add(segment.p3);
                    continue;
                }

                Vector2 p01 = (segment.p0 + segment.p1) * 0.5f;
                Vector2 p12 = (segment.p1 + segment.p2) * 0.5f;
                Vector2 p23 = (segment.p2 + segment.p3) * 0.5f;
                Vector2 p012 = (p01 + p12) * 0.5f;
                Vector2 p123 = (p12 + p23) * 0.5f;
                Vector2 p0123 = (p012 + p123) * 0.5f;

                // Push in reverse order so first half is processed first
                stack.Push((p0123, p123, p23, segment.p3, segment.depth + 1));
                stack.Push((segment.p0, p01, p012, p0123, segment.depth + 1));
            }
        }

        internal static bool IsBezierFlatEnough(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float toleranceSqr)
        {
            float d1 = DistancePointToLineSqr(p1, p0, p3);
            float d2 = DistancePointToLineSqr(p2, p0, p3);
            return Mathf.Max(d1, d2) <= toleranceSqr;
        }

        internal static float DistancePointToLineSqr(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 line = lineEnd - lineStart;
            float lenSq = line.sqrMagnitude;
            if (lenSq < 0.0001f)
                return (point - lineStart).sqrMagnitude;

            float t = Vector2.Dot(point - lineStart, line) / lenSq;
            Vector2 projection = lineStart + t * line;
            return (point - projection).sqrMagnitude;
        }

        const float DEFAULT_TOLERANCE = 1.0f;
        const int MAX_SUBDIVISION_DEPTH = 10;
    }
}

#if !UNITY_2022_1_OR_NEWER
namespace UnityEngine.UIElements
{
    // placeholder for Painter2D in Unity versions prior to 2022.1 to avoid #ifdef mess
    internal enum ArcDirection { Clockwise, CounterClockwise }
    internal enum LineJoin { Miter, Bevel, Round }
    internal enum LineCap { Butt, Round, Square }

    internal class Painter2D
    {
        internal Color fillColor;
        internal Color strokeColor;
        internal float lineWidth;
        internal object lineJoin;
        internal object lineCap;

        internal void BeginPath() { }
        internal void MoveTo(Vector2 vector) { }
        internal void LineTo(Vector2 p) { }
        internal void ClosePath() { }
        internal void ArcTo(Vector2 p0, Vector2 p1, float radiusX) { }
        internal void Arc(Vector2 center, float radius, float i, float i1) { }
        internal  void Stroke() { }
        internal void Fill() { }
        internal void BezierCurveTo(Vector2 vector2, Vector2 vector3, Vector2 vector4) { }
    }
}
#endif
