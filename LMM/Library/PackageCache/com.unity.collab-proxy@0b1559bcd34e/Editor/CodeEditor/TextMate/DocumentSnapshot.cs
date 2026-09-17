using System;
using System.Linq;
using Unity.CodeEditor.Document;

namespace Unity.CodeEditor.TextMate
{
    internal class DocumentSnapshot
    {
        private LineRange[] _lineRanges;
        private TextDocument _document;
        private ITextSource _textSource;
        private object _lock = new object();
        private int _lineCount;

        internal int LineCount
        {
            get { lock (_lock) { return _lineCount; } }
        }

        internal DocumentSnapshot(TextDocument document)
        {
            _document = document;
            _lineRanges = new LineRange[document.LineCount];

            Update(null);
        }

        internal void RemoveLines(int startLine, int endLine)
        {
            lock (_lock)
            {
                var tmpList = _lineRanges.ToList();
                tmpList.RemoveRange(startLine, endLine - startLine + 1);
                _lineRanges = tmpList.ToArray();
                _lineCount = _lineRanges.Length;
            }
        }

        internal string GetLineText(int lineIndex)
        {
            lock (_lock)
            {
                var lineRange = _lineRanges[lineIndex];
                return _textSource.GetText(lineRange.Offset, lineRange.Length);
            }
        }

        internal string GetLineTextIncludingTerminator(int lineIndex)
        {
            lock (_lock)
            {
                var lineRange = _lineRanges[lineIndex];
                return _textSource.GetText(lineRange.Offset, lineRange.TotalLength);
            }
        }

        internal ReadOnlyMemory<char> GetLineTextIncludingTerminatorAsMemory(int lineIndex)
        {
            lock (_lock)
            {
                var lineRange = _lineRanges[lineIndex];
                return _textSource.GetTextAsMemory(lineRange.Offset, lineRange.TotalLength);
            }
        }

        internal string GetLineTerminator(int lineIndex)
        {
            lock (_lock)
            {
                var lineRange = _lineRanges[lineIndex];
                return _textSource.GetText(lineRange.Offset + lineRange.Length, lineRange.TotalLength - lineRange.Length);
            }
        }

        internal int GetLineLength(int lineIndex)
        {
            lock (_lock)
            {
                return _lineRanges[lineIndex].Length;
            }
        }

        internal int GetTotalLineLength(int lineIndex)
        {
            lock (_lock)
            {
                return _lineRanges[lineIndex].TotalLength;
            }
        }

        internal string GetText()
        {
            lock (_lock)
            {
                return _textSource.Text;
            }
        }

        internal void Update(DocumentChangeEventArgs e)
        {
            lock (_lock)
            {
                _lineCount = _document.Lines.Count;

                if (e != null && e.OffsetChangeMap != null && _lineRanges != null && _lineCount == _lineRanges.Length)
                {
                    // it's a single-line change
                    // update the offsets usign the OffsetChangeMap
                    RecalculateOffsets(e);
                }
                else
                {
                    // recompute all the line ranges
                    // based in the document lines
                    RecomputeAllLineRanges(e);
                }

                _textSource = _document.CreateSnapshot();
            }
        }

        private void RecalculateOffsets(DocumentChangeEventArgs e)
        {
            var changedLine = _document.GetLineByOffset(e.Offset);
            int lineIndex = changedLine.LineNumber - 1;

            _lineRanges[lineIndex].Offset = changedLine.Offset;
            _lineRanges[lineIndex].Length = changedLine.Length;
            _lineRanges[lineIndex].TotalLength = changedLine.TotalLength;

            for (int i = lineIndex + 1; i < _lineCount; i++)
            {
                _lineRanges[i].Offset = e.OffsetChangeMap.GetNewOffset(_lineRanges[i].Offset);
            }
        }

        private void RecomputeAllLineRanges(DocumentChangeEventArgs e)
        {
            Array.Resize(ref _lineRanges, _lineCount);

            int currentLineIndex = (e != null) ?
                _document.GetLineByOffset(e.Offset).LineNumber - 1 : 0;
            var currentLine = _document.GetLineByNumber(currentLineIndex + 1);

            while (currentLine != null)
            {
                _lineRanges[currentLineIndex].Offset = currentLine.Offset;
                _lineRanges[currentLineIndex].Length = currentLine.Length;
                _lineRanges[currentLineIndex].TotalLength = currentLine.TotalLength;
                currentLine = currentLine.NextLine;
                currentLineIndex++;
            }
        }

        struct LineRange
        {
            internal int Offset;
            internal int Length;
            internal int TotalLength;
        }
    }
}
