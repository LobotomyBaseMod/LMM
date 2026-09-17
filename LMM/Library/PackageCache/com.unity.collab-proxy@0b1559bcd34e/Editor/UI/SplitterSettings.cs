using UnityEditor;

#if !UNITY_6000_3_OR_NEWER
using SplitterState = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SplitterState;
#endif

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class SplitterSettings
    {
        internal static float[] Load(
            string settingName,
            float[] defaultValue)
        {
            float leftValue = EditorPrefs.GetFloat(
                GetSettingKey(settingName, false),
                defaultValue[0]);

            float rightValue = EditorPrefs.GetFloat(
                GetSettingKey(settingName, true),
                defaultValue[1]);

            return new float [] { leftValue, rightValue };
        }

        internal static void Save(
            SplitterState splitterState,
            string settingName)
        {
            float[] relativeSizes = splitterState.relativeSizes;

            EditorPrefs.SetFloat(
                GetSettingKey(settingName, false),
                relativeSizes[0]);
            EditorPrefs.SetFloat(
                GetSettingKey(settingName, true),
                relativeSizes[1]);
        }

        static string GetSettingKey(string settingName, bool isRight)
        {
            return string.Format(
                settingName,
                PlayerSettings.productGUID,
                isRight ? RIGHT_SPLITTER_KEY_SUFFIX : LEFT_SPLITTER_KEY_SUFFIX);
        }

        static string LEFT_SPLITTER_KEY_SUFFIX = "LeftSplitter";
        static string RIGHT_SPLITTER_KEY_SUFFIX = "RightSplitter";
    }
}
