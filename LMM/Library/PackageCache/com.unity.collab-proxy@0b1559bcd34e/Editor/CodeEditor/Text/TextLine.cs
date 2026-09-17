using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CodeEditor.Rendering;
using Unity.CodeEditor.Text;
using UnityEngine;

namespace Unity.CodeEditor.Text
{
    /// <summary>
    /// Represents a line of text that is used for text rendering.
    /// </summary>
    internal class TextLine : IDisposable
    {
        private readonly TextRun[] _textRuns;
        private int _initialTextLineOffset;

        internal TextLine(
            TextRun[] textRuns,
            int length,
            GUIContent markupGUIContent,
            GUIContent plainGuiContent,
            float height,
            Rect lineRect,
            TextParagraphProperties paragraphProperties,
            int initialTextLineOffset)
        {
            _textRuns = textRuns;
            _initialTextLineOffset = initialTextLineOffset;
            Length = length;
            MarkupGUIContent = markupGUIContent;
            PlainGUIContent = plainGuiContent;
            TextParagraphProperties = paragraphProperties;
            Height = Mathf.Round(height + paragraphProperties.Leading);
            LineRect = lineRect;
        }

        /// <summary>
        /// Gets the text runs that are contained within a line.
        /// </summary>
        /// <value>
        /// The contained text runs.
        /// </value>
        internal IReadOnlyList<TextRun> TextRuns => _textRuns;

        /// <summary>
        /// Gets the total number of TextSource positions of the current line.
        /// </summary>
        internal int Length { get; }

        internal TextParagraphProperties TextParagraphProperties { get; }

        internal GUIContent MarkupGUIContent { get; }

        internal GUIContent PlainGUIContent { get; }

        internal string MarkupText => MarkupGUIContent?.text ?? string.Empty;

        /// <summary>
        /// Gets the height of a line of text.
        /// </summary>
        /// <returns>
        /// The text line height.
        /// </returns>
        internal float Height { get; }

        internal Rect LineRect { get; }

        /// <summary>
        /// Gets the width of a line of text, excluding trailing whitespace characters.
        /// </summary>
        /// <returns>
        /// The text line width, excluding trailing whitespace characters.
        /// </returns>
        internal float Width => LineRect.width;

        /// <summary>
        /// Gets the width of a line of text, including trailing whitespace characters.
        /// </summary>
        /// <returns>
        /// The text line width, including trailing whitespace characters.
        /// </returns>
        internal float WidthIncludingTrailingWhitespace => LineRect.width;

        internal int TrailingWhitespaceLength { get; }

        /// <summary>
        /// Gets the character hit corresponding to the specified distance from the beginning of the line.
        /// </summary>
        /// <param name="distance">A <see cref="double"/> value that represents the distance from the beginning of the line.</param>
        /// <returns>The <see cref="CharacterHit"/> object at the specified distance from the beginning of the line.</returns>
        internal CharacterHit GetCharacterHitFromDistance(float distance)
        {
            int index = TextParagraphProperties.GUIStyle.GetCursorStringIndex(LineRect, PlainGUIContent, new Vector2(distance, 0));
            return new CharacterHit(index + _initialTextLineOffset, 0);
        }

        /// <summary>
        /// Gets the distance from the beginning of the line to the specified character hit.
        /// <see cref="CharacterHit"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> object whose distance you want to query.</param>
        /// <returns>A <see cref="double"/> that represents the distance from the beginning of the line.</returns>
        internal float GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            return TextParagraphProperties.GUIStyle.GetCursorPixelPosition(LineRect, PlainGUIContent, characterHit.FirstCharacterIndex - _initialTextLineOffset).x;
        }

        /// <summary>
        /// Gets the next character hit for caret navigation.
        /// </summary>
        /// <param name="characterHit">The current <see cref="CharacterHit"/>.</param>
        /// <returns>The next <see cref="CharacterHit"/>.</returns>
        internal CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the previous character hit for caret navigation.
        /// </summary>
        /// <param name="characterHit">The current <see cref="CharacterHit"/>.</param>
        /// <returns>The previous <see cref="CharacterHit"/>.</returns>
        internal CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the previous character hit after backspacing.
        /// </summary>
        /// <param name="characterHit">The current <see cref="CharacterHit"/>.</param>
        /// <returns>The <see cref="CharacterHit"/> after backspacing.</returns>
        internal CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get an array of bounding rectangles of a range of characters within a text line.
        /// </summary>
        /// <param name="firstTextSourceCharacterIndex">index of first character of specified range</param>
        /// <param name="textLength">number of characters of the specified range</param>
        /// <returns>an array of bounding rectangles.</returns>
        internal IReadOnlyList<Rect> GetTextBounds(int firstTextSourceCharacterIndex, int textLength)
        {
            Vector2 iniPoint = TextParagraphProperties.GUIStyle.GetCursorPixelPosition(LineRect, PlainGUIContent, firstTextSourceCharacterIndex - _initialTextLineOffset);
            Vector2 endPoint = TextParagraphProperties.GUIStyle.GetCursorPixelPosition(LineRect, PlainGUIContent, firstTextSourceCharacterIndex + textLength - _initialTextLineOffset);
            return new List<Rect>() { new Rect(iniPoint.x, iniPoint.y, endPoint.x - iniPoint.x, endPoint.y - iniPoint.y) };
        }

        public void Dispose()
        {

        }
    }
}