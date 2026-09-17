using System.Linq;

using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class AttributeColor
    {
        internal static Color FromAttributeName(string userName)
        {
            return ColorFromText.Get(userName, mColors);
        }

        static readonly Color[] mColors = new Color[] {
            new Color(0.784f, 0.424f, 0.000f, 1f), // #C86C00
            new Color(0.302f, 0.439f, 0.933f, 1f), // #4D70EE
            new Color(0.580f, 0.302f, 0.933f, 1f), // #944DEE
            new Color(0.000f, 0.643f, 0.784f, 1f), // #00A4C8
            new Color(0.667f, 0.302f, 0.608f, 1f), // #AA4D9B
            new Color(0.231f, 0.231f, 0.231f, 1f), // #3B3B3B
            new Color(0.612f, 0.612f, 0.612f, 1f),  // #9C9C9C
            new Color(0.839f, 0.361f, 0.541f, 1f), // #D65C8A
            new Color(0.361f, 0.557f, 0.839f, 1f), // #5C8ED6
            new Color(0.424f, 0.373f, 0.839f, 1f), // #6C5FD6
            new Color(0.784f, 0.361f, 0.424f, 1f), // #C85C6C
            new Color(0.361f, 0.608f, 0.541f, 1f), // #5C9B8A
            new Color(0.541f, 0.424f, 0.361f, 1f), // #8A6C5C
            new Color(0.424f, 0.541f, 0.361f, 1f), // #6C8A5C
            new Color(0.361f, 0.424f, 0.541f, 1f), // #5C6C8A
        };
    }
}
