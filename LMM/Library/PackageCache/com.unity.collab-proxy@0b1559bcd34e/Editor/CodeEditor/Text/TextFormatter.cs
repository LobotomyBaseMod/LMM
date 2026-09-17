using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.CodeEditor.Rendering;
using UnityEngine;

namespace Unity.CodeEditor.Text
{
    internal class TextFormatter
    {
        internal static TextFormatter Current = new TextFormatter();

        internal interface ITextSource
        {
            TextRun GetTextRun(int textOffset);
        }

        internal Vector2 CalcSize(string text, TextParagraphProperties paragraphProperties)
        {
            GUIContent guiContent = new GUIContent(text);
            return paragraphProperties.GUIStyle.CalcSize(guiContent);
        }

        internal TextLine FormatLine(
            ITextSource textSource,
            int textOffset,
            float availableWidth,
            TextParagraphProperties paragraphProperties)
        {
            List<TextRun> textRuns = FetchTextRuns(textSource, availableWidth, paragraphProperties, textOffset);

            StringBuilder lineText = new StringBuilder();
            StringBuilder markup = new StringBuilder();

            foreach (TextRun textRun in textRuns)
            {
                if (textRun is TextEndOfParagraph)
                    continue;

                lineText.Append(textRun.Text);

                markup.Append("<color=#");
                markup.Append(ColorUtility.ToHtmlStringRGB(textRun.TextParagraphProperties.GUIStyle.normal.textColor));
                markup.Append(">");
                markup.Append(Escape(textRun.Text));
                markup.Append("</color>");
            }

            GUIContent markupGuiContent = new GUIContent(markup.ToString());
            GUIContent plainGuiContent = new GUIContent(lineText.ToString());

            float height = paragraphProperties.GUIStyle.CalcHeight(plainGuiContent, availableWidth);
            Vector2 lineSize = paragraphProperties.GUIStyle.CalcSize(plainGuiContent);
            Rect lineRect = new Rect(0, 0,  paragraphProperties.TextWrapping ? Math.Min(lineSize.x, availableWidth) : lineSize.x, height);

            TextLine result = new TextLine(
                textRuns.ToArray(),
                textRuns.Aggregate(0, (acc, run) => acc + run.Length),
                markupGuiContent,
                plainGuiContent,
                height,
                lineRect,
                paragraphProperties,
                textOffset);

            return result;
        }

        List<TextRun> FetchTextRuns(
            ITextSource textSource,
            float availableWidth,
            TextParagraphProperties paragraphProperties,
            int textOffset)
        {
            List<TextRun> result = new List<TextRun>();

            float accumulatedWidth = 0;

            while (true)
            {
                TextRun textRun = textSource.GetTextRun(textOffset);

                if (paragraphProperties.TextWrapping && textRun.Text != null)
                {
                    float textRunWidth = CalcSize(textRun.Text, textRun.TextParagraphProperties).x;

                    if (accumulatedWidth + textRunWidth > availableWidth)
                    {
                        if (textRun is TabTextRun)
                        {
                            result.Add(textRun);
                            return result;
                        }

                        if (!textRun.Text.Contains(' '))
                        {
                            if (result.Count == 0)
                            {
                                if (PerformCharacterWrap(availableWidth, textRun.Text, textRun.TextParagraphProperties, accumulatedWidth, result, ref result))
                                    return result;
                            }

                            return result;
                        }
                        else
                        {
                            StringBuilder wordBuffer = new StringBuilder();
                            StringBuilder currentWordBuffer = new StringBuilder();

                            for (int i = 0; i < textRun.Text.Length; i++)
                            {
                                currentWordBuffer.Append(textRun.Text[i]);

                                if (!IsWordSeparator(textRun.Text[i]) && i < textRun.Text.Length - 1)
                                    continue;

                                string currentWord = currentWordBuffer.ToString();
                                float wordWidth = CalcSize(currentWord, textRun.TextParagraphProperties).x;
                                if (accumulatedWidth + wordWidth > availableWidth)
                                {
                                    if (wordBuffer.Length == 0)
                                    {
                                        if (PerformCharacterWrap(availableWidth, currentWord, textRun.TextParagraphProperties, accumulatedWidth, result, ref result))
                                            return result;
                                    }

                                    result.Add(new StringTextRun(wordBuffer.ToString(), textRun.TextParagraphProperties));
                                    return result;
                                }

                                wordBuffer.Append(currentWordBuffer);
                                accumulatedWidth += wordWidth;
                                currentWordBuffer.Clear();
                            }
                        }
                    }

                    accumulatedWidth += textRunWidth;
                }

                result.Add(textRun);
                textOffset += textRun.Length;

                if (textRun is TextEndOfParagraph)
                    break;
            }

            return result;
        }

        bool PerformCharacterWrap(
            float availableWidth,
            string text,
            TextParagraphProperties p,
            float accumulatedWidth,
            List<TextRun> result,
            ref List<TextRun> textRuns)
        {
            StringBuilder charBuffer = new StringBuilder();
            foreach (char c in text)
            {
                float charWidth = CalcSize(c.ToString(), p).x;
                if (accumulatedWidth + charWidth > availableWidth)
                {
                    if (charBuffer.Length == 0)
                    {
                        // no content, at least add the current char
                        result.Add(new StringTextRun(c.ToString(), p));
                        {
                            textRuns = result;
                            return true;
                        }
                    }

                    result.Add(new StringTextRun(charBuffer.ToString(), p));
                    {
                        textRuns = result;
                        return true;
                    }
                }

                charBuffer.Append(c);
                accumulatedWidth += charWidth;
            }

            return false;
        }

        private static bool IsWordSeparator(char c)
        {
            return char.IsWhiteSpace(c) || char.IsPunctuation(c);
        }

        private string Escape(string text)
        {
            if (text == null)
                return string.Empty;

            return text.Replace("<", "<\u200B");
        }
    }
}