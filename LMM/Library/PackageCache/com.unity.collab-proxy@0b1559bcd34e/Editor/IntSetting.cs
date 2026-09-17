using UnityEditor;

namespace Unity.PlasticSCM.Editor
{
    internal static class IntSetting
    {
        internal static int Load(string settingName, int defaultValue)
        {
            return EditorPrefs.GetInt(GetSettingKey(settingName), defaultValue);
        }

        internal static void Save(int value, string settingName)
        {
            EditorPrefs.SetInt(GetSettingKey(settingName), value);
        }

        internal static void Clear(string settingName)
        {
            EditorPrefs.DeleteKey(GetSettingKey(settingName));
        }

        static string GetSettingKey(string settingName)
        {
            return string.Format(settingName, PlayerSettings.productGUID);
        }
    }
}
