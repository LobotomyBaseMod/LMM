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
using System.ComponentModel;
using System.Linq;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Editing
{
    /// <summary>
    /// Handles selection of text using the mouse.
    /// </summary>
    internal sealed class SelectionMouseHandler //: ITextAreaInputHandler
    {
        #region enum SelectionMode

        private enum SelectionMode
        {
            /// <summary>
            /// no selection (no mouse button down)
            /// </summary>
            None,
            /// <summary>
            /// left mouse button down on selection, might be normal click
            /// or might be drag'n'drop
            /// </summary>
            PossibleDragStart,
            /// <summary>
            /// dragging text
            /// </summary>
            Drag,
            /// <summary>
            /// normal selection (click+drag)
            /// </summary>
            Normal,
            /// <summary>
            /// whole-word selection (double click+drag or ctrl+click+drag)
            /// </summary>
            WholeWord,
            /// <summary>
            /// whole-line selection (triple click+drag)
            /// </summary>
            WholeLine,
            /// <summary>
            /// rectangular selection (alt+click+drag)
            /// </summary>
            Rectangular
        }
        #endregion

        private SelectionMode _mode;
        private AnchorSegment _startWord;
        private Vector2 _possibleDragStartMousePos;
        private int _consecutiveClicks;
        private float _lastClickTime;
        private Vector2 _lastClickPosition;

        #region Constructor + Attach + Detach
        internal SelectionMouseHandler(TextArea textArea)
        {
            TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
        }

        internal TextArea TextArea { get; }

        internal void Attach()
        {
            //TextArea.PointerPressed += TextArea_MouseLeftButtonDown;
            //TextArea.PointerMoved += TextArea_MouseMove;
            //TextArea.PointerReleased += TextArea_MouseLeftButtonUp;
            //textArea.QueryCursor += textArea_QueryCursor;
            TextArea.OptionChanged += TextArea_OptionChanged;

            _enableTextDragDrop = TextArea.Options.EnableTextDragDrop;
            if (_enableTextDragDrop)
            {
                AttachDragDrop();
            }
        }

        internal void Detach()
        {
            _mode = SelectionMode.None;
            //TextArea.PointerPressed -= TextArea_MouseLeftButtonDown;
            //TextArea.PointerMoved -= TextArea_MouseMove;
            //TextArea.PointerReleased -= TextArea_MouseLeftButtonUp;
            //textArea.QueryCursor -= textArea_QueryCursor;
            TextArea.OptionChanged -= TextArea_OptionChanged;
            if (_enableTextDragDrop)
            {
                DetachDragDrop();
            }
        }

        private void AttachDragDrop()
        {
            //textArea.AllowDrop = true;
            //textArea.GiveFeedback += textArea_GiveFeedback;
            //textArea.QueryContinueDrag += textArea_QueryContinueDrag;
            //textArea.DragEnter += textArea_DragEnter;
            //textArea.DragOver += textArea_DragOver;
            //textArea.DragLeave += textArea_DragLeave;
            //textArea.Drop += textArea_Drop;
        }

        private void DetachDragDrop()
        {
            //textArea.AllowDrop = false;
            //textArea.GiveFeedback -= textArea_GiveFeedback;
            //textArea.QueryContinueDrag -= textArea_QueryContinueDrag;
            //textArea.DragEnter -= textArea_DragEnter;
            //textArea.DragOver -= textArea_DragOver;
            //textArea.DragLeave -= textArea_DragLeave;
            //textArea.Drop -= textArea_Drop;
        }

        private bool _enableTextDragDrop;

        private void TextArea_OptionChanged(object sender, PropertyChangedEventArgs e)
        {
            var newEnableTextDragDrop = TextArea.Options.EnableTextDragDrop;
            if (newEnableTextDragDrop != _enableTextDragDrop)
            {
                _enableTextDragDrop = newEnableTextDragDrop;
                if (newEnableTextDragDrop)
                    AttachDragDrop();
                else
                    DetachDragDrop();
            }
        }
        #endregion

        #region Dropping text
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //void textArea_DragEnter(object sender, DragEventArgs e)
        //{
        //	try {
        //		e.Effects = GetEffect(e);
        //		textArea.Caret.Show();
        //	} catch (Exception ex) {
        //		OnDragException(ex);
        //	}
        //}

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //void textArea_DragOver(object sender, DragEventArgs e)
        //{
        //	try {
        //		e.Effects = GetEffect(e);
        //	} catch (Exception ex) {
        //		OnDragException(ex);
        //	}
        //}

        //DragDropEffects GetEffect(DragEventArgs e)
        //{
        //	if (e.Data.GetDataPresent(DataFormats.UnicodeText, true)) {
        //		e.Handled = true;
        //		int visualColumn;
        //		bool isAtEndOfLine;
        //		int offset = GetOffsetFromMousePosition(e.GetPosition(textArea.TextView), out visualColumn, out isAtEndOfLine);
        //		if (offset >= 0) {
        //			textArea.Caret.Position = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
        //			textArea.Caret.DesiredXPos = double.NaN;
        //			if (textArea.ReadOnlySectionProvider.CanInsert(offset)) {
        //				if ((e.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move
        //				    && (e.KeyStates & DragDropKeyStates.ControlKey) != DragDropKeyStates.ControlKey)
        //				{
        //					return DragDropEffects.Move;
        //				} else {
        //					return e.AllowedEffects & DragDropEffects.Copy;
        //				}
        //			}
        //		}
        //	}
        //	return DragDropEffects.None;
        //}

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //void textArea_DragLeave(object sender, DragEventArgs e)
        //{
        //	try {
        //		e.Handled = true;
        //		if (!textArea.IsKeyboardFocusWithin)
        //			textArea.Caret.Hide();
        //	} catch (Exception ex) {
        //		OnDragException(ex);
        //	}
        //}

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //void textArea_Drop(object sender, DragEventArgs e)
        //{
        //	try {
        //		DragDropEffects effect = GetEffect(e);
        //		e.Effects = effect;
        //		if (effect != DragDropEffects.None) {
        //			int start = textArea.Caret.Offset;
        //			if (mode == SelectionMode.Drag && textArea.Selection.Contains(start)) {
        //				Debug.WriteLine("Drop: did not drop: drop target is inside selection");
        //				e.Effects = DragDropEffects.None;
        //			} else {
        //				Debug.WriteLine("Drop: insert at " + start);

        //				var pastingEventArgs = new DataObjectPastingEventArgs(e.Data, true, DataFormats.UnicodeText);
        //				textArea.RaiseEvent(pastingEventArgs);
        //				if (pastingEventArgs.CommandCancelled)
        //					return;

        //				string text = EditingCommandHandler.GetTextToPaste(pastingEventArgs, textArea);
        //				if (text == null)
        //					return;
        //				bool rectangular = pastingEventArgs.DataObject.GetDataPresent(RectangleSelection.RectangularSelectionDataType);

        //				// Mark the undo group with the currentDragDescriptor, if the drag
        //				// is originating from the same control. This allows combining
        //				// the undo groups when text is moved.
        //				textArea.Document.UndoStack.StartUndoGroup(this.currentDragDescriptor);
        //				try {
        //					if (rectangular && RectangleSelection.PerformRectangularPaste(textArea, textArea.Caret.Position, text, true)) {

        //					} else {
        //						textArea.Document.Insert(start, text);
        //						textArea.Selection = Selection.Create(textArea, start, start + text.Length);
        //					}
        //				} finally {
        //					textArea.Document.UndoStack.EndUndoGroup();
        //				}
        //			}
        //			e.Handled = true;
        //		}
        //	} catch (Exception ex) {
        //		OnDragException(ex);
        //	}
        //}

        //void OnDragException(Exception ex)
        //{
        //	// swallows exceptions during drag'n'drop or reports them incorrectly, so
        //	// we re-throw them later to allow the application's unhandled exception handler
        //	// to catch them
        //	textArea.Dispatcher.BeginInvoke(
        //		DispatcherPriority.Send,
        //		new Action(delegate {
        //		           	throw new DragDropException("Exception during drag'n'drop", ex);
        //		           }));
        //}

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //void textArea_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        //{
        //	try {
        //		e.UseDefaultCursors = true;
        //		e.Handled = true;
        //	} catch (Exception ex) {
        //		OnDragException(ex);
        //	}
        //}

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //void textArea_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        //{
        //	try {
        //		if (e.EscapePressed) {
        //			e.Action = DragAction.Cancel;
        //		} else if ((e.KeyStates & DragDropKeyStates.LeftMouseButton) != DragDropKeyStates.LeftMouseButton) {
        //			e.Action = DragAction.Drop;
        //		} else {
        //			e.Action = DragAction.Continue;
        //		}
        //		e.Handled = true;
        //	} catch (Exception ex) {
        //		OnDragException(ex);
        //	}
        //}
        #endregion

        #region Start Drag
        //object currentDragDescriptor;

        //void StartDrag()
        //{
        //	// prevent nested StartDrag calls
        //	mode = SelectionMode.Drag;

        //	// mouse capture and Drag'n'Drop doesn't mix
        //	textArea.ReleaseMouseCapture();

        //	DataObject dataObject = textArea.Selection.CreateDataObject(textArea);

        //	DragDropEffects allowedEffects = DragDropEffects.All;
        //	var deleteOnMove = textArea.Selection.Segments.Select(s => new AnchorSegment(textArea.Document, s)).ToList();
        //	foreach (ISegment s in deleteOnMove) {
        //		ISegment[] result = textArea.GetDeletableSegments(s);
        //		if (result.Length != 1 || result[0].Offset != s.Offset || result[0].EndOffset != s.EndOffset) {
        //			allowedEffects &= ~DragDropEffects.Move;
        //		}
        //	}

        //	var copyingEventArgs = new DataObjectCopyingEventArgs(dataObject, true);
        //	textArea.RaiseEvent(copyingEventArgs);
        //	if (copyingEventArgs.CommandCancelled)
        //		return;

        //	object dragDescriptor = new object();
        //	this.currentDragDescriptor = dragDescriptor;

        //	DragDropEffects resultEffect;
        //	using (textArea.AllowCaretOutsideSelection()) {
        //		var oldCaretPosition = textArea.Caret.Position;
        //		try {
        //			Debug.WriteLine("DoDragDrop with allowedEffects=" + allowedEffects);
        //			resultEffect = DragDrop.DoDragDrop(textArea, dataObject, allowedEffects);
        //			Debug.WriteLine("DoDragDrop done, resultEffect=" + resultEffect);
        //		} catch (COMException ex) {
        //			// ignore COM errors - don't crash on badly implemented drop targets
        //			Debug.WriteLine("DoDragDrop failed: " + ex.ToString());
        //			return;
        //		}
        //		if (resultEffect == DragDropEffects.None) {
        //			// reset caret if drag was aborted
        //			textArea.Caret.Position = oldCaretPosition;
        //		}
        //	}

        //	this.currentDragDescriptor = null;

        //	if (deleteOnMove != null && resultEffect == DragDropEffects.Move && (allowedEffects & DragDropEffects.Move) == DragDropEffects.Move) {
        //		bool draggedInsideSingleDocument = (dragDescriptor == textArea.Document.UndoStack.LastGroupDescriptor);
        //		if (draggedInsideSingleDocument)
        //			textArea.Document.UndoStack.StartContinuedUndoGroup(null);
        //		textArea.Document.BeginUpdate();
        //		try {
        //			foreach (ISegment s in deleteOnMove) {
        //				textArea.Document.Remove(s.Offset, s.Length);
        //			}
        //		} finally {
        //			textArea.Document.EndUpdate();
        //			if (draggedInsideSingleDocument)
        //				textArea.Document.UndoStack.EndUndoGroup();
        //		}
        //	}
        //}
        #endregion

        #region QueryCursor
        // provide the IBeam Cursor for the text area
        //void textArea_QueryCursor(object sender, QueryCursorEventArgs e)
        //{
        //	if (!e.Handled) {
        //		if (mode != SelectionMode.None) {
        //			// during selection, use IBeam cursor even outside the text area
        //			e.Cursor = Cursors.IBeam;
        //			e.Handled = true;
        //		} else if (textArea.TextView.VisualLinesValid) {
        //			// Only query the cursor if the visual lines are valid.
        //			// If they are invalid, the cursor will get re-queried when the visual lines
        //			// get refreshed.
        //			Point p = e.GetPosition(textArea.TextView);
        //			if (p.X >= 0 && p.Y >= 0 && p.X <= textArea.TextView.ActualWidth && p.Y <= textArea.TextView.ActualHeight) {
        //				int visualColumn;
        //				bool isAtEndOfLine;
        //				int offset = GetOffsetFromMousePosition(e, out visualColumn, out isAtEndOfLine);
        //				if (enableTextDragDrop && textArea.Selection.Contains(offset))
        //					e.Cursor = Cursors.Arrow;
        //				else
        //					e.Cursor = Cursors.IBeam;
        //				e.Handled = true;
        //			}
        //		}
        //	}
        //}
        #endregion

        #region LeftButtonDown

        internal void OnPointerPressed(PointerDownEvent e)
        {
            Vector2 mousePoint = GetTextViewRelativePosition(e);

            if (e.button != 0)
            {
                if (TextArea.RightClickMovesCaret == true)
                {
                    SetCaretOffsetToMousePosition(mousePoint);
                }
            }
            else
            {
                int clickCount = ComputeClickCount(e);
                _mode = SelectionMode.None;
                bool isShift = (e.modifiers & EventModifiers.Shift) != 0;
                bool isAlt = (e.modifiers & EventModifiers.Alt) != 0;
                bool isCtrl = (e.modifiers & EventModifiers.Control) != 0
                           || (e.modifiers & EventModifiers.Command) != 0;

                if (_enableTextDragDrop && clickCount == 1 && !isShift)
                {
                    var offset = GetOffsetFromMousePosition(mousePoint, out _, out _);
                    if (TextArea.Selection.Contains(offset))
                    {
                        _mode = SelectionMode.PossibleDragStart;
                        _possibleDragStartMousePos = mousePoint;
                        return;
                    }
                }

                var oldPosition = TextArea.Caret.Position;
                SetCaretOffsetToMousePosition(mousePoint);

                if (!isShift)
                {
                    TextArea.ClearSelection();
                }

                if (isAlt && TextArea.Options.EnableRectangularSelection)
                {
                    _mode = SelectionMode.Rectangular;
                    if (isShift && TextArea.Selection is RectangleSelection)
                    {
                        TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
                    }
                }
                else if (isCtrl && clickCount == 1)
                {
                    _mode = SelectionMode.WholeWord;
                    if (isShift && !(TextArea.Selection is RectangleSelection))
                    {
                        TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
                    }
                }
                else if (e.button == 0 && clickCount == 1)
                {
                    _mode = SelectionMode.Normal;
                    if (isShift && !(TextArea.Selection is RectangleSelection))
                    {
                        TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
                    }
                }
                else
                {
                    SimpleSegment startWord;
                    if (clickCount == 3)
                    {
                        _mode = SelectionMode.WholeLine;
                        startWord = GetLineAtMousePosition(mousePoint);
                    }
                    else
                    {
                        _mode = SelectionMode.WholeWord;
                        startWord = GetWordAtMousePosition(mousePoint);
                    }

                    if (startWord == SimpleSegment.Invalid)
                    {
                        _mode = SelectionMode.None;
                        return;
                    }
                    if (isShift && !TextArea.Selection.IsEmpty)
                    {
                        if (startWord.Offset < TextArea.Selection.SurroundingSegment.Offset)
                        {
                            TextArea.Selection = TextArea.Selection.SetEndpoint(new TextViewPosition(TextArea.Document.GetLocation(startWord.Offset)));
                        }
                        else if (startWord.EndOffset > TextArea.Selection.SurroundingSegment.EndOffset)
                        {
                            TextArea.Selection = TextArea.Selection.SetEndpoint(new TextViewPosition(TextArea.Document.GetLocation(startWord.EndOffset)));
                        }
                        _startWord = new AnchorSegment(TextArea.Document, TextArea.Selection.SurroundingSegment);
                    }
                    else
                    {
                        TextArea.Selection = Selection.Create(TextArea, startWord.Offset, startWord.EndOffset);
                        _startWord = new AnchorSegment(TextArea.Document, startWord.Offset, startWord.Length);
                    }
                }
            }
        }

        private int ComputeClickCount(PointerDownEvent e)
        {
            float currentTime = Time.realtimeSinceStartup;
            Vector2 currentPosition = e.position;

            if (currentTime - _lastClickTime < TripleClickTimeSeconds
                && Vector2.Distance(currentPosition, _lastClickPosition) < TripleClickMaxDistance)
            {
                _consecutiveClicks++;
            }
            else
            {
                _consecutiveClicks = 1;
            }

            _lastClickTime = currentTime;
            _lastClickPosition = currentPosition;

            return _consecutiveClicks;
        }

        #endregion

        #region LeftButtonClick

        #endregion

        #region LeftButtonDoubleTap

        #endregion

        #region Mouse Position <-> Text coordinates

        private Vector2 GetTextViewRelativePosition(IPointerEvent e)
        {
            return TextArea.TextView.WorldToLocal(e.position);
        }

        private SimpleSegment GetWordAtMousePosition(Vector2 posRelativeToTextView)
        {
            var textView = TextArea.TextView;
            if (textView == null) return SimpleSegment.Invalid;
            var pos = posRelativeToTextView;
            if (pos.y < 0)
                pos.y = 0;
            if (pos.y > textView.Bounds.height)
                pos.y = textView.Bounds.height;
            pos += textView.ScrollOffset;
            var line = textView.GetVisualLineFromVisualTop(pos.y);
            if (line != null && line.TextLines != null)
            {
                var visualColumn = line.GetVisualColumn(pos, TextArea.Selection.EnableVirtualSpace);
                var wordStartVc = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, TextArea.Selection.EnableVirtualSpace);
                if (wordStartVc == -1)
                    wordStartVc = 0;
                var wordEndVc = line.GetNextCaretPosition(wordStartVc, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, TextArea.Selection.EnableVirtualSpace);
                if (wordEndVc == -1)
                    wordEndVc = line.VisualLength;
                var relOffset = line.FirstDocumentLine.Offset;
                var wordStartOffset = line.GetRelativeOffset(wordStartVc) + relOffset;
                var wordEndOffset = line.GetRelativeOffset(wordEndVc) + relOffset;
                return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
            }
            else
            {
                return SimpleSegment.Invalid;
            }
        }

        private SimpleSegment GetLineAtMousePosition(Vector2 posRelativeToTextView)
        {
            var textView = TextArea.TextView;
            if (textView == null) return SimpleSegment.Invalid;
            var pos = posRelativeToTextView;
            if (pos.y < 0)
                pos.y = 0;
            if (pos.y > textView.Bounds.height)
                pos.y = textView.Bounds.height;
            pos += textView.ScrollOffset;
            var line = textView.GetVisualLineFromVisualTop(pos.y);
            return line != null && line.TextLines != null
                ? new SimpleSegment(line.StartOffset, line.LastDocumentLine.EndOffset - line.StartOffset)
                : SimpleSegment.Invalid;
        }

        private int GetOffsetFromMousePosition(Vector2 mousePosition, out int visualColumn, out bool isAtEndOfLine)
        {
            visualColumn = 0;
            var textView = TextArea.TextView;
            var pos = mousePosition;
            if (pos.y < 0)
                pos.y = 0;
            if (pos.y > textView.Bounds.height)
                pos.y = textView.Bounds.height;
            pos += textView.ScrollOffset;
            if (pos.y >= textView.DocumentHeight)
                pos.y = textView.DocumentHeight - ExtensionMethods.Epsilon;
            var line = textView.GetVisualLineFromVisualTop(pos.y);
            if (line != null && line.TextLines != null)
            {
                visualColumn = line.GetVisualColumn(pos, TextArea.Selection.EnableVirtualSpace, out isAtEndOfLine);
                return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
            }
            isAtEndOfLine = false;
            return -1;
        }

        private int GetOffsetFromMousePositionFirstTextLineOnly(Vector2 positionRelativeToTextView, out int visualColumn)
        {
            visualColumn = 0;
            var textView = TextArea.TextView;
            var pos = positionRelativeToTextView;
            if (pos.y < 0)
                pos.y = 0;
            if (pos.y > textView.Bounds.height)
                pos.y = textView.Bounds.height;
            pos += textView.ScrollOffset;
            if (pos.y >= textView.DocumentHeight)
                pos.y = textView.DocumentHeight - ExtensionMethods.Epsilon;
            var line = textView.GetVisualLineFromVisualTop(pos.y);
            if (line != null && line.TextLines != null)
            {
                visualColumn = line.GetVisualColumn(line.TextLines.First(), pos.x, TextArea.Selection.EnableVirtualSpace);
                return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
            }
            return -1;
        }
        #endregion

        private const int MinimumHorizontalDragDistance = 2;
        private const int MinimumVerticalDragDistance = 2;
        private const float TripleClickTimeSeconds = 0.5f;
        private const float TripleClickMaxDistance = 5f;

        #region MouseMove

        internal void OnPointerMoved(PointerMoveEvent e)
        {
            Vector2 mousePoint = GetTextViewRelativePosition(e);

            if (_mode == SelectionMode.Normal || _mode == SelectionMode.WholeWord || _mode == SelectionMode.WholeLine || _mode == SelectionMode.Rectangular)
            {
                if (TextArea.TextView.VisualLinesValid)
                {
                    ExtendSelectionToMouse(mousePoint);
                }
            }
            else if (_mode == SelectionMode.PossibleDragStart)
            {
                Vector2 mouseMovement = mousePoint - _possibleDragStartMousePos;
                if (Math.Abs(mouseMovement.x) > MinimumHorizontalDragDistance
                    || Math.Abs(mouseMovement.y) > MinimumVerticalDragDistance)
                {
                    // TODO: drag
                    //StartDrag();
                }
            }
        }
        #endregion

        #region ExtendSelection

        private void SetCaretOffsetToMousePosition(Vector2 posRelativeToTextView)
        {
            int visualColumn;
            bool isAtEndOfLine;
            int offset;
            if (_mode == SelectionMode.Rectangular)
            {
                offset = GetOffsetFromMousePositionFirstTextLineOnly(posRelativeToTextView, out visualColumn);
                isAtEndOfLine = true;
            }
            else
            {
                offset = GetOffsetFromMousePosition(posRelativeToTextView, out visualColumn, out isAtEndOfLine);
            }

            if (offset >= 0)
            {
                TextArea.Caret.Position = new TextViewPosition(TextArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
                TextArea.Caret.DesiredXPos = float.NaN;
            }
        }

        private void ExtendSelectionToMouse(Vector2 posRelativeToTextView)
        {
            var oldPosition = TextArea.Caret.Position;
            if (_mode == SelectionMode.Normal || _mode == SelectionMode.Rectangular)
            {
                SetCaretOffsetToMousePosition(posRelativeToTextView);
                if (_mode == SelectionMode.Normal && TextArea.Selection is RectangleSelection)
                    TextArea.Selection = new SimpleSelection(TextArea, oldPosition, TextArea.Caret.Position);
                else if (_mode == SelectionMode.Rectangular && !(TextArea.Selection is RectangleSelection))
                    TextArea.Selection = new RectangleSelection(TextArea, oldPosition, TextArea.Caret.Position);
                else
                    TextArea.Selection = TextArea.Selection.StartSelectionOrSetEndpoint(oldPosition, TextArea.Caret.Position);
            }
            else if (_mode == SelectionMode.WholeWord || _mode == SelectionMode.WholeLine)
            {
                var newWord = (_mode == SelectionMode.WholeLine) ? GetLineAtMousePosition(posRelativeToTextView) : GetWordAtMousePosition(posRelativeToTextView);
                if (newWord != SimpleSegment.Invalid && _startWord != null)
                {
                    TextArea.Selection = Selection.Create(TextArea,
                                                          Math.Min(newWord.Offset, _startWord.Offset),
                                                          Math.Max(newWord.EndOffset, _startWord.EndOffset));
                    TextArea.Caret.Offset = newWord.Offset < _startWord.Offset ? newWord.Offset : Math.Max(newWord.EndOffset, _startWord.EndOffset);
                }
            }
            TextArea.Caret.BringCaretToView(5.0f);
        }
        #endregion

        #region MouseLeftButtonUp

        internal void OnPointerReleased(PointerUpEvent e)
        {
            if (_mode == SelectionMode.None)
                return;
            Vector2 mousePoint = GetTextViewRelativePosition(e);
            switch (_mode)
            {
                case SelectionMode.PossibleDragStart:
                    SetCaretOffsetToMousePosition(mousePoint);
                    TextArea.ClearSelection();
                    break;
                case SelectionMode.Normal:
                case SelectionMode.WholeWord:
                case SelectionMode.WholeLine:
                case SelectionMode.Rectangular:
                    if (TextArea.Options.ExtendSelectionOnMouseUp)
                        ExtendSelectionToMouse(mousePoint);
                    break;
            }
            _mode = SelectionMode.None;
        }
        #endregion
    }
}
