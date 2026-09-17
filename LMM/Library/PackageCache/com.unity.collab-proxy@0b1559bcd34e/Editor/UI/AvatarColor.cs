using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class AvatarColor
    {
        internal static Color FromUserName(string userName)
        {
            return ColorFromText.Get(userName, mColors);
        }

        static Color[] mColors = new Color[] {
            new Color(0.784f, 0.424f, 0.000f, 1f), // #C86C00
            new Color(0.063f, 0.620f, 0.353f, 1f), // #109E5A
            new Color(0.302f, 0.439f, 0.933f, 1f), // #4D70EE
            new Color(0.580f, 0.302f, 0.933f, 1f), // #944DEE
            new Color(0.784f, 0.000f, 0.000f, 1f), // #C80000
            new Color(0.000f, 0.643f, 0.784f, 1f), // #00A4C8
            new Color(0.667f, 0.302f, 0.608f, 1f), // #AA4D9B
            new Color(0.714f, 0.600f, 0.000f, 1f), // #B69900
            new Color(0.231f, 0.231f, 0.231f, 1f), // #3B3B3B
            new Color(0.612f, 0.612f, 0.612f, 1f)  // #9C9C9C
        };
    }
}
