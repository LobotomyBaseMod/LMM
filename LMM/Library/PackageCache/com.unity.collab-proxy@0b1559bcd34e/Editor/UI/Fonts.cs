using System.IO;

using UnityEditor;
using UnityEngine;

using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.AssetUtils;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class Fonts
    {
        internal enum Name
        {
            Cascadia,
        }

        internal static Font GetCascadiaFont()
        {
            if (mCascadiaFont == null)
                mCascadiaFont = Fonts.GetFont(Fonts.Name.Cascadia);

            return mCascadiaFont;
        }

        static Font GetFont(this Name font)
        {
            string fontFileName = font.ToString().ToLower() + ".ttf";

            string fontFileRelativePath = GetFontFileRelativePath(fontFileName);

            Font result = AssetDatabase.LoadAssetAtPath<Font>(fontFileRelativePath);

            if (result == null)
                mLog.WarnFormat("Font not found: {0}", fontFileName);

            return result;
        }

        static string GetFontFileRelativePath(string fontFileName)
        {
            return Path.Combine(
                AssetsPath.GetFontsFolderRelativePath(),
                fontFileName);
        }

        static Font mCascadiaFont;

        static readonly ILog mLog = PlasticApp.GetLogger("Fonts");
    }
}
