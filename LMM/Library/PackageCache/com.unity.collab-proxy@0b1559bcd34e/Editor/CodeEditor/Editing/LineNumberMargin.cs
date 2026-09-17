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
using System.Collections.Generic;
using System.Globalization;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Rendering;
using Unity.CodeEditor.Text;
using Unity.CodeEditor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Editing
{
    /// <summary>
    /// Margin showing line numbers.
    /// </summary>
    internal class LineNumberMargin : AbstractMargin
    {
        private AnchorSegment _selectionStart;
        private bool _selecting;

        internal float LeftHorizontalMargin { get; set; } = 5;
        internal float RightHorizontalMargin { get; set; } = 15;

        private readonly IMGUIContainer _imguiContainer;
        private readonly VisualElement _separator;
        private float _textAreaWidth;

        private readonly List<LineNumberEntry> _lineEntries = new List<LineNumberEntry>();

        private struct LineNumberEntry
        {
            internal GUIContent Content;
            internal float Y;
        }

        internal LineNumberMargin() : base()
        {
            style.position = Position.Relative;
            style.overflow = Overflow.Hidden;

            _imguiContainer = new IMGUIContainer(OnGUI);
            _imguiContainer.pickingMode = PickingMode.Ignore;
            _imguiContainer.focusable = false;
            _imguiContainer.style.position = Position.Absolute;
            _imguiContainer.style.left = 0;
            _imguiContainer.style.top = 0;
            _imguiContainer.style.right = 0;
            _imguiContainer.style.bottom = 0;
            Add(_imguiContainer);

            _separator = new VisualElement();
            _separator.style.position = Position.Absolute;
            _separator.style.right = 7;
            _separator.style.top = 0;
            _separator.style.bottom = 0;
            _separator.style.width = 1;
            Add(_separator);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var textView = TextView;
            if (textView == null)
                return;

            var paragraphProperties = CreateTextParagraphProperties();
            var guiStyle = paragraphProperties.GUIStyle;

            foreach (var entry in _lineEntries)
            {
                var rect = new Rect(0, entry.Y, _textAreaWidth, guiStyle.lineHeight);
                guiStyle.Draw(rect, entry.Content, false, false, false, false);
            }
        }

        private void UpdateLineNumbers()
        {
            var textView = TextView;
            if (textView == null || !textView.VisualLinesValid)
                return;

            UpdateWidth();

            _separator.style.backgroundColor = new StyleColor(textView.LineNumbersSeparatorColor);

            _lineEntries.Clear();
            foreach (var line in textView.VisualLines)
            {
                var lineNumber = line.FirstDocumentLine.LineNumber;
                var y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);

                _lineEntries.Add(new LineNumberEntry
                {
                    Content = new GUIContent(lineNumber.ToString(CultureInfo.CurrentCulture)),
                    Y = Mathf.Round(y - textView.VerticalOffset),
                });
            }

            _imguiContainer.MarkDirtyRepaint();
        }

        private void UpdateWidth()
        {
            var textView = TextView;
            if (textView == null) return;

            TextFormatter textFormatter = new TextFormatter();
            var paragraphProperties = CreateTextParagraphProperties();
            var size = textFormatter.CalcSize(
                new string('9', MaxLineNumberLength),
                paragraphProperties
            );
            _textAreaWidth = size.x + LeftHorizontalMargin;
            style.width = _textAreaWidth + RightHorizontalMargin;
        }

        /// <inheritdoc/>
		protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
            }
            base.OnTextViewChanged(oldTextView, newTextView);
            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
            }
            UpdateLineNumbers();
        }

        /// <inheritdoc/>
        protected override void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
        {
            if (oldDocument != null)
            {
                TextDocumentWeakEventManager.LineCountChanged.RemoveHandler(oldDocument, OnDocumentLineCountChanged);
            }
            base.OnDocumentChanged(oldDocument, newDocument);
            if (newDocument != null)
            {
                TextDocumentWeakEventManager.LineCountChanged.AddHandler(newDocument, OnDocumentLineCountChanged);
            }
            OnDocumentLineCountChanged();
        }

        private void OnDocumentLineCountChanged(object sender, EventArgs e)
        {
            OnDocumentLineCountChanged();
        }

        void TextViewVisualLinesChanged(object sender, EventArgs e)
        {
            UpdateLineNumbers();
        }

        protected int MaxLineNumberLength = 1;

        private void OnDocumentLineCountChanged()
        {
            var documentLineCount = Document?.LineCount ?? 1;
            var newLength = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

            if (newLength < 2)
                newLength = 2;

            if (newLength != MaxLineNumberLength)
            {
                MaxLineNumberLength = newLength;
                UpdateLineNumbers();
            }
        }

        private TextParagraphProperties CreateTextParagraphProperties()
        {
            TextParagraphProperties result = new TextParagraphProperties(
                    false,
                    0,
                    _textView.Font,
                    _textView.FontSize,
                    _textView.LineNumbersForegroundColor);

            result.GUIStyle.alignment = UnityEngine.TextAnchor.MiddleRight;
            return result;
        }
    }
}
