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
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Platform;
using Unity.CodeEditor.Rendering;
using Unity.CodeEditor.Text;
using Unity.CodeEditor.Utils;
using UnityEngine;
using UnityEngine.UIElements;

using LogicalDirection = Unity.CodeEditor.Document.LogicalDirection;

namespace Unity.CodeEditor.Editing
{
    internal enum CaretMovementType
    {
        None,
        CharLeft,
        CharRight,
        Backspace,
        WordLeft,
        WordRight,
        LineUp,
        LineDown,
        PageUp,
        PageDown,
        LineStart,
        LineEnd,
        DocumentStart,
        DocumentEnd
    }

    internal class CaretNavigationCommandHandler
    {
        Dictionary<KeyGesture, CaretNavigationCommand> _navigationCommands = new Dictionary<KeyGesture, CaretNavigationCommand>();

        internal CaretNavigationCommandHandler(TextArea textArea)
        {
            TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));

            var keymap = PlatformHotkeyConfiguration.Current;

            _navigationCommands.Add(new KeyGesture(KeyCode.LeftArrow), CaretNavigationCommand.MoveLeftByCharacter);
            _navigationCommands.Add(new KeyGesture(KeyCode.LeftArrow, keymap.SelectionModifiers), CaretNavigationCommand.SelectLeftByCharacter);
            _navigationCommands.Add(new KeyGesture(KeyCode.LeftArrow, keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectLeftByCharacter);
            _navigationCommands.Add(new KeyGesture(KeyCode.RightArrow), CaretNavigationCommand.MoveRightByCharacter);
            _navigationCommands.Add(new KeyGesture(KeyCode.RightArrow, keymap.SelectionModifiers), CaretNavigationCommand.SelectRightByCharacter);
            _navigationCommands.Add(new KeyGesture(KeyCode.RightArrow, keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectRightByCharacter);

            _navigationCommands.Add(new KeyGesture(KeyCode.LeftArrow, keymap.WholeWordTextActionModifiers), CaretNavigationCommand.MoveLeftByWord);
            _navigationCommands.Add(new KeyGesture(KeyCode.LeftArrow, keymap.WholeWordTextActionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.SelectLeftByWord);
            _navigationCommands.Add(new KeyGesture(KeyCode.LeftArrow, keymap.WholeWordTextActionModifiers | keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectLeftByWord);
            _navigationCommands.Add(new KeyGesture(KeyCode.RightArrow, keymap.WholeWordTextActionModifiers), CaretNavigationCommand.MoveRightByWord);
            _navigationCommands.Add(new KeyGesture(KeyCode.RightArrow, keymap.WholeWordTextActionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.SelectRightByWord);
            _navigationCommands.Add(new KeyGesture(KeyCode.RightArrow, keymap.WholeWordTextActionModifiers | keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectRightByWord);

            _navigationCommands.Add(new KeyGesture(KeyCode.UpArrow), CaretNavigationCommand.MoveUpByLine);
            _navigationCommands.Add(new KeyGesture(KeyCode.UpArrow, keymap.SelectionModifiers), CaretNavigationCommand.SelectUpByLine);
            _navigationCommands.Add(new KeyGesture(KeyCode.UpArrow, keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectUpByLine);
            _navigationCommands.Add(new KeyGesture(KeyCode.DownArrow), CaretNavigationCommand.MoveDownByLine);
            _navigationCommands.Add(new KeyGesture(KeyCode.DownArrow, keymap.SelectionModifiers), CaretNavigationCommand.SelectDownByLine);
            _navigationCommands.Add(new KeyGesture(KeyCode.DownArrow, keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectDownByLine);

            _navigationCommands.Add(new KeyGesture(KeyCode.PageDown), CaretNavigationCommand.MoveDownByPage);
            _navigationCommands.Add(new KeyGesture(KeyCode.PageDown, keymap.SelectionModifiers), CaretNavigationCommand.SelectDownByPage);
            _navigationCommands.Add(new KeyGesture(KeyCode.PageUp), CaretNavigationCommand.MoveUpByPage);
            _navigationCommands.Add(new KeyGesture(KeyCode.PageUp, keymap.SelectionModifiers), CaretNavigationCommand.SelectUpByPage);

            keymap.MoveCursorToTheStartOfLine.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.MoveToLineStart));
            keymap.MoveCursorToTheStartOfLineWithSelection.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.SelectToLineStart));
            keymap.MoveCursorToTheEndOfLine.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.MoveToLineEnd));
            keymap.MoveCursorToTheEndOfLineWithSelection.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.SelectToLineEnd));

