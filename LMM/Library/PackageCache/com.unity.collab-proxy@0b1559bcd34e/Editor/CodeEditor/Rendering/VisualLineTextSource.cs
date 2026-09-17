using System;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Text;
using Unity.CodeEditor.Utils;
using UnityEngine;

namespace Unity.CodeEditor.Rendering
{
    internal class VisualLineTextSource : ITextRunConstructionContext, TextFormatter.ITextSource
    {
        public VisualLineTextSource(VisualLine visualLine)
        {
           VisualLine = visualLine;
        }

		public VisualLine VisualLine { get; private set; }
		public TextView TextView { get; set; }
		public TextDocument Document { get; set; }
		public TextParagraphProperties TextParagraphProperties { get; set; }

		public TextRun GetTextRun(int textSourceCharacterIndex)
		{
			try {
				foreach (VisualLineElement element in VisualLine.Elements) {
					if (textSourceCharacterIndex >= element.VisualColumn
						&& textSourceCharacterIndex < element.VisualColumn + element.VisualLength) {
						int relativeOffset = textSourceCharacterIndex - element.VisualColumn;
						TextRun run = element.CreateTextRun(textSourceCharacterIndex, this);
						if (run == null)
							throw new ArgumentNullException(element.GetType().Name + ".CreateTextRun");
						if (run.Length == 0)
							throw new ArgumentException("The returned TextRun must not have length 0.", element.GetType().Name + ".Length");
						if (relativeOffset + run.Length > element.VisualLength)
							throw new ArgumentException("The returned TextRun is too long.", element.GetType().Name + ".CreateTextRun");
						/*if (run is InlineObjectRun inlineRun) {
							inlineRun.VisualLine = VisualLine;
							VisualLine.HasInlineObjects = true;
							TextView.AddInlineObject(inlineRun);
						}*/
						return run;
					}
				}
				if (TextView.Options.ShowEndOfLine && textSourceCharacterIndex == VisualLine.VisualLength && VisualLine.LastDocumentLine.DelimiterLength > 0) {
					return CreateTextRunForNewLine();
				}
				return new TextEndOfParagraph(1, TextParagraphProperties);
			} catch (Exception ex) {
				Debug.Log(ex.ToString());
				throw;
			}
		}

        private TextRun CreateTextRunForNewLine()
        {
            string newlineText = "";
            DocumentLine lastDocumentLine = VisualLine.LastDocumentLine;
            if (lastDocumentLine.DelimiterLength == 2)
            {
                newlineText = TextView.Options.EndOfLineCRLFGlyph;
            }
            else if (lastDocumentLine.DelimiterLength == 1)
            {
                char newlineChar = Document.GetCharAt(lastDocumentLine.Offset + lastDocumentLine.Length);
                if (newlineChar == '\r')
                    newlineText = TextView.Options.EndOfLineCRGlyph;
                else if (newlineChar == '\n')
                    newlineText = TextView.Options.EndOfLineLFGlyph;
                else
                    newlineText = "?";
            }

            var p = TextParagraphProperties.Clone();
            p.SetForegroundColor(TextView.NonPrintableCharacterColor);
            return new StringTextRun(newlineText, p);
        }

        internal ReadOnlyMemory<char> GetPrecedingText(int textSourceCharacterIndexLimit)
		{
			try {
				foreach (VisualLineElement element in VisualLine.Elements) {
					if (textSourceCharacterIndexLimit > element.VisualColumn
						&& textSourceCharacterIndexLimit <= element.VisualColumn + element.VisualLength) {
						var span = element.GetPrecedingText(textSourceCharacterIndexLimit, this);
						if (span.IsEmpty)
							break;
						int relativeOffset = textSourceCharacterIndexLimit - element.VisualColumn;
						if (span.Length > relativeOffset)
							throw new ArgumentException("The returned TextSpan is too long.", element.GetType().Name + ".GetPrecedingText");
						return span;
					}
				}

				return ReadOnlyMemory<char>.Empty;
			} catch (Exception ex) {
				Debug.Log(ex.ToString());
				throw;
			}
		}

		private string _cachedString;
		private int _cachedStringOffset;

		public StringSegment GetText(int offset, int length)
		{
			if (_cachedString != null) {
				if (offset >= _cachedStringOffset && offset + length <= _cachedStringOffset + _cachedString.Length) {
					return new StringSegment(_cachedString, offset - _cachedStringOffset, length);
				}
			}
			_cachedStringOffset = offset;
			return new StringSegment(_cachedString = Document.GetText(offset, length));
		}

    }
}