using System;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Rendering;
using UnityEngine;

namespace Unity.CodeEditor.TextMate
{
    internal abstract class GenericLineTransformer : DocumentColorizingTransformer
    {
        private Action<Exception> _exceptionHandler;

        internal GenericLineTransformer(Action<Exception> exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            try
            {
                TransformLine(line, CurrentContext);
            }
            catch (Exception ex)
            {
                _exceptionHandler?.Invoke(ex);
            }
        }

        protected abstract void TransformLine(DocumentLine line, ITextRunConstructionContext context);

        internal void SetTextStyle(
            DocumentLine line,
            int startIndex,
            int length,
            Color foreground,
            Color background,
            FontStyle fontStyle,
            //FontWeight fontWeigth,
            bool isUnderline)
        {
            int startOffset = 0;
            int endOffset = 0;

            if (startIndex >= 0 && length > 0)
            {
                if ((line.Offset + startIndex + length) > line.EndOffset)
                {
                    length = (line.EndOffset - startIndex) - line.Offset - startIndex;
                }

                startOffset = line.Offset + startIndex;
                endOffset = line.Offset + startIndex + length;
            }
            else
            {
                startOffset = line.Offset;
                endOffset = line.EndOffset;
            }

            if (startOffset > CurrentContext.Document.TextLength ||
                endOffset > CurrentContext.Document.TextLength)
                return;

            ChangeLinePart(
                startOffset,
                endOffset,
                visualLine => ChangeVisualLine(visualLine, foreground, background, fontStyle/*, fontWeigth*/, isUnderline));
        }

        void ChangeVisualLine(
            VisualLineElement visualLine,
            Color foreground,
            Color background,
            FontStyle fontStyle,
            //FontWeight fontWeigth,
            bool isUnderline)
        {
            TextParagraphProperties newProperties = visualLine.TextParagraphProperties.Clone();

            if (foreground != null)
                newProperties.SetForegroundColor(foreground);

            /*if (background != null)
                newProperties.SetBackgroundBrush(background);

            if (isUnderline)
            {
                newProperties.SetTextDecorations(TextDecorations.Underline);
            }

            if (visualLine.TextRunProperties.Typeface.Style != fontStyle ||
                newProperties.Typeface.Weight != fontWeigth)
            {
                newProperties.SetTypeface(new Typeface(
                    visualLine.TextRunProperties.Typeface.FontFamily, fontStyle, fontWeigth));
            }*/

            visualLine.SetTextParagraphProperties(newProperties);
        }
    }
}