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

using System.Collections.Generic;
using Unity.CodeEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Editing
{
    internal sealed class SelectionLayer : Layer
    {
        private static readonly Color SelectionColor = TextEditorColors.Selection;
        private readonly TextArea _textArea;
        private readonly List<VisualElement> _selectionElements = new List<VisualElement>();

        internal SelectionLayer(TextArea textArea) : base(textArea.TextView, KnownLayer.Selection)
        {
            _textArea = textArea;
            TextView.VisualLinesChanged += (s, e) => InvalidateSelection();
            TextView.ScrollOffsetChanged += (s, e) => InvalidateSelection();
        }

        internal void InvalidateSelection()
        {
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            if (_textArea.Selection is EmptySelection)
            {
                for (int i = 0; i < _selectionElements.Count; i++)
                    _selectionElements[i].visible = false;
                return;
            }

            int needed = 0;
            foreach (var segment in _textArea.Selection.Segments)
            {
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(
                    TextView, segment, _textArea.Selection.EnableVirtualSpace))
                {
                    needed++;
                }
            }

            while (_selectionElements.Count < needed)
            {
                var el = new VisualElement();
                el.pickingMode = PickingMode.Ignore;
                el.style.position = Position.Absolute;
                el.style.backgroundColor = SelectionColor;
                _selectionElements.Add(el);
                Add(el);
            }

            int index = 0;
            foreach (var segment in _textArea.Selection.Segments)
            {
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(
                    TextView, segment, _textArea.Selection.EnableVirtualSpace))
                {
                    var el = _selectionElements[index];
                    el.style.left = rect.x;
                    el.style.top = rect.y;
                    el.style.width = rect.width;
                    el.style.height = rect.height;
                    el.visible = true;
                    index++;
                }
            }

            for (int i = index; i < _selectionElements.Count; i++)
                _selectionElements[i].visible = false;
        }
    }
}
