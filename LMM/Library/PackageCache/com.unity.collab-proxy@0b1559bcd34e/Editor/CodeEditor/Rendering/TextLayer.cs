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
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Rendering
{
    internal sealed class TextLayer : Layer
    {
        internal int Index;

        private readonly List<VisualLineDrawingVisual> _visuals = new List<VisualLineDrawingVisual>();
        private readonly IMGUIContainer _imguiContainer;

        internal TextLayer(TextView textView) : base(textView, KnownLayer.Text)
        {
            _imguiContainer = new IMGUIContainer(OnGUI);
            _imguiContainer.pickingMode = PickingMode.Ignore;
            _imguiContainer.focusable = false;
            _imguiContainer.style.position = Position.Absolute;
            _imguiContainer.style.left = 0;
            _imguiContainer.style.top = 0;
            _imguiContainer.style.right = 0;
            _imguiContainer.style.bottom = 0;
            Add(_imguiContainer);
        }

        internal void SetVisualLines(ICollection<VisualLine> visualLines)
        {
            _visuals.Clear();
            foreach (var newLine in visualLines)
            {
                var visual = newLine.Render();
                _visuals.Add(visual);
            }

            ArrangeVisuals();
            _imguiContainer.MarkDirtyRepaint();
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            foreach (var visual in _visuals)
            {
                float pos = 0;
                foreach (var textLine in visual.VisualLine.TextLines)
                {
                    float x = Mathf.Round(visual.RenderTransform.x);
                    float y = Mathf.Round(pos + visual.RenderTransform.y);

                    var guiStyle = textLine.TextParagraphProperties.GUIStyle;
                    var rect = new Rect(x, y, _imguiContainer.contentRect.width - x, textLine.Height);

                    guiStyle.Draw(rect, textLine.MarkupGUIContent, false, false, false, false);

                    pos += textLine.Height;
                }
            }
        }

        void ArrangeVisuals()
        {
            TextView.ArrangeTextLayer(_visuals);
        }
    }
}
