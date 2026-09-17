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
using Unity.CodeEditor.Rendering;
using Unity.CodeEditor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Editing
{
    internal sealed class CaretLayer : Layer
    {
        private readonly TextArea _textArea;
        private readonly VisualElement _caretElement;

        private bool _isVisible;
        private Rect _caretRectangle;

        private IVisualElementScheduledItem _blinkSchedule;
        private bool _blink;

        internal CaretLayer(TextArea textArea) : base(textArea.TextView, KnownLayer.Caret)
        {
            _textArea = textArea;

            _caretElement = new VisualElement();
            _caretElement.pickingMode = PickingMode.Ignore;
            _caretElement.style.position = Position.Absolute;
            _caretElement.visible = false;
            Add(_caretElement);
        }

        private void UpdateCaretVisual()
        {
            if (!_isVisible || !_blink)
            {
                _caretElement.visible = false;
                return;
            }

            var caretBrush = CaretBrush ?? TextView.ForegroundColor;

            var r = new Rect(_caretRectangle.x - TextView.HorizontalOffset,
                              _caretRectangle.y - TextView.VerticalOffset,
                              _caretRectangle.width,
                              _caretRectangle.height);

            r = PixelSnapHelpers.Round(r, PixelSnapHelpers.GetPixelSize(this));

            _caretElement.style.left = r.x;
            _caretElement.style.top = r.y;
            _caretElement.style.width = r.width;
            _caretElement.style.height = r.height;
            _caretElement.style.backgroundColor = caretBrush;
            _caretElement.visible = true;
        }

        internal void Show(Rect caretRectangle)
        {
            _caretRectangle = caretRectangle;
            _isVisible = true;
            StartBlinkAnimation();
            UpdateCaretVisual();
        }

        internal void Hide()
        {
            if (_isVisible)
            {
                _isVisible = false;
                StopBlinkAnimation();
                UpdateCaretVisual();
            }
        }

        private void StartBlinkAnimation()
        {
            _blink = true;
            StopBlinkAnimation();
            _blinkSchedule = schedule.Execute(() =>
            {
                _blink = !_blink;
                UpdateCaretVisual();
            }).StartingIn(500).Every(500);
        }

        private void StopBlinkAnimation()
        {
            _blinkSchedule?.Pause();
            _blinkSchedule = null;
        }

        internal Color? CaretBrush;
    }
}
