using System;
using System.Collections.Generic;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.TextMate;
using UnityEngine;

using UE = UnityEngine;
using TM = TextMateSharp.Themes;

namespace Unity.CodeEditor.TextMate
{
    internal abstract class TextTransformation : TextSegment
    {
        internal abstract void Transform(GenericLineTransformer transformer, DocumentLine line);
    }

    internal class ForegroundTextTransformation : TextTransformation
    {
        internal Dictionary<int, Color> ColorMap { get; set; }
        internal Action<Exception> ExceptionHandler { get; set; }
        internal int ForegroundColor { get; set; }
        internal int BackgroundColor { get; set; }
        internal TM.FontStyle FontStyle { get; set; }

        internal override void Transform(GenericLineTransformer transformer, DocumentLine line)
        {
            try
            {
                if (Length == 0)
                {
                    return;
                }

                var formattedOffset = 0;
                var endOffset = line.EndOffset;

                if (StartOffset > line.Offset)
                {
                    formattedOffset = StartOffset - line.Offset;
                }

                if (EndOffset < line.EndOffset)
                {
                    endOffset = EndOffset;
                }

                transformer.SetTextStyle(line, formattedOffset, endOffset - line.Offset - formattedOffset,
                    GetBrush(ForegroundColor),
                    GetBrush(BackgroundColor),
                    GetFontStyle(),
                    //GetFontWeight(),
                    IsUnderline());
            }
            catch (Exception ex)
            {
                ExceptionHandler?.Invoke(ex);
            }
        }

        UE.FontStyle GetFontStyle()
        {
            if (FontStyle != TM.FontStyle.NotSet &&
                (FontStyle & TM.FontStyle.Italic) != 0)
                return UE.FontStyle.Italic;

            if (FontStyle != TM.FontStyle.NotSet &&
                (FontStyle & TM.FontStyle.Bold) != 0)
                return UE.FontStyle.Bold;

            if (FontStyle != TM.FontStyle.NotSet &&
                (FontStyle & TM.FontStyle.Bold) != 0 &&
                (FontStyle & TM.FontStyle.Italic) != 0)
                return UE.FontStyle.BoldAndItalic;

            return UE.FontStyle.Normal;
        }

        bool IsUnderline()
        {
            if (FontStyle != TM.FontStyle.NotSet &&
                (FontStyle & TM.FontStyle.Underline) != 0)
                return true;

            return false;
        }

        UE.Color GetBrush(int colorId)
        {
            if (ColorMap == null)
                return UE.Color.red;

            ColorMap.TryGetValue(colorId, out UE.Color result);
            return result;
        }
    }
}
