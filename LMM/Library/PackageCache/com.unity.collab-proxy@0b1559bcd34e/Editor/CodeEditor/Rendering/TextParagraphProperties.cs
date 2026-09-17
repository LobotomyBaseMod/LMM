using UnityEngine;

namespace Unity.CodeEditor.Rendering
{
    internal class TextParagraphProperties
    {
        private readonly double _tabSize;
        private readonly bool _textWrapping;
        private readonly GUIStyle _guiStyle;

        internal TextParagraphProperties Clone()
        {
            TextParagraphProperties result = new TextParagraphProperties(
                TextWrapping,
                TabSize,
                Font,
                FontSize,
                ForegroundColor);

            return result;
        }

        internal bool TextWrapping
        {
            get { return _textWrapping; }
        }

        internal double TabSize
        {
            get { return _tabSize; }
        }

        internal Font Font
        {
            get { return _guiStyle.font; }
        }

        internal int FontSize
        {
            get { return _guiStyle.fontSize; }
        }

        internal Color ForegroundColor
        {
            get { return _guiStyle.normal.textColor; }
        }

        internal GUIStyle GUIStyle
        {
            get { return _guiStyle; }
        }

        internal float Leading { get; set; } = 2;

        internal bool FirstLineInParagraph { get; set; }
        internal double Indent { get; set; }

        internal void SetForegroundColor(Color foreground)
        {
            if (foreground.r == 0 && foreground.g == 0 && foreground.b == 0 && foreground.a == 0)
            {
                _guiStyle.normal.textColor = TextEditorColors.DefaultText;
                return;
            }

            _guiStyle.normal.textColor = foreground;
        }

        internal TextParagraphProperties(
            bool textWrapping,
            double tabSize,
            Font font,
            int fontSize,
            Color foregroundColor)
        {
            _tabSize = tabSize;
            _textWrapping = textWrapping;
            _guiStyle = new GUIStyle()
            {
                wordWrap = false,
                font = font,
                fontSize = fontSize,
                normal =
                {
                    textColor = foregroundColor,
                },
                richText = true,
            };
        }
    }
}