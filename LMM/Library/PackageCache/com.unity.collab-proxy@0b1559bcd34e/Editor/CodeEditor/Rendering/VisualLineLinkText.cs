// TO COMPLETE

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

namespace Unity.CodeEditor.Rendering
{
    /// <summary>
    /// VisualLineElement that represents a piece of text and is a clickable link.
    /// </summary>
    internal class VisualLineLinkText : VisualLineText
    {
        /// <summary>
        /// Gets/Sets the URL that is navigated to when the link is clicked.
        /// </summary>
        internal Uri NavigateUri { get; set; }

        /// <summary>
        /// Gets/Sets the window name where the URL will be opened.
        /// </summary>
        internal string TargetName { get; set; }

        /// <summary>
        /// Gets/Sets whether the user needs to press Control to click the link.
        /// The default value is true.
        /// </summary>
        internal bool RequireControlModifierForClick { get; set; }

        /// <summary>
        /// Creates a visual line text element with the specified length.
        /// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
        /// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
        /// </summary>
        internal VisualLineLinkText(VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
        {
            RequireControlModifierForClick = true;
        }
    }
}