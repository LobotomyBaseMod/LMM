using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections
{
    internal static class BezierCurveExtensions
    {
        internal static void DrawCubicBezier(
            this Painter2D painter,
            Vector2 start,
            Vector2 controlPoint1,
            Vector2 controlPoint2,
            Vector2 end,
            List<Vector2> hitTestPath)
        {
            painter.BeginPath();
            painter.MoveTo(start);

            painter.AppendCubicBezier(start,
                controlPoint1,
                controlPoint2,
                end,
                hitTestPath,
                true);
        }

        internal static void AppendCubicBezier(
            this Painter2D painter,
            Vector2 start,
            Vector2 controlPoint1,
            Vector2 controlPoint2,
            Vector2 end,
            List<Vector2> hitTestPath,
            bool includeStart)
        {
            if (includeStart && hitTestPath != null)
                AddPointIfNeeded(hitTestPath, start);

            float toleranceSqr = DEFAULT_TOLERANCE * DEFAULT_TOLERANCE;
            List<BezierSegment> stack = GetSegmentStack();
            stack.Add(new BezierSegment(start, controlPoint1, controlPoint2, end, 0));

            while (stack.Count > 0)
            {
                int lastIndex = stack.Count - 1;
                BezierSegment segment = stack[lastIndex];
                stack.RemoveAt(lastIndex);

                if (segment.Depth >= MAX_SUBDIVISION_DEPTH ||
                    IsFlatEnough(segment, toleranceSqr))
                {
                    painter.LineTo(segment.P3);
                    if (hitTestPath != null)
                        hitTestPath.Add(segment.P3);
                    continue;
                }

                Vector2 p01 = (segment.P0 + segment.P1) * 0.5f;
                Vector2 p12 = (segment.P1 + segment.P2) * 0.5f;
                Vector2 p23 = (segment.P2 + segment.P3) * 0.5f;
                Vector2 p012 = (p01 + p12) * 0.5f;
                Vector2 p123 = (p12 + p23) * 0.5f;
                Vector2 p0123 = (p012 + p123) * 0.5f;

                stack.Add(new BezierSegment(p0123, p123, p23, segment.P3, segment.Depth + 1));
                stack.Add(new BezierSegment(segment.P0, p01, p012, p0123, segment.Depth + 1));
            }
        }

        static bool IsFlatEnough(BezierSegment segment, float toleranceSqr)
        {
            float d1 = DistancePointToLineSqr(segment.P1, segment.P0, segment.P3);
            float d2 = DistancePointToLineSqr(segment.P2, segment.P0, segment.P3);
            return Mathf.Max(d1, d2) <= toleranceSqr;
        }

        static float DistancePointToLineSqr(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 line = lineEnd - lineStart;
            float lenSq = line.sqrMagnitude;
            if (lenSq < MIN_LINE_LENGTH_SQR)
                return (point - lineStart).sqrMagnitude;

            float t = Vector2.Dot(point - lineStart, line) / lenSq;
            Vector2 projection = lineStart + t * line;
            return (point - projection).sqrMagnitude;
        }

        static void AddPointIfNeeded(List<Vector2> path, Vector2 point)
        {
            if (path.Count == 0 || path[path.Count - 1] != point)
                path.Add(point);
        }

        static List<BezierSegment> GetSegmentStack()
        {
            if (mSegmentStack == null)
                mSegmentStack = new List<BezierSegment>(64);
            else
                mSegmentStack.Clear();

            return mSegmentStack;
        }

        const float DEFAULT_TOLERANCE = 0.15f;
        const int MAX_SUBDIVISION_DEPTH = 12;
        const float MIN_LINE_LENGTH_SQR = 0.0001f;

        static List<BezierSegment> mSegmentStack;

        readonly struct BezierSegment
        {
            internal readonly Vector2 P0;
            internal readonly Vector2 P1;
            internal readonly Vector2 P2;
            internal readonly Vector2 P3;
            internal readonly int Depth;

            internal BezierSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int depth)
            {
                P0 = p0;
                P1 = p1;
                P2 = p2;
                P3 = p3;
                Depth = depth;
            }
        }
    }
}
