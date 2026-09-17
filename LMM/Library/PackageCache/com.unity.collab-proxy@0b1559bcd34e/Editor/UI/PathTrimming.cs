using System.IO;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal interface ICalcTextSize
    {
        float GetTextWidth(string text);
    }

    internal class CalcTextSize : ICalcTextSize
    {
        internal static ICalcTextSize FromStyle(GUIStyle style)
        {
            return new CalcTextSize(style);
        }

        CalcTextSize(GUIStyle style)
        {
            mStyle = style;
        }

        float ICalcTextSize.GetTextWidth(string text)
        {
            return mStyle.CalcSize(new GUIContent(text)).x;
        }

        readonly GUIStyle mStyle;
    }

    internal static class PathTrimming
    {
        /// <summary>
        /// Truncates a path to fit within the available width using binary search.
        /// Always preserves the beginning and end of the path when truncating.
        /// </summary>
        internal static string TruncatePath(
            string path,
            float availableWidth,
            ICalcTextSize calcTextSize,
            out bool wasTrimmed)
        {
            wasTrimmed = false;

            if (availableWidth <= 0)
                return string.Empty;

            float fullWidth = calcTextSize.GetTextWidth(path);

            // Path fits completely
            if (fullWidth <= availableWidth)
                return path;

            wasTrimmed = true;

            // Binary search for the optimal character count
            int minChars = 1;
            int maxChars = path.Length;
            int bestLength = 0;
            string bestFit = string.Empty;

            while (minChars <= maxChars)
            {
                int midLength = (minChars + maxChars) / 2;
                string truncated = TruncatePathByLength(path, midLength);
                float width = calcTextSize.GetTextWidth(truncated);

                if (width <= availableWidth)
                {
                    // This fits, try to fit more
                    bestLength = midLength;
                    bestFit = truncated;
                    minChars = midLength + 1;
                    continue;
                }

                // Too wide, try less
                maxChars = midLength - 1;
            }

            // If we couldn't fit anything reasonable, ensure at least some content is shown
            if (bestLength == 0 || string.IsNullOrEmpty(bestFit))
            {
                // Show at least part of the filename
                int lastSeparator = FindLastSeparator(path);
                if (lastSeparator != -1)
                {
                    string filename = path.Substring(lastSeparator + 1);

                    // Try to fit just the filename with middle truncation
                    minChars = 1;
                    maxChars = filename.Length;

                    while (minChars <= maxChars)
                    {
                        int midLength = (minChars + maxChars) / 2;
                        string truncated = TruncateMid(filename, midLength);
                        float width = calcTextSize.GetTextWidth(truncated);

                        if (width <= availableWidth)
                        {
                            bestFit = truncated;
                            minChars = midLength + 1;
                            continue;
                        }

                        maxChars = midLength - 1;
                    }
                }
                else
                {
                    // No path separator, just truncate the middle
                    bestFit = TruncateMid(path, 3);
                }
            }

            return bestFit;
        }

        /// <summary>
        /// Finds the last occurrence of either forward slash or backslash separator.
        /// Works cross-platform for both Windows and Unix-style paths.
        /// </summary>
        static int FindLastSeparator(string path)
        {
            int lastBackslash = path.LastIndexOf('\\');
            int lastForwardSlash = path.LastIndexOf('/');
            return Mathf.Max(lastBackslash, lastForwardSlash);
        }

        /// <summary>
        /// Truncates the given string to the number of characters given by the length parameter.
        /// The value is truncated (if necessary) by removing characters from the middle of the
        /// string and inserting an ellipsis in their place.
        /// </summary>
        static string TruncateMid(string value, int length)
        {
            if (value.Length <= length)
                return value;

            if (length <= 0)
                return string.Empty;

            if (length == 1)
                return ELLIPSIS;

            int mid = (length - 1) / 2;
            string pre = value.Substring(0, Mathf.FloorToInt(mid));
            string post = value.Substring(value.Length - Mathf.CeilToInt(mid));

            return string.Concat(pre, ELLIPSIS, post);
        }

        /// <summary>
        /// String truncation for paths.
        /// This method takes a path and returns it truncated (if necessary) to the exact
        /// number of characters specified by the length parameter.
        /// </summary>
        static string TruncatePathByLength(string path, int length)
        {
            if (path.Length <= length)
                return path;

            if (length <= 0)
                return string.Empty;

            if (length == 1)
                return ELLIPSIS;

            int lastSeparator = FindLastSeparator(path);

            // No directory prefix, fall back to middle ellipsis
            if (lastSeparator == -1)
                return TruncateMid(path, length);

            int filenameLength = path.Length - lastSeparator - 1;

            // File name prefixed with …/ would be too long, fall back to middle ellipsis
            if (filenameLength + 2 > length)
                return TruncateMid(path, length);

            string pre = path.Substring(0, length - filenameLength - 2);
            string post = path.Substring(lastSeparator);

            return string.Concat(pre, ELLIPSIS, post);
        }

        const string ELLIPSIS = "…";
    }
}
