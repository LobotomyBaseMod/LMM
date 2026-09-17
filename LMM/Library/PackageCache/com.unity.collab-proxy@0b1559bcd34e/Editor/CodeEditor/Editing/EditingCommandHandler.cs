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
using System.Linq;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Platform;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Editing
{
    /// <summary>
    /// We re-use the CommandBinding and InputBinding instances between multiple text areas,
    /// so this class is static.
    /// </summary>
    internal class EditingCommandHandler
    {
        Dictionary<KeyGesture, EditingCommand> _editingCommands = new Dictionary<KeyGesture, EditingCommand>();

        internal EditingCommandHandler(TextArea textArea)
        {
            TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));

            var keymap = PlatformHotkeyConfiguration.Current;

            _editingCommands.Add(new KeyGesture(KeyCode.Delete), EditingCommand.Delete);
            _editingCommands.Add(new KeyGesture(KeyCode.Delete, KeyModifiers.Control), EditingCommand.DeleteNextWord);
            _editingCommands.Add(new KeyGesture(KeyCode.Backspace), EditingCommand.Backspace);
            _editingCommands.Add(new KeyGesture(KeyCode.Backspace, KeyModifiers.Shift), EditingCommand.Backspace); // make Shift-Backspace do the same as plain backspace
            _editingCommands.Add(new KeyGesture(KeyCode.Backspace, KeyModifiers.Control), EditingCommand.DeletePreviousWord);
            _editingCommands.Add(new KeyGesture(KeyCode.KeypadEnter), EditingCommand.EnterParagraphBreak);
            _editingCommands.Add(new KeyGesture(KeyCode.Return), EditingCommand.EnterParagraphBreak);
            _editingCommands.Add(new KeyGesture(KeyCode.Return, KeyModifiers.Shift), EditingCommand.EnterLineBreak);
            _editingCommands.Add(new KeyGesture(KeyCode.Tab), EditingCommand.TabForward);
            _editingCommands.Add(new KeyGesture(KeyCode.Tab, KeyModifiers.Shift), EditingCommand.TabBackward);

            keymap.Copy.ForEach(g => _editingCommands.Add(g, EditingCommand.Copy));
            keymap.Cut.ForEach(g => _editingCommands.Add(g, EditingCommand.Cut));
            keymap.Paste.ForEach(g => _editingCommands.Add(g, EditingCommand.Paste));

            _editingCommands.Add(new KeyGesture(KeyCode.Insert), EditingCommand.ToggleOverstrike);
            keymap.DeleteWholeLine.ForEach(g => _editingCommands.Add(g, EditingCommand.DeleteLine));
            _editingCommands.Add(new KeyGesture(KeyCode.I, keymap.CommandModifiers), EditingCommand.IndentSelection);

            keymap.Undo.ForEach(g => _editingCommands.Add(g, EditingCommand.Undo));
            keymap.Redo.ForEach(g => _editingCommands.Add(g, EditingCommand.Redo));
        }

        internal TextArea TextArea { get; }

        internal void OnKeyDown(KeyDownEvent e)
        {
            if (TextArea?.Document == null)
                return;

            EditingCommand command = GetEditingCommand(e);

            switch (command)
            {
                case EditingCommand.Delete:
                    Delete();
                    e.StopPropagation();
                    break;
                case EditingCommand.Copy:
                    Copy();
                    e.StopPropagation();
                    break;
                case EditingCommand.Cut:
                    Cut();
                    e.StopPropagation();
                    break;
                case EditingCommand.Paste:
                    Paste();
                    e.StopPropagation();
                    break;
                case EditingCommand.DeleteNextWord:
                    DeleteNextWord();
                    e.StopPropagation();
                    break;
                case EditingCommand.Backspace:
                    Backspace();
                    e.StopPropagation();
                    break;
                case EditingCommand.DeletePreviousWord:
                    DeletePreviousWord();
                    e.StopPropagation();
                    break;
                case EditingCommand.EnterParagraphBreak:
                    EnterParagraphBreak();
                    e.StopPropagation();
                    break;
                case EditingCommand.EnterLineBreak:
                    EnterLineBreak();
                    e.StopPropagation();
                    break;
                case EditingCommand.TabForward:
                    TabForward();
                    e.StopPropagation();
                    break;
                case EditingCommand.TabBackward:
                    TabBackward();
                    e.StopPropagation();
                    break;
                case EditingCommand.ToggleOverstrike:
                    ToggleOverstrike();
                    e.StopPropagation();
                    break;
                case EditingCommand.DeleteLine:
                    DeleteLine();
                    e.StopPropagation();
                    break;
                case EditingCommand.IndentSelection:
                    IndentSelection();
                    e.StopPropagation();
                    break;
                case EditingCommand.Undo:
                    ExecuteUndo();
                    e.StopPropagation();
                    break;
                case EditingCommand.Redo:
                    ExecuteRedo();
                    e.StopPropagation();
                    break;
            }
        }

        EditingCommand GetEditingCommand(KeyDownEvent e)
        {
            KeyGesture keyGesture = new KeyGesture(e.keyCode, e.GetKeyModifiers());

            if (_editingCommands.TryGetValue(keyGesture, out EditingCommand command))
                return command;

            return EditingCommand.None;
        }

        void Paste()
        {
            OnPaste(TextArea);
        }

        void Cut()
        {
            OnCut(TextArea);
        }

        void IndentSelection()
        {
            OnIndentSelection(TextArea);
        }

        void DeleteLine()
        {
            OnDeleteLine(TextArea);
        }

        void ToggleOverstrike()
        {
            OnToggleOverstrike(TextArea);
        }

        void TabBackward()
        {
            OnShiftTab(TextArea);
        }

        void TabForward()
        {
            OnTab(TextArea);
        }

        void EnterLineBreak()
        {
            OnEnter(TextArea);
        }

        void EnterParagraphBreak()
        {
            OnEnter(TextArea);
        }

        void DeletePreviousWord()
        {
            OnDelete(TextArea, CaretMovementType.WordLeft);
        }

        void Backspace()
        {
            OnDelete(TextArea, CaretMovementType.Backspace);
        }

        void Copy()
        {
            OnCopy(TextArea);
        }

        void Delete()
        {
            OnDelete(TextArea, CaretMovementType.CharRight);
        }

        void DeleteNextWord()
        {
            OnDelete(TextArea, CaretMovementType.WordRight);
        }

        #region Text Transformation Helpers

        private enum DefaultSegmentType
        {
            WholeDocument,
            CurrentLine
        }

        /// <summary>
        /// Calls transformLine on all lines in the selected range.
        /// transformLine needs to handle read-only segments!
        /// </summary>
        private static void TransformSelectedLines(Action<TextArea, DocumentLine> transformLine, TextArea textArea,
            DefaultSegmentType defaultSegmentType)
        {
            if (textArea?.Document != null)
            {
                using (textArea.Document.RunUpdate())
                {
                    DocumentLine start, end;
                    if (textArea.Selection.IsEmpty)
                    {
                        if (defaultSegmentType == DefaultSegmentType.CurrentLine)
                        {
                            start = end = textArea.Document.GetLineByNumber(textArea.Caret.Line);
                        }
                        else if (defaultSegmentType == DefaultSegmentType.WholeDocument)
                        {
                            start = textArea.Document.Lines.First();
                            end = textArea.Document.Lines.Last();
                        }
                        else
                        {
                            start = end = null;
                        }
                    }
                    else
                    {
                        var segment = textArea.Selection.SurroundingSegment;
                        start = textArea.Document.GetLineByOffset(segment.Offset);
                        end = textArea.Document.GetLineByOffset(segment.EndOffset);
                        // don't include the last line if no characters on it are selected
                        if (start != end && end.Offset == segment.EndOffset)
                            end = end.PreviousLine;
                    }
                    if (start != null)
                    {
                        transformLine(textArea, start);
                        while (start != end)
                        {
                            start = start.NextLine;
                            transformLine(textArea, start);
                        }
                    }
                }
                textArea.Caret.BringCaretToView();
            }
        }

        /// <summary>
        /// Calls transformLine on all writable segment in the selected range.
        /// </summary>
        private static void TransformSelectedSegments(Action<TextArea, ISegment> transformSegment, TextArea textArea,
            DefaultSegmentType defaultSegmentType)
        {
            if (textArea?.Document != null)
            {
                using (textArea.Document.RunUpdate())
                {
                    IEnumerable<ISegment> segments;
                    if (textArea.Selection.IsEmpty)
                    {
                        if (defaultSegmentType == DefaultSegmentType.CurrentLine)
                        {
                            segments = new ISegment[] { textArea.Document.GetLineByNumber(textArea.Caret.Line) };
                        }
                        else if (defaultSegmentType == DefaultSegmentType.WholeDocument)
                        {
                            segments = textArea.Document.Lines;
                        }
                        else
                        {
                            segments = null;
                        }
                    }
                    else
                    {
                        segments = textArea.Selection.Segments;
                    }
                    if (segments != null)
                    {
                        foreach (var segment in segments.Reverse())
                        {
                            foreach (var writableSegment in textArea.GetDeletableSegments(segment).Reverse())
                            {
                                transformSegment(textArea, writableSegment);
                            }
                        }
                    }
                }
                textArea.Caret.BringCaretToView();
            }
        }

        #endregion

        #region EnterLineBreak

        private static void OnEnter(TextArea textArea)
        {
            if (textArea != null && textArea.IsFocused)
            {
                textArea.PerformTextInput("\n");
            }
        }

        #endregion

        #region Tab

        private static void OnTab(TextArea textArea)
        {
            if (textArea?.Document != null)
            {
                using (textArea.Document.RunUpdate())
                {
                    if (textArea.Selection.IsMultiline)
                    {
                        var segment = textArea.Selection.SurroundingSegment;
                        var start = textArea.Document.GetLineByOffset(segment.Offset);
                        var end = textArea.Document.GetLineByOffset(segment.EndOffset);
                        // don't include the last line if no characters on it are selected
                        if (start != end && end.Offset == segment.EndOffset)
                            end = end.PreviousLine;
                        var current = start;
                        while (true)
                        {
                            var offset = current.Offset;
                            if (textArea.ReadOnlySectionProvider.CanInsert(offset))
                                textArea.Document.Replace(offset, 0, textArea.Options.IndentationString,
                                    OffsetChangeMappingType.KeepAnchorBeforeInsertion);
                            if (current == end)
                                break;
                            current = current.NextLine;
                        }
                    }
                    else
                    {
                        var indentationString = textArea.Options.GetIndentationString(textArea.Caret.Column);
                        textArea.ReplaceSelectionWithText(indentationString);
                    }
                }
                textArea.Caret.BringCaretToView();
            }
        }

        private static void OnShiftTab(TextArea textArea)
        {
            TransformSelectedLines(
                delegate (TextArea textArea, DocumentLine line)
                {
                    var offset = line.Offset;
                    var s = TextUtilities.GetSingleIndentationSegment(textArea.Document, offset,
                        textArea.Options.IndentationSize);
                    if (s.Length > 0)
                    {
                        s = textArea.GetDeletableSegments(s).FirstOrDefault();
                        if (s != null && s.Length > 0)
                        {
                            textArea.Document.Remove(s.Offset, s.Length);
                        }
                    }
                }, textArea, DefaultSegmentType.CurrentLine);
        }

        #endregion

        #region Delete

        static void OnDelete(TextArea textArea, CaretMovementType caretMovement)
        {
            if (textArea?.Document != null)
            {
                if (textArea.Selection.IsEmpty)
                {
                    var startPos = textArea.Caret.Position;
                    var enableVirtualSpace = textArea.Options.EnableVirtualSpace;
                    // When pressing delete; don't move the caret further into virtual space - instead delete the newline
                    if (caretMovement == CaretMovementType.CharRight)
                        enableVirtualSpace = false;
                    var desiredXPos = textArea.Caret.DesiredXPos;
                    var endPos = CaretNavigationCommandHandler.GetNewCaretPosition(
                        textArea.TextView, startPos, caretMovement, enableVirtualSpace, ref desiredXPos);
                    // GetNewCaretPosition may return (0,0) as new position,
                    // thus we need to validate endPos before using it in the selection.
                    if (endPos.Line < 1 || endPos.Column < 1)
                        endPos = new TextViewPosition(Math.Max(endPos.Line, 1), Math.Max(endPos.Column, 1));
                    // Don't do anything if the number of lines of a rectangular selection would be changed by the deletion.
                    if (textArea.Selection is RectangleSelection && startPos.Line != endPos.Line)
                        return;
                    // Don't select the text to be deleted; just reuse the ReplaceSelectionWithText logic
                    // Reuse the existing selection, so that we continue using the same logic
                    textArea.Selection.StartSelectionOrSetEndpoint(startPos, endPos)
                        .ReplaceSelectionWithText(string.Empty);
                }
                else
                {
                    textArea.RemoveSelectedText();
                }
                textArea.Caret.BringCaretToView();
            }
        }

        private static bool CanDelete(TextArea textArea)
        {
            if (textArea?.Document != null)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Clipboard commands

        private static bool CanCut(TextArea textArea)
        {
            // HasSomethingSelected for copy and cut commands
            if (textArea?.Document != null)
            {
                return (textArea.Options.CutCopyWholeLine || !textArea.Selection.IsEmpty) && !textArea.IsReadOnly;
            }

            return false;
        }

        private static bool CanCopy(TextArea textArea)
        {
            // HasSomethingSelected for copy and cut commands
            if (textArea?.Document != null)
            {
                return textArea.Options.CutCopyWholeLine || !textArea.Selection.IsEmpty;
            }

            return false;
        }

        private static void OnCopy(TextArea textArea)
        {
            if (textArea?.Document != null)
            {
                if (textArea.Selection.IsEmpty && textArea.Options.CutCopyWholeLine)
                {
                    var currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
                    CopyWholeLine(textArea, currentLine);
                }
                else
                {
                    CopySelectedText(textArea);
                }
            }
        }

        private static void OnCut(TextArea textArea)
        {
            if (textArea?.Document != null)
            {
                if (textArea.Selection.IsEmpty && textArea.Options.CutCopyWholeLine)
                {
                    var currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
                    if (CopyWholeLine(textArea, currentLine))
                    {
                        var segmentsToDelete =
                            textArea.GetDeletableSegments(
                                new SimpleSegment(currentLine.Offset, currentLine.TotalLength));
                        for (var i = segmentsToDelete.Length - 1; i >= 0; i--)
                        {
                            textArea.Document.Remove(segmentsToDelete[i]);
                        }
                    }
                }
                else
                {
                    if (CopySelectedText(textArea))
                        textArea.RemoveSelectedText();
                }
                textArea.Caret.BringCaretToView();
            }
        }

        private static bool CopySelectedText(TextArea textArea)
        {
            var text = textArea.Selection.GetText();
            text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);

            SetClipboardText(text);

            textArea.OnTextCopied(new TextArea.TextEventArgs(text));
            return true;
        }

        private static void SetClipboardText(string text)
        {
            try
            {
                GUIUtility.systemCopyBuffer = text;
            }
            catch (Exception)
            {
                // Apparently this exception sometimes happens randomly.
                // The MS controls just ignore it, so we'll do the same.
            }
        }

        private static bool CopyWholeLine(TextArea textArea, DocumentLine line)
        {
            ISegment wholeLine = new SimpleSegment(line.Offset, line.TotalLength);
            var text = textArea.Document.GetText(wholeLine);
            // Ignore empty line copy
            if(string.IsNullOrEmpty(text)) return false;
            // Ensure we use the appropriate newline sequence for the OS
            text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);

            // TODO: formats
            //DataObject data = new DataObject();
            //if (ConfirmDataFormat(textArea, data, DataFormats.UnicodeText))
            //    data.SetText(text);

            //// Also copy text in HTML format to clipboard - good for pasting text into Word
            //// or to the SharpDevelop forums.
            //if (ConfirmDataFormat(textArea, data, DataFormats.Html))
            //{
            //    IHighlighter highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
            //    HtmlClipboard.SetHtml(data,
            //        HtmlClipboard.CreateHtmlFragment(textArea.Document, highlighter, wholeLine,
            //            new HtmlOptions(textArea.Options)));
            //}

            //if (ConfirmDataFormat(textArea, data, LineSelectedType))
            //{
            //    var lineSelected = new MemoryStream(1);
            //    lineSelected.WriteByte(1);
            //    data.SetData(LineSelectedType, lineSelected, false);
            //}

            //var copyingEventArgs = new DataObjectCopyingEventArgs(data, false);
            //textArea.RaiseEvent(copyingEventArgs);
            //if (copyingEventArgs.CommandCancelled)
            //    return false;

            SetClipboardText(text);

            textArea.OnTextCopied(new TextArea.TextEventArgs(text));
            return true;
        }

        private static bool CanPaste(TextArea textArea)
        {
            if (textArea?.Document != null)
            {
                return textArea.ReadOnlySectionProvider.CanInsert(textArea.Caret.Offset);
            }

            return false;
        }

        private static void OnPaste(TextArea textArea)
        {
            if (textArea?.Document != null)
            {
                textArea.Document.BeginUpdate();

                string text = null;
                try
                {
                    text = GUIUtility.systemCopyBuffer;
                }
                catch (Exception)
                {
                    textArea.Document.EndUpdate();
                    return;
                }

                if (text == null)
                {
                    textArea.Document.EndUpdate();
                    return;
                }


                text = GetTextToPaste(text, textArea);

                if (!string.IsNullOrEmpty(text))
                {
                    textArea.ReplaceSelectionWithText(text);
                }

                textArea.Caret.BringCaretToView();

                textArea.Document.EndUpdate();
            }
        }

        internal static string GetTextToPaste(string text, TextArea textArea)
        {
            try
            {
                // Try retrieving the text as one of:
                //  - the FormatToApply
                //  - UnicodeText
                //  - Text
                // (but don't try the same format twice)
                //if (pastingEventArgs.FormatToApply != null && dataObject.GetDataPresent(pastingEventArgs.FormatToApply))
                //    text = (string)dataObject.GetData(pastingEventArgs.FormatToApply);
                //else if (pastingEventArgs.FormatToApply != DataFormats.UnicodeText &&
                //         dataObject.GetDataPresent(DataFormats.UnicodeText))
                //    text = (string)dataObject.GetData(DataFormats.UnicodeText);
                //else if (pastingEventArgs.FormatToApply != DataFormats.Text &&
                //         dataObject.GetDataPresent(DataFormats.Text))
                //    text = (string)dataObject.GetData(DataFormats.Text);
                //else
                //    return null; // no text data format
                // convert text back to correct newlines for this document
                var newLine = TextUtilities.GetNewLineFromDocument(textArea.Document, textArea.Caret.Line);
                text = TextUtilities.NormalizeNewLines(text, newLine);
                text = textArea.Options.ConvertTabsToSpaces
                    ? text.Replace("\t", new String(' ', textArea.Options.IndentationSize))
                    : text;
                return text;
            }
            catch (OutOfMemoryException)
            {
                // may happen when trying to paste a huge string
                return null;
            }
        }

        #endregion

        #region Toggle Overstrike

        private static void OnToggleOverstrike(TextArea textArea)
        {
            if (textArea != null && textArea.Options.AllowToggleOverstrikeMode)
            {
                //textArea.OverstrikeMode = !textArea.OverstrikeMode;
            }
        }

        #endregion

        #region DeleteLine

        private static void OnDeleteLine(TextArea textArea)
        {
            if (textArea?.Document != null)
            {
                int firstLineIndex, lastLineIndex;
                if (textArea.Selection.Length == 0)
                {
                    // There is no selection, simply delete current line
                    firstLineIndex = lastLineIndex = textArea.Caret.Line;
                }
                else
                {
                    // There is a selection, remove all lines affected by it (use Min/Max to be independent from selection direction)
                    firstLineIndex = Math.Min(textArea.Selection.StartPosition.Line,
                        textArea.Selection.EndPosition.Line);
                    lastLineIndex = Math.Max(textArea.Selection.StartPosition.Line,
                        textArea.Selection.EndPosition.Line);
                }
                var startLine = textArea.Document.GetLineByNumber(firstLineIndex);
                var endLine = textArea.Document.GetLineByNumber(lastLineIndex);
                textArea.Selection = Selection.Create(textArea, startLine.Offset,
                    endLine.Offset + endLine.TotalLength);
                textArea.RemoveSelectedText();
            }
        }

        #endregion

        #region Remove..Whitespace / Convert Tabs-Spaces

        private static void OnRemoveLeadingWhitespace(TextArea textArea)
        {
            TransformSelectedLines(
                delegate (TextArea textArea, DocumentLine line)
                {
                    textArea.Document.Remove(TextUtilities.GetLeadingWhitespace(textArea.Document, line));
                }, textArea, DefaultSegmentType.WholeDocument);
        }

        private static void OnRemoveTrailingWhitespace(TextArea textArea)
        {
            TransformSelectedLines(
                delegate (TextArea textArea, DocumentLine line)
                {
                    textArea.Document.Remove(TextUtilities.GetTrailingWhitespace(textArea.Document, line));
                }, textArea, DefaultSegmentType.WholeDocument);
        }

        private static void OnConvertTabsToSpaces(TextArea textArea)
        {
            TransformSelectedSegments(ConvertTabsToSpaces, textArea, DefaultSegmentType.WholeDocument);
        }

        private static void OnConvertLeadingTabsToSpaces(TextArea textArea)
        {
            TransformSelectedLines(
                delegate (TextArea textArea, DocumentLine line)
                {
                    ConvertTabsToSpaces(textArea, TextUtilities.GetLeadingWhitespace(textArea.Document, line));
                }, textArea, DefaultSegmentType.WholeDocument);
        }

        private static void ConvertTabsToSpaces(TextArea textArea, ISegment segment)
        {
            var document = textArea.Document;
            var endOffset = segment.EndOffset;
            var indentationString = new string(' ', textArea.Options.IndentationSize);
            for (var offset = segment.Offset; offset < endOffset; offset++)
            {
                if (document.GetCharAt(offset) == '\t')
                {
                    document.Replace(offset, 1, indentationString, OffsetChangeMappingType.CharacterReplace);
                    endOffset += indentationString.Length - 1;
                }
            }
        }

        private static void OnConvertSpacesToTabs(TextArea textArea)
        {
            TransformSelectedSegments(ConvertSpacesToTabs, textArea, DefaultSegmentType.WholeDocument);
        }

        private static void OnConvertLeadingSpacesToTabs(TextArea textArea)
        {
            TransformSelectedLines(
                delegate (TextArea textArea, DocumentLine line)
                {
                    ConvertSpacesToTabs(textArea, TextUtilities.GetLeadingWhitespace(textArea.Document, line));
                }, textArea, DefaultSegmentType.WholeDocument);
        }

        private static void ConvertSpacesToTabs(TextArea textArea, ISegment segment)
        {
            var document = textArea.Document;
            var endOffset = segment.EndOffset;
            var indentationSize = textArea.Options.IndentationSize;
            var spacesCount = 0;
            for (var offset = segment.Offset; offset < endOffset; offset++)
            {
                if (document.GetCharAt(offset) == ' ')
                {
                    spacesCount++;
                    if (spacesCount == indentationSize)
                    {
                        document.Replace(offset - (indentationSize - 1), indentationSize, "\t",
                            OffsetChangeMappingType.CharacterReplace);
                        spacesCount = 0;
                        offset -= indentationSize - 1;
                        endOffset -= indentationSize - 1;
                    }
                }
                else
                {
                    spacesCount = 0;
                }
            }
        }

        #endregion

        #region Convert...Case

        private static void ConvertCase(Func<string, string> transformText, TextArea textArea)
        {
            TransformSelectedSegments(
                delegate (TextArea textArea, ISegment segment)
                {
                    var oldText = textArea.Document.GetText(segment);
                    var newText = transformText(oldText);
                    textArea.Document.Replace(segment.Offset, segment.Length, newText,
                        OffsetChangeMappingType.CharacterReplace);
                }, textArea, DefaultSegmentType.WholeDocument);
        }

        private static void OnConvertToUpperCase(TextArea textArea)
        {
            ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToUpper, textArea);
        }

        private static void OnConvertToLowerCase(object target, TextArea textArea)
        {
            ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToLower, textArea);
        }

        private static void OnConvertToTitleCase(object target, TextArea textArea)
        {
            throw new NotSupportedException();
            //ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase, target, args);
        }

        private static void OnInvertCase(object target, TextArea textArea)
        {
            ConvertCase(InvertCase, textArea);
        }

        private static string InvertCase(string text)
        {
            // TODO: culture
            //var culture = CultureInfo.CurrentCulture;
            var buffer = text.ToCharArray();
            for (var i = 0; i < buffer.Length; ++i)
            {
                var c = buffer[i];
                buffer[i] = char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
            }
            return new string(buffer);
        }

        #endregion

        #region Undo / Redo

        private UndoStack GetUndoStack()
        {
            var document = TextArea.Document;
            return document?.UndoStack;
        }

        private void ExecuteUndo()
        {
            var undoStack = GetUndoStack();
            if (undoStack != null)
            {
                if (undoStack.CanUndo)
                {
                    undoStack.Undo();
                    TextArea.Caret.BringCaretToView();
                }
            }
        }

        private bool CanExecuteUndo()
        {
            var undoStack = GetUndoStack();
            if (undoStack != null)
            {
                return undoStack.CanUndo;
            }

            return false;
        }

        private void ExecuteRedo()
        {
            var undoStack = GetUndoStack();
            if (undoStack != null)
            {
                if (undoStack.CanRedo)
                {
                    undoStack.Redo();
                    TextArea.Caret.BringCaretToView();
                }
            }
        }

        private bool CanExecuteRedo()
        {
            var undoStack = GetUndoStack();
            if (undoStack != null)
            {
                return undoStack.CanRedo;
            }

            return false;
        }
        #endregion
        private static void OnIndentSelection(TextArea textArea)
        {
            if (textArea?.Document != null && textArea.IndentationStrategy != null)
            {
                using (textArea.Document.RunUpdate())
                {
                    int start, end;
                    if (textArea.Selection.IsEmpty)
                    {
                        start = 1;
                        end = textArea.Document.LineCount;
                    }
                    else
                    {
                        start = textArea.Document.GetLineByOffset(textArea.Selection.SurroundingSegment.Offset)
                            .LineNumber;
                        end = textArea.Document.GetLineByOffset(textArea.Selection.SurroundingSegment.EndOffset)
                            .LineNumber;
                    }
                    textArea.IndentationStrategy.IndentLines(textArea.Document, start, end);
                }
                textArea.Caret.BringCaretToView();
            }
        }
    }
}