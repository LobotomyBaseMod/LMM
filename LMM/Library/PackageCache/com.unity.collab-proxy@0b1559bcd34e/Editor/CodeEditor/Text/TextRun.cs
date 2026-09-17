using System;
using System.Diagnostics;
using Unity.CodeEditor.Rendering;
using Unity.CodeEditor.Utils;
using UnityEngine;

namespace Unity.CodeEditor.Text
{
    internal abstract class TextRun
    {
        internal const int DefaultTextSourceLength = 1;

        /// <summary>
        ///  Gets the text source length.
        /// </summary>
        internal virtual int Length => DefaultTextSourceLength;

        /// <summary>
        /// Gets the text run's text.
        /// </summary>
        internal virtual string Text => default;
        internal TextParagraphProperties TextParagraphProperties { get; }

        internal TextRun(TextParagraphProperties properties)
        {
            TextParagraphProperties = properties;
        }
    }

    internal class StringTextRun : TextRun
    {
        private string _contents;

        internal StringTextRun(string contents, TextParagraphProperties properties) : base(properties)
        {
            _contents = contents;
        }

        internal override int Length => _contents.Length;
        internal override string Text => _contents;
    }

    internal class TabTextRun : StringTextRun
    {
        internal TabTextRun(string tabString, TextParagraphProperties properties) : base(tabString, properties)
        {
        }
    }

    internal class StringSegmentTextRun : TextRun
    {
        private StringSegment _stringSegment;

        internal StringSegmentTextRun(StringSegment stringSegment, TextParagraphProperties properties) : base(properties)
        {
            _stringSegment = stringSegment;
        }

        internal override int Length => _stringSegment.Count;
        internal override string Text => _stringSegment.Text.Substring(_stringSegment.Offset, _stringSegment.Count);
    }

    internal class TextEndOfLine : TextRun
    {
        internal TextEndOfLine(int textSourceLength, TextParagraphProperties properties) : base(properties)
        {
            Length = textSourceLength;
        }

        internal override int Length { get; }
    }

    internal class TextEndOfParagraph : TextEndOfLine
    {
        internal TextEndOfParagraph(TextParagraphProperties properties) : base(DefaultTextSourceLength, properties)
        {

        }

        internal TextEndOfParagraph(int textSourceLength, TextParagraphProperties properties) : base(textSourceLength, properties)
        {

        }
    }

    internal class DrawableTextRun : StringTextRun
    {
        internal DrawableTextRun(string text, TextParagraphProperties properties) : base(text, properties)
        {

        }

        internal virtual void Draw(Vector2 origin)
        {

        }
    }
}