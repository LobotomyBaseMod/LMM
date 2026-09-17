using UnityEngine;

namespace UnityEngine
{
    internal static class RectExtensions
    {
        internal static bool Contains(this Rect r, Rect other)
        {
            return r.xMin <= other.xMin &&
                   r.xMax >= other.xMax &&
                   r.yMin <= other.yMin &&
                   r.yMax >= other.yMax;
        }

        internal static bool Intersects(this Rect r, Rect other)
        {
            return r.xMin < other.xMax &&
                   r.xMax > other.xMin &&
                   r.yMin < other.yMax &&
                   r.yMax > other.yMin;
        }

        internal static Rect Intersect(this Rect rect, Rect other)
        {
            float xMin = Mathf.Max(rect.xMin, other.xMin);
            float xMax = Mathf.Min(rect.xMax, other.xMax);
            float yMin = Mathf.Max(rect.yMin, other.yMin);
            float yMax = Mathf.Min(rect.yMax, other.yMax);

            // If there's no intersection, return a rect with zero width/height
            if (xMax < xMin || yMax < yMin)
                return new Rect(0, 0, 0, 0);

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        internal static Rect Inflate(this Rect rect, float amount)
        {
            return rect.Inflate(amount, amount);
        }

        internal static Rect Inflate(this Rect rect, float xAmount, float yAmount)
        {
            return new Rect(
                rect.xMin - xAmount,
                rect.yMin - yAmount,
                rect.width + (2 * xAmount),
                rect.height + (2 * yAmount));
        }
    }
}
