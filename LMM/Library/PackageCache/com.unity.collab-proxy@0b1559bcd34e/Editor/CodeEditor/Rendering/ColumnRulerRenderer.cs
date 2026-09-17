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
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Rendering
{
    internal sealed class ColumnRulerRenderer : IBackgroundRenderer
    {
        private Color _color;
        private IEnumerable<int> _columns;
        private readonly TextView _textView;
        private readonly List<VisualElement> _rulerElements = new List<VisualElement>();

        static readonly float DefaultThickness = 1f;

        internal ColumnRulerRenderer(TextView textView)
        {
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));
            _textView.BackgroundRenderers.Add(this);
            _textView.VisualLinesChanged += OnVisualLinesChanged;
        }

        public KnownLayer Layer => KnownLayer.Background;

        internal void SetRuler(IEnumerable<int> columns, Color color)
        {
            _columns = columns;
            _color = color;
            UpdateRulerVisuals();
        }

        public void Draw(TextView textView)
        {
        }

        private void OnVisualLinesChanged(object sender, EventArgs e)
        {
            UpdateRulerVisuals();
        }

        private void UpdateRulerVisuals()
        {
            if (_columns == null)
            {
                for (int i = 0; i < _rulerElements.Count; i++)
                    _rulerElements[i].visible = false;
                return;
            }

            int needed = 0;
            foreach (var col in _columns)
                needed++;

            while (_rulerElements.Count < needed)
            {
                var el = new VisualElement();
                el.pickingMode = PickingMode.Ignore;
                el.style.position = Position.Absolute;
                el.style.top = 0;
                el.style.bottom = 0;
                el.style.width = DefaultThickness;
                _rulerElements.Add(el);
                _textView.Add(el);
            }

            int index = 0;
            foreach (var column in _columns)
            {
                var el = _rulerElements[index];
                float xPos = _textView.WideSpaceWidth * column - _textView.HorizontalOffset;
                el.transform.position = new Vector3(xPos, 0, 0);
                el.style.backgroundColor = _color;
                el.visible = true;
                index++;
            }

            for (int i = index; i < _rulerElements.Count; i++)
                _rulerElements[i].visible = false;
        }
    }
}