            _navigationCommands.Add(new KeyGesture(KeyCode.Home, keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectToLineStart);
            _navigationCommands.Add(new KeyGesture(KeyCode.End, keymap.BoxSelectionModifiers | keymap.SelectionModifiers), CaretNavigationCommand.BoxSelectToLineEnd);

            keymap.MoveCursorToTheStartOfDocument.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.MoveToDocumentStart));
            keymap.MoveCursorToTheStartOfDocumentWithSelection.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.SelectToDocumentStart));
            keymap.MoveCursorToTheEndOfDocument.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.MoveToDocumentEnd));
            keymap.MoveCursorToTheEndOfDocumentWithSelection.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.SelectToDocumentEnd));
            keymap.SelectAll.ForEach(g => _navigationCommands.Add(g, CaretNavigationCommand.SelectAll));
        }

        internal TextArea TextArea { get; }

        internal void OnKeyDown(KeyDownEvent e)
        {
            if (TextArea?.Document == null)
                return;

            CaretNavigationCommand command = GetEditingCommand(e);

            switch (command)
            {
                case CaretNavigationCommand.MoveLeftByCharacter:
                    MoveLeftByCharacter();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectLeftByCharacter:
                    SelectLeftByCharacter();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectLeftByCharacter:
                    BoxSelectLeftByCharacter();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveRightByCharacter:
                    MoveRightByCharacter();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectRightByCharacter:
                    SelectRightByCharacter();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectRightByCharacter:
                    BoxSelectRightByCharacter();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveLeftByWord:
                    MoveLeftByWord();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectLeftByWord:
                    SelectLeftByWord();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectLeftByWord:
                    BoxSelectLeftByWord();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveRightByWord:
                    MoveRightByWord();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectRightByWord:
                    SelectRightByWord();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectRightByWord:
                    BoxSelectRightByWord();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveUpByLine:
                    MoveUpByLine();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectUpByLine:
                    SelectUpByLine();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectUpByLine:
                    BoxSelectUpByLine();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveDownByLine:
                    MoveDownByLine();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectDownByLine:
                    SelectDownByLine();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectDownByLine:
                    BoxSelectDownByLine();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveDownByPage:
                    MoveDownByPage();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectDownByPage:
                    SelectDownByPage();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveUpByPage:
                    MoveUpByPage();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectUpByPage:
                    SelectUpByPage();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveToLineStart:
                    MoveToLineStart();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectToLineStart:
                    SelectToLineStart();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveToLineEnd:
                    MoveToLineEnd();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectToLineEnd:
                    SelectToLineEnd();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectToLineStart:
                    BoxSelectToLineStart();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.BoxSelectToLineEnd:
                    BoxSelectToLineEnd();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveToDocumentStart:
                    MoveToDocumentStart();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectToDocumentStart:
                    SelectToDocumentStart();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.MoveToDocumentEnd:
                    MoveToDocumentEnd();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectToDocumentEnd:
                    SelectToDocumentEnd();
                    e.StopPropagation();
                    break;
                case CaretNavigationCommand.SelectAll:
                    SelectAll();
                    e.StopPropagation();
                    break;
            }
        }

        CaretNavigationCommand GetEditingCommand(KeyDownEvent e)
        {
            KeyGesture keyGesture = new KeyGesture(e.keyCode, e.GetKeyModifiers());

            if (_navigationCommands.TryGetValue(keyGesture, out CaretNavigationCommand command))
                return command;

            return CaretNavigationCommand.None;
        }

        void MoveLeftByCharacter()
        {
            OnMoveCaret(TextArea, CaretMovementType.CharLeft);
        }

        void SelectLeftByCharacter()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.CharLeft);
        }

        void BoxSelectLeftByCharacter()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.CharLeft);
        }

        void MoveRightByCharacter()
        {
            OnMoveCaret(TextArea, CaretMovementType.CharRight);
        }

        void SelectRightByCharacter()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.CharRight);
        }

        void BoxSelectRightByCharacter()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.CharRight);
        }

        void MoveLeftByWord()
        {
            OnMoveCaret(TextArea, CaretMovementType.WordLeft);
        }

        void SelectLeftByWord()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.WordLeft);
        }

        void BoxSelectLeftByWord()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.WordLeft);
        }

        void MoveRightByWord()
        {
            OnMoveCaret(TextArea, CaretMovementType.WordRight);
        }

        void SelectRightByWord()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.WordRight);
        }

        void BoxSelectRightByWord()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.WordRight);
        }

        void MoveUpByLine()
        {
            OnMoveCaret(TextArea, CaretMovementType.LineUp);
        }

        void SelectUpByLine()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.LineUp);
        }

        void BoxSelectUpByLine()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.LineUp);
        }

        void MoveDownByLine()
        {
            OnMoveCaret(TextArea, CaretMovementType.LineDown);
        }

        void SelectDownByLine()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.LineDown);
        }

        void BoxSelectDownByLine()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.LineDown);
        }

        void MoveDownByPage()
        {
            OnMoveCaret(TextArea, CaretMovementType.PageDown);
        }

        void SelectDownByPage()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.PageDown);

        }

        void MoveUpByPage()
        {
            OnMoveCaret(TextArea, CaretMovementType.PageUp);
        }

        void SelectUpByPage()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.PageUp);
        }

        void MoveToLineStart()
        {
            OnMoveCaret(TextArea, CaretMovementType.LineStart);
        }

        void SelectToLineStart()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.LineStart);
        }

        void MoveToLineEnd()
        {
            OnMoveCaret(TextArea, CaretMovementType.LineEnd);
        }

        void SelectToLineEnd()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.LineEnd);
        }

        void BoxSelectToLineStart()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.LineStart);
        }

        void BoxSelectToLineEnd()
        {
            OnMoveCaretBoxSelection(TextArea, CaretMovementType.LineEnd);
        }

        void MoveToDocumentStart()
        {
            OnMoveCaret(TextArea, CaretMovementType.DocumentStart);
        }

        void SelectToDocumentStart()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.DocumentStart);
        }

        void MoveToDocumentEnd()
        {
            OnMoveCaret(TextArea, CaretMovementType.DocumentEnd);
        }

        void SelectToDocumentEnd()
        {
            OnMoveCaretExtendSelection(TextArea, CaretMovementType.DocumentEnd);
        }

        void SelectAll()
        {
            OnSelectAll(TextArea);
        }

        static void OnSelectAll(TextArea textArea)
        {
            textArea.Caret.Offset = textArea.Document.TextLength;
            textArea.Selection = Selection.Create(textArea, 0, textArea.Document.TextLength);
        }

        static void OnMoveCaret(TextArea textArea, CaretMovementType direction)
        {
            textArea.ClearSelection();
            MoveCaret(textArea, direction);
            float borderSize = direction == CaretMovementType.CharRight ||
                               direction == CaretMovementType.WordRight ||
                               direction == CaretMovementType.CharLeft ||
                               direction == CaretMovementType.WordLeft ||
                               direction == CaretMovementType.Backspace
                ? textArea.TextView.WideSpaceWidth * 2 : 0;
            textArea.Caret.BringCaretToView(borderSize);
        }

        static void OnMoveCaretExtendSelection(TextArea textArea, CaretMovementType direction)
        {
            var oldPosition = textArea.Caret.Position;
            MoveCaret(textArea, direction);
            textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
            textArea.Caret.BringCaretToView();
        }

        static void OnMoveCaretBoxSelection(TextArea textArea, CaretMovementType direction)
        {
            // First, convert the selection into a rectangle selection
            // (this is required so that virtual space gets enabled for the caret movement)
            if (textArea.Options.EnableRectangularSelection && !(textArea.Selection is RectangleSelection))
            {
                textArea.Selection = textArea.Selection.IsEmpty
                    ? new RectangleSelection(textArea, textArea.Caret.Position, textArea.Caret.Position)
                    : new RectangleSelection(textArea, textArea.Selection.StartPosition,
                        textArea.Caret.Position);
            }
            // Now move the caret and extend the selection
            var oldPosition = textArea.Caret.Position;
            MoveCaret(textArea, direction);
            textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
            textArea.Caret.BringCaretToView();
        }

        #region Caret movement
        internal static void MoveCaret(TextArea textArea, CaretMovementType direction)
        {
            var desiredXPos = textArea.Caret.DesiredXPos;
            textArea.Caret.Position = GetNewCaretPosition(textArea.TextView, textArea.Caret.Position, direction, textArea.Selection.EnableVirtualSpace, ref desiredXPos);
            textArea.Caret.DesiredXPos = desiredXPos;
        }

        internal static TextViewPosition GetNewCaretPosition(TextView textView, TextViewPosition caretPosition, CaretMovementType direction, bool enableVirtualSpace, ref float desiredXPos)
        {
            switch (direction)
            {
                case CaretMovementType.None:
                    return caretPosition;
                case CaretMovementType.DocumentStart:
                    desiredXPos = float.NaN;
                    return new TextViewPosition(0, 0);
                case CaretMovementType.DocumentEnd:
                    desiredXPos = float.NaN;
                    return new TextViewPosition(textView.Document.GetLocation(textView.Document.TextLength));
            }
            var caretLine = textView.Document.GetLineByNumber(caretPosition.Line);
            var visualLine = textView.GetOrConstructVisualLine(caretLine);
            var textLine = visualLine.GetTextLine(caretPosition.VisualColumn, caretPosition.IsAtEndOfLine);
            switch (direction)
            {
                case CaretMovementType.CharLeft:
                    desiredXPos = float.NaN;
                    // do not move caret to previous line in virtual space
                    if (caretPosition.VisualColumn == 0 && enableVirtualSpace)
                        return caretPosition;
                    return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.Normal, enableVirtualSpace);
                case CaretMovementType.Backspace:
                    desiredXPos = float.NaN;
                    return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.EveryCodepoint, enableVirtualSpace);
                case CaretMovementType.CharRight:
                    desiredXPos = float.NaN;
                    return GetNextCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.Normal, enableVirtualSpace);
                case CaretMovementType.WordLeft:
                    desiredXPos = float.NaN;
                    return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.WordStart, enableVirtualSpace);
                case CaretMovementType.WordRight:
                    desiredXPos = float.NaN;
                    return GetNextCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.WordStart, enableVirtualSpace);
                case CaretMovementType.LineUp:
                case CaretMovementType.LineDown:
                case CaretMovementType.PageUp:
                case CaretMovementType.PageDown:
                    return GetUpDownCaretPosition(textView, caretPosition, direction, visualLine, textLine, enableVirtualSpace, ref desiredXPos);
                case CaretMovementType.LineStart:
                    desiredXPos = float.NaN;
                    return GetStartOfLineCaretPosition(caretPosition.VisualColumn, visualLine, textLine, enableVirtualSpace);
                case CaretMovementType.LineEnd:
                    desiredXPos = float.NaN;
                    return GetEndOfLineCaretPosition(visualLine, textLine);
                default:
                    throw new NotSupportedException(direction.ToString());
            }
        }
        #endregion

        #region Home/End

        static TextViewPosition GetStartOfLineCaretPosition(int oldVisualColumn, VisualLine visualLine, TextLine textLine, bool enableVirtualSpace)
        {
            var newVisualCol = visualLine.GetTextLineVisualStartColumn(textLine);
            if (newVisualCol == 0)
                newVisualCol = visualLine.GetNextCaretPosition(newVisualCol - 1, LogicalDirection.Forward, CaretPositioningMode.WordStart, enableVirtualSpace);
            if (newVisualCol < 0)
                throw ThrowUtil.NoValidCaretPosition();
            // when the caret is already at the start of the text, jump to start before whitespace
            if (newVisualCol == oldVisualColumn)
                newVisualCol = 0;
            return visualLine.GetTextViewPosition(newVisualCol);
        }

        static TextViewPosition GetEndOfLineCaretPosition(VisualLine visualLine, TextLine textLine)
        {
            var newVisualCol = visualLine.GetTextLineVisualStartColumn(textLine) + textLine.Length - textLine.TrailingWhitespaceLength;
            var pos = visualLine.GetTextViewPosition(newVisualCol);
            pos.IsAtEndOfLine = true;
            return pos;
        }
        #endregion

        #region By-character / By-word movement

        static TextViewPosition GetNextCaretPosition(TextView textView, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, bool enableVirtualSpace)
        {
            var pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Forward, mode, enableVirtualSpace);
            if (pos >= 0)
            {
                return visualLine.GetTextViewPosition(pos);
            }
            else
            {
                // move to start of next line
                var nextDocumentLine = visualLine.LastDocumentLine.NextLine;
                if (nextDocumentLine != null)
                {
                    var nextLine = textView.GetOrConstructVisualLine(nextDocumentLine);
                    pos = nextLine.GetNextCaretPosition(-1, LogicalDirection.Forward, mode, enableVirtualSpace);
                    if (pos < 0)
                        throw ThrowUtil.NoValidCaretPosition();
                    return nextLine.GetTextViewPosition(pos);
                }
                else
                {
                    // at end of document
                    System.Diagnostics.Debug.Assert(visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength == textView.Document.TextLength);
                    return new TextViewPosition(textView.Document.GetLocation(textView.Document.TextLength));
                }
            }
        }

        static TextViewPosition GetPrevCaretPosition(TextView textView, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, bool enableVirtualSpace)
        {
            var pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Backward, mode, enableVirtualSpace);
            if (pos >= 0)
            {
                return visualLine.GetTextViewPosition(pos);
            }
            else
            {
                // move to end of previous line
                var previousDocumentLine = visualLine.FirstDocumentLine.PreviousLine;
                if (previousDocumentLine != null)
                {
                    var previousLine = textView.GetOrConstructVisualLine(previousDocumentLine);
                    pos = previousLine.GetNextCaretPosition(previousLine.VisualLength + 1, LogicalDirection.Backward, mode, enableVirtualSpace);
                    if (pos < 0)
                        throw ThrowUtil.NoValidCaretPosition();
                    return previousLine.GetTextViewPosition(pos);
                }
                else
                {
                    // at start of document
                    System.Diagnostics.Debug.Assert(visualLine.FirstDocumentLine.Offset == 0);
                    return new TextViewPosition(0, 0);
                }
            }
        }
        #endregion

        #region Line+Page up/down

        static TextViewPosition GetUpDownCaretPosition(TextView textView, TextViewPosition caretPosition, CaretMovementType direction, VisualLine visualLine, TextLine textLine, bool enableVirtualSpace, ref float xPos)
        {
            // moving up/down happens using the desired visual X position
            if (float.IsNaN(xPos))
                xPos = visualLine.GetTextLineVisualXPosition(textLine, caretPosition.VisualColumn);
            // now find the TextLine+VisualLine where the caret will end up in
            var targetVisualLine = visualLine;
            TextLine targetLine;
            var textLineIndex = visualLine.TextLines.IndexOf(textLine);
            switch (direction)
            {
                case CaretMovementType.LineUp:
                    {
                        // Move up: move to the previous TextLine in the same visual line
                        // or move to the last TextLine of the previous visual line
                        var prevLineNumber = visualLine.FirstDocumentLine.LineNumber - 1;
                        if (textLineIndex > 0)
                        {
                            targetLine = visualLine.TextLines[textLineIndex - 1];
                        }
                        else if (prevLineNumber >= 1)
                        {
                            var prevLine = textView.Document.GetLineByNumber(prevLineNumber);
                            targetVisualLine = textView.GetOrConstructVisualLine(prevLine);
                            targetLine = targetVisualLine.TextLines[targetVisualLine.TextLines.Count - 1];
                        }
                        else
                        {
                            targetLine = null;
                        }
                        break;
                    }
                case CaretMovementType.LineDown:
                    {
                        // Move down: move to the next TextLine in the same visual line
                        // or move to the first TextLine of the next visual line
                        var nextLineNumber = visualLine.LastDocumentLine.LineNumber + 1;
                        if (textLineIndex < visualLine.TextLines.Count - 1)
                        {
                            targetLine = visualLine.TextLines[textLineIndex + 1];
                        }
                        else if (nextLineNumber <= textView.Document.LineCount)
                        {
                            var nextLine = textView.Document.GetLineByNumber(nextLineNumber);
                            targetVisualLine = textView.GetOrConstructVisualLine(nextLine);
                            targetLine = targetVisualLine.TextLines[0];
                        }
                        else
                        {
                            targetLine = null;
                        }
                        break;
                    }
                case CaretMovementType.PageUp:
                case CaretMovementType.PageDown:
                    {
                        // Page up/down: find the target line using its visual position
                        var yPos = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.LineMiddle);
                        if (direction == CaretMovementType.PageUp)
                            yPos -= textView.Bounds.height;
                        else
                            yPos += textView.Bounds.height;
                        var newLine = textView.GetDocumentLineByVisualTop(yPos);
                        targetVisualLine = textView.GetOrConstructVisualLine(newLine);
                        targetLine = targetVisualLine.GetTextLineByVisualYPosition(yPos);
                        break;
                    }
                default:
                    throw new NotSupportedException(direction.ToString());
            }
            if (targetLine != null)
            {
                var yPos = targetVisualLine.GetTextLineVisualYPosition(targetLine, VisualYPosition.LineMiddle);
                var newVisualColumn = targetVisualLine.GetVisualColumn(new Vector2(xPos, yPos), enableVirtualSpace);

                // prevent wrapping to the next line; TODO: could 'IsAtEnd' help here?
                var targetLineStartCol = targetVisualLine.GetTextLineVisualStartColumn(targetLine);
                if (newVisualColumn >= targetLineStartCol + targetLine.Length)
                {
                    if (newVisualColumn <= targetVisualLine.VisualLength)
                        newVisualColumn = targetLineStartCol + targetLine.Length - 1;
                }
                return targetVisualLine.GetTextViewPosition(newVisualColumn);
            }
            else
            {
                return caretPosition;
            }
        }
        #endregion
    }
}