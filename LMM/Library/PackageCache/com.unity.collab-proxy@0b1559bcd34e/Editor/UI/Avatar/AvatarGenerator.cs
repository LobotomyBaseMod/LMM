using UnityEngine;

using PlasticGui;

namespace Unity.PlasticSCM.Editor.UI.Avatar
{
    internal static class AvatarGenerator
    {
        internal static Texture2D GenerateFromUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return null;

            Font font = GetDefaultFont();

            if (font == null)
                return null;

            Color bgColor = AvatarColor.FromUserName(userName);
            string initials = GetAvatarInitial.ForUserName(userName);

            return Generate(bgColor, initials, font);
        }

        static Texture2D Generate(Color bgColor, string initials, Font font)
        {
            int fontSize = Mathf.RoundToInt(RENDER_SIZE * 0.48f);
            font.RequestCharactersInTexture(initials, fontSize, FontStyle.Normal);

            Texture2D textTexture = RenderTextToTexture(font, initials, fontSize);
            Texture2D highRes = CompositeCircleAndText(bgColor, textTexture);

            UnityEngine.Object.DestroyImmediate(textTexture, true);

            Texture2D result = Downsample(highRes);

            UnityEngine.Object.DestroyImmediate(highRes, true);

            return result;
        }

        static Texture2D RenderTextToTexture(
            Font font, string initials, int fontSize)
        {
            RenderTexture rt = RenderTexture.GetTemporary(
                RENDER_SIZE, RENDER_SIZE, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, RENDER_SIZE, RENDER_SIZE, 0);

            DrawInitials(font, initials, fontSize);

            GL.PopMatrix();

            Texture2D result = new Texture2D(
                RENDER_SIZE, RENDER_SIZE, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, RENDER_SIZE, RENDER_SIZE), 0, 0);
            result.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        static Texture2D CompositeCircleAndText(
            Color bgColor, Texture2D textTexture)
        {
            Color[] textPixels = textTexture.GetPixels();
            Color[] pixels = new Color[RENDER_SIZE * RENDER_SIZE];

            float center = RENDER_SIZE / 2f;
            float radius = RENDER_SIZE / 2f;

            for (int y = 0; y < RENDER_SIZE; y++)
            {
                for (int x = 0; x < RENDER_SIZE; x++)
                {
                    int idx = y * RENDER_SIZE + x;

                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float circleAlpha = Mathf.Clamp01(radius - dist + 0.5f);

                    float textCoverage = textPixels[idx].r;

                    pixels[idx] = new Color(
                        Mathf.Lerp(bgColor.r, 1f, textCoverage),
                        Mathf.Lerp(bgColor.g, 1f, textCoverage),
                        Mathf.Lerp(bgColor.b, 1f, textCoverage),
                        circleAlpha);
                }
            }

            Texture2D result = new Texture2D(
                RENDER_SIZE, RENDER_SIZE, TextureFormat.RGBA32, false);
            result.SetPixels(pixels);
            result.Apply();

            return result;
        }

        static Texture2D Downsample(Texture2D source)
        {
            int blockSize = RENDER_SIZE / OUTPUT_SIZE;
            float invBlockArea = 1f / (blockSize * blockSize);

            Color[] sourcePixels = source.GetPixels();
            Color[] targetPixels = new Color[OUTPUT_SIZE * OUTPUT_SIZE];

            for (int ty = 0; ty < OUTPUT_SIZE; ty++)
            {
                for (int tx = 0; tx < OUTPUT_SIZE; tx++)
                {
                    float r = 0, g = 0, b = 0, a = 0;

                    for (int by = 0; by < blockSize; by++)
                    {
                        int rowOffset = (ty * blockSize + by) * RENDER_SIZE;

                        for (int bx = 0; bx < blockSize; bx++)
                        {
                            Color c = sourcePixels[rowOffset + tx * blockSize + bx];
                            r += c.r;
                            g += c.g;
                            b += c.b;
                            a += c.a;
                        }
                    }

                    targetPixels[ty * OUTPUT_SIZE + tx] = new Color(
                        r * invBlockArea,
                        g * invBlockArea,
                        b * invBlockArea,
                        a * invBlockArea);
                }
            }

            Texture2D result = new Texture2D(
                OUTPUT_SIZE, OUTPUT_SIZE, TextureFormat.RGBA32, false);
            result.filterMode = FilterMode.Bilinear;
            result.SetPixels(targetPixels);
            result.Apply();

            return result;
        }

        static void DrawInitials(Font font, string initials, int fontSize)
        {
            if (font.material == null)
                return;

            font.material.SetPass(0);

            CharacterInfo[] charInfos = new CharacterInfo[initials.Length];
            float totalAdvance = 0;
            float maxAscent = 0;
            float maxDescent = 0;

            for (int i = 0; i < initials.Length; i++)
            {
                if (!font.GetCharacterInfo(
                        initials[i], out charInfos[i], fontSize, FontStyle.Normal))
                    return;

                totalAdvance += charInfos[i].advance;
                maxAscent = Mathf.Max(maxAscent, charInfos[i].maxY);
                maxDescent = Mathf.Min(maxDescent, charInfos[i].minY);
            }

            float baselineY = (RENDER_SIZE + maxAscent + maxDescent) / 2f;
            float startX = (RENDER_SIZE - totalAdvance) / 2f;

            GL.Begin(GL.QUADS);
            GL.Color(Color.white);

            float curX = startX;
            for (int i = 0; i < initials.Length; i++)
            {
                CharacterInfo ci = charInfos[i];

                float left = curX + ci.minX;
                float right = curX + ci.maxX;
                float top = baselineY - ci.maxY;
                float bottom = baselineY - ci.minY;

                GL.TexCoord2(ci.uvBottomLeft.x, ci.uvBottomLeft.y);
                GL.Vertex3(left, bottom, 0);

                GL.TexCoord2(ci.uvBottomRight.x, ci.uvBottomRight.y);
                GL.Vertex3(right, bottom, 0);

                GL.TexCoord2(ci.uvTopRight.x, ci.uvTopRight.y);
                GL.Vertex3(right, top, 0);

                GL.TexCoord2(ci.uvTopLeft.x, ci.uvTopLeft.y);
                GL.Vertex3(left, top, 0);

                curX += ci.advance;
            }

            GL.End();
        }

        static Font GetDefaultFont()
        {
            if (mFont != null)
                return mFont;

#if UNITY_2022_1_OR_NEWER
            mFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            mFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

            return mFont;
        }

        static Font mFont;

        const int RENDER_SIZE = 256;
        const int OUTPUT_SIZE = 32;
    }
}
