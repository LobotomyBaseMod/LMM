using System.IO;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    internal static class Utilities
    {
        internal static Texture2D LoadIcon(string path, string name)
        {
            if(EditorGUIUtility.isProSkin)
            {
                name = $"d_{name}";
            }
            var combinePath = Path.Combine(path, name);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(combinePath);
        }
    }
}
