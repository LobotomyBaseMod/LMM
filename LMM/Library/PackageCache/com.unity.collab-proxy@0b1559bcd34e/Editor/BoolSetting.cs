using UnityEditor;

namespace Unity.PlasticSCM.Editor
{
    internal static class BoolSetting
    {
        internal static bool Exists(
            string boolSettingName)
        {
            return EditorPrefs.HasKey(
                GetSettingKey(boolSettingName));
        }
        internal static bool Load(string settingName, bool defaultValue)
        {
            return EditorPrefs.GetBool(GetSettingKey(settingName), defaultValue);
        }

        internal static void Save(bool value, string settingName)
        {
            EditorPrefs.SetBool(GetSettingKey(settingName), value);
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
