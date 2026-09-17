using UnityEngine;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class SplitterGUILayout
    {
        internal delegate void BeginHorizontalSplitDelegate(
            SplitterState splitterState,
            params GUILayoutOption[] guiLayoutOptions);

        internal static BeginHorizontalSplitDelegate BeginHorizontalSplit { get; set; }

        internal delegate void EndHorizontalSplitDelegate();

        internal static EndHorizontalSplitDelegate EndHorizontalSplit { get; set; }

        internal delegate void BeginVerticalSplitDelegate(
            SplitterState splitterState,
            params GUILayoutOption[] guiLayoutOptions);

        internal static BeginVerticalSplitDelegate BeginVerticalSplit { get; set; }

        internal delegate void EndVerticalSplitDelegate();

        internal static EndVerticalSplitDelegate EndVerticalSplit { get; set; }
    }
}
