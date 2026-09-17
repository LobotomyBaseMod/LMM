using System;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Rendering;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.CodeEditor.TextMate
{
    internal class TextEditorModel : AbstractLineList, IDisposable
    {
        private readonly TextDocument _document;
        private readonly TextView _textView;
        private DocumentSnapshot _documentSnapshot;
        private Action<Exception> _exceptionHandler;
        private InvalidLineRange _invalidRange;

        internal DocumentSnapshot DocumentSnapshot { get { return _documentSnapshot; } }
        internal InvalidLineRange InvalidRange { get { return _invalidRange; } }

        internal TextEditorModel(TextView textView, TextDocument document, Action<Exception> exceptionHandler)
        {
            _textView = textView;
            _document = document;
            _exceptionHandler = exceptionHandler;

            _documentSnapshot = new DocumentSnapshot(_document);

            for (int i = 0; i < _document.LineCount; i++)
                AddLine(i);

            _document.Changing += DocumentOnChanging;
            _document.Changed += DocumentOnChanged;
            _document.UpdateFinished += DocumentOnUpdateFinished;
            _textView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
        }

        public override void Dispose()
        {
            _document.Changing -= DocumentOnChanging;
            _document.Changed -= DocumentOnChanged;
            _document.UpdateFinished -= DocumentOnUpdateFinished;
            _textView.ScrollOffsetChanged -= TextView_ScrollOffsetChanged;
        }

        public override void UpdateLine(int lineIndex) { }

        internal void InvalidateViewPortLines()
        {
            if (!_textView.VisualLinesValid ||
                _textView.VisualLines.Count == 0)
                return;

            InvalidateLineRange(
                _textView.VisualLines[0].FirstDocumentLine.LineNumber - 1,
                _textView.VisualLines[_textView.VisualLines.Count - 1].LastDocumentLine.LineNumber - 1);
        }

        public override int GetNumberOfLines()
        {
            return _documentSnapshot.LineCount;
        }

        public override LineText GetLineTextIncludingTerminators(int lineIndex)
        {
            return _documentSnapshot.GetLineTextIncludingTerminatorAsMemory(lineIndex);
        }

        public override int GetLineLength(int lineIndex)
        {
            return _documentSnapshot.GetLineLength(lineIndex);
        }

        private void TextView_ScrollOffsetChanged(object sender, EventArgs e)
        {
            try
            {
                TokenizeViewPort();
            }
            catch (Exception ex)
            {
                _exceptionHandler?.Invoke(ex);
            }
        }

        private void DocumentOnChanging(object sender, DocumentChangeEventArgs e)
        {
            try
            {
                if (e.RemovalLength > 0)
                {
                    var startLine = _document.GetLineByOffset(e.Offset).LineNumber - 1;
                    var endLine = _document.GetLineByOffset(e.Offset + e.RemovalLength).LineNumber - 1;

                    for (int i = endLine; i > startLine; i--)
                    {
                        RemoveLine(i);
                    }

                    _documentSnapshot.RemoveLines(startLine, endLine);
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler?.Invoke(ex);
            }
        }

        private void DocumentOnChanged(object sender, DocumentChangeEventArgs e)
        {
            try
            {
                int startLine = _document.GetLineByOffset(e.Offset).LineNumber - 1;
                int endLine = startLine;
                if (e.InsertionLength > 0)
                {
                    endLine = _document.GetLineByOffset(e.Offset + e.InsertionLength).LineNumber - 1;

                    for (int i = startLine; i < endLine; i++)
                    {
                        AddLine(i);
                    }
                }

                _documentSnapshot.Update(e);

                if (startLine == 0)
                {
                    SetInvalidRange(startLine, endLine);
                    return;
                }

                // some grammars (JSON, csharp, ...)
                // need to invalidate the previous line too

                SetInvalidRange(startLine - 1, endLine);
            }
            catch (Exception ex)
            {
                _exceptionHandler?.Invoke(ex);
            }
        }

        private void SetInvalidRange(int startLine, int endLine)
        {
            if (!_document.IsInUpdate)
            {
                InvalidateLineRange(startLine, endLine);
                return;
            }

            // we're in a document change, store the max invalid range
            if (_invalidRange == null)
            {
                _invalidRange = new InvalidLineRange(startLine, endLine);
                return;
            }

            _invalidRange.SetInvalidRange(startLine, endLine);
        }

        void DocumentOnUpdateFinished(object sender, EventArgs e)
        {
            if (_invalidRange == null)
                return;

            try
            {
                InvalidateLineRange(_invalidRange.StartLine, _invalidRange.EndLine);
            }
            finally
            {
                _invalidRange = null;
            }
        }

        private void TokenizeViewPort()
        {
            EditorDispatcher.Dispatch(() =>
            {
                if (!_textView.VisualLinesValid ||
                    _textView.VisualLines.Count == 0)
                    return;

                ForceTokenization(
                    _textView.VisualLines[0].FirstDocumentLine.LineNumber - 1,
                    _textView.VisualLines[_textView.VisualLines.Count - 1].LastDocumentLine.LineNumber - 1);
            });
        }

        internal class InvalidLineRange
        {
            internal int StartLine { get; private set; }
            internal int EndLine { get; private set; }

            internal InvalidLineRange(int startLine, int endLine)
            {
                StartLine = startLine;
                EndLine = endLine;
            }

            internal void SetInvalidRange(int startLine, int endLine)
            {
                if (startLine < StartLine)
                    StartLine = startLine;

                if (endLine > EndLine)
                    EndLine = endLine;
            }
        }
    }
}
