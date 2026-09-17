using System.Linq;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class ColorFromText
    {
        internal static Color Get(string text, Color[] colors)
        {
            int index = 0;

            if (!string.IsNullOrEmpty(text))
            {
                int value = text.Select(x => (int)x).Sum();
                index = value % (colors.Length - 1);
            }

            if (index >= 0 && index <= colors.Length - 1)
                return colors[index];

            return colors[0];
        }
    }
}
