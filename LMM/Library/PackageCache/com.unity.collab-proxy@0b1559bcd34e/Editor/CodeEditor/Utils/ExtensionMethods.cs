using System;
using System.Collections.Generic;
using Unity.CodeEditor.Editing;
using UnityEngine;

namespace Unity.CodeEditor.Utils
{
    //PENDING TO COMPLETE
    internal static class ExtensionMethods
    {
        #region Rectangle

        internal static Rect WithY(this Rect rect, float y)
        {
            return new Rect(rect.x, y, rect.width, rect.height);
        }

        internal static Rect WithWidth(this Rect rect, float width)
        {
            return new Rect(rect.x, rect.y, width, rect.height);
        }

        #endregion

        #region Vector2
        internal static bool IsClose(this Vector2 v, Vector2 other)
        {
            return IsClose(v.x, other.x) && IsClose(v.y, other.y);
        }

        internal static Vector2 RelativeTo(this Vector2 v, Rect rect)
        {
            return new Vector2(v.x - rect.x, v.y - rect.y);
        }

        #endregion

        #region Epsilon / IsClose / CoerceValue
        /// <summary>
        /// Epsilon used for <c>IsClose()</c> implementations.
        /// We can use up quite a few digits in front of the decimal point (due to visual positions being relative to document origin),
        /// and there's no need to be too accurate (we're dealing with pixels here),
        /// so we will use the value 0.01.
        /// Previosly we used 1e-8 but that was causing issues:
        /// https://community.sharpdevelop.net/forums/t/16048.aspx
        /// </summary>
        internal const float Epsilon = 0.01f;

        /// <summary>
        /// Returns true if the doubles are close (difference smaller than 0.01).
        /// </summary>
        internal static bool IsClose(this float d1, float d2)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (d1 == d2) // required for infinities
                return true;
            return Math.Abs(d1 - d2) < Epsilon;
        }

        /// <summary>
        /// Returns true if the doubles are close (difference smaller than 0.01).
        /// </summary>
        internal static bool IsClose(this double d1, double d2)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (d1 == d2) // required for infinities
                return true;
            return Math.Abs(d1 - d2) < Epsilon;
        }

        /// <summary>
        /// Returns true if the doubles are close (difference smaller than 0.01).
        /// </summary>
        /*public static bool IsClose(this Size d1, Size d2)
        {
            return IsClose(d1.Width, d2.Width) && IsClose(d1.Height, d2.Height);
        }*/

        /// <summary>
        /// Forces the value to stay between mininum and maximum.
        /// </summary>
        /// <returns>minimum, if value is less than minimum.
        /// Maximum, if value is greater than maximum.
        /// Otherwise, value.</returns>
        internal static double CoerceValue(this double value, double minimum, double maximum)
        {
            return Math.Max(Math.Min(value, maximum), minimum);
        }

        /// <summary>
        /// Forces the value to stay between mininum and maximum.
        /// </summary>
        /// <returns>minimum, if value is less than minimum.
        /// Maximum, if value is greater than maximum.
        /// Otherwise, value.</returns>
        internal static int CoerceValue(this int value, int minimum, int maximum)
        {
            return Math.Max(Math.Min(value, maximum), minimum);
        }
        #endregion

        #region AddRange / Sequence
        internal static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
        {
            foreach (T e in elements)
                collection.Add(e);
        }

        /// <summary>
        /// Creates an IEnumerable with a single value.
        /// </summary>
        internal static IEnumerable<T> Sequence<T>(T value)
        {
            yield return value;
        }
        #endregion

        internal static T PeekOrDefault<T>(this ImmutableStack<T> stack)
        {
            return stack.IsEmpty ? default(T) : stack.Peek();
        }

        /// <summary>
        /// Gets the union of two rectangles.
        /// </summary>
        /// <param name="other">The other rectangle.</param>
        /// <returns>The union.</returns>
        internal static Rect Union(this Rect rect, Rect other)
        {
            if (rect.width == 0 && rect.height == 0)
            {
                return other;
            }
            else if (other.width == 0 && other.height == 0)
            {
                return rect;
            }
            else
            {
                var x1 = Math.Min(rect.x, other.x);
                var x2 = Math.Max(rect.xMax, other.xMax);
                var y1 = Math.Min(rect.y, other.y);
                var y2 = Math.Max(rect.yMax, other.yMax);

                return new Rect(new Vector2(x1, y1), new Vector2(x2, y2));
            }
        }
    }
}
