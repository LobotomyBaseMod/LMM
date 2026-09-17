// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Drawing;
using UnityEngine;

namespace Unity.CodeEditor.Utils
{
	/// <summary>
	/// Contains static helper methods for aligning stuff on a whole number of pixels.
	/// </summary>
	internal static class PixelSnapHelpers
	{
		/// <summary>
		/// Gets the pixel size on the screen containing visual.
		/// This method does not take transforms on visual into account.
		/// </summary>
		internal static Vector2 GetPixelSize(object visual)
		{
			if (visual == null)
				throw new ArgumentNullException(nameof(visual));

            // TODO-avedit
            //PresentationSource source = PresentationSource.FromVisual(visual);
            //if (source != null) {
            //	Matrix matrix = source.CompositionTarget.TransformFromDevice;
            //	return new Size(matrix.M11, matrix.M22);
            //} else {
            return new Vector2(1, 1);
            //}
        }
		
		/// <summary>
		/// Aligns <paramref name="value"/> on the next middle of a pixel.
		/// </summary>
		/// <param name="value">The value that should be aligned</param>
		/// <param name="pixelSize">The size of one pixel</param>
		internal static float PixelAlign(float value, float pixelSize)
		{
			// 0 -> 0.5
			// 0.1 -> 0.5
			// 0.5 -> 0.5
			// 0.9 -> 0.5
			// 1 -> 1.5
			return pixelSize * ((float)Math.Round((value / pixelSize) + 0.5f, MidpointRounding.AwayFromZero) - 0.5f);
		}
		
		/// <summary>
		/// Aligns the borders of rect on the middles of pixels.
		/// </summary>
		internal static Rect PixelAlign(Rect rect, Vector2 pixelSize)
		{
			var x = PixelAlign(rect.x, pixelSize.x);
			var y = PixelAlign(rect.y, pixelSize.y);
			var width = Round(rect.width, pixelSize.x);
			var height = Round(rect.height, pixelSize.y);
			return new Rect(x, y, width, height);
		}
		
		/// <summary>
		/// Rounds <paramref name="point"/> to whole number of pixels.
		/// </summary>
		internal static Point Round(Point point, Vector2 pixelSize)
		{
			return new Point((int)Round(point.X, pixelSize.x), (int)Round(point.Y, pixelSize.y));
		}
		
		/// <summary>
		/// Rounds val to whole number of pixels.
		/// </summary>
		internal static Rect Round(Rect rect, Vector2 pixelSize)
		{
			return new Rect((float)Round(rect.x, pixelSize.x), (float)Round(rect.y, pixelSize.y),
			                (float)Round(rect.width, pixelSize.x), (float)Round(rect.height, pixelSize.y));
		}
		
		/// <summary>
		/// Rounds <paramref name="value"/> to a whole number of pixels.
		/// </summary>
		internal static float Round(double value, double pixelSize)
		{
			return (float)(pixelSize * Math.Round(value / pixelSize, MidpointRounding.AwayFromZero));
		}
		
		/// <summary>
		/// Rounds <paramref name="value"/> to an whole odd number of pixels.
		/// </summary>
		internal static double RoundToOdd(float value, float pixelSize)
		{
			return Round(value - pixelSize, pixelSize * 2) + pixelSize;
		}
	}
}