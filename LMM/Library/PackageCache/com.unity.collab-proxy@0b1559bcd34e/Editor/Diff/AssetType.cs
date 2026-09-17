using System.Collections.Generic;
using Codice.CM.Client.Differences.Graphic;
using Unity.PlasticSCM.Editor.AssetUtils;
using UnityEditor;
using UnityEngine;

namespace Plugins.PlasticSCM.Editor.Diff
{
    internal static class DiffViewerDataExtensions
    {
        internal static bool IsImage(this DiffViewerData data)
        {
            return TEXTURE_EXTENSIONS.Contains(data.Extension);
        }

        internal static bool IsSerializedAsset(this DiffViewerData data)
        {
            return SERIALIZED_EXTENSIONS.Contains(data.Extension);
        }

        static readonly HashSet<string> TEXTURE_EXTENSIONS = new HashSet<string>
        {
            ".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff",
            ".bmp", ".gif", ".psd", ".iff", ".pict", ".pct",
            ".pic", ".hdr", ".exr", ".dds", ".ktx", ".ktx2",
            ".astc", ".raw"
        };

        static readonly HashSet<string> SERIALIZED_EXTENSIONS = new HashSet<string>
        {
            ".prefab", ".unity", ".asset", ".mat", ".controller",
            ".overrideController", ".mask", ".preset", ".anim",
            ".lighting", ".giparams", ".shadervariants", ".guiskin",
            ".fontsettings", ".physicmaterial", ".physicsmaterial2d",
            ".cubemap", ".flare", ".mixer", ".renderTexture",
            ".meta"
        };
    }
}
