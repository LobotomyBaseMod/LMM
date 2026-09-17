using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal static class UVCSPluginIsEnabledPreference
    {
        internal static bool IsEnabled()
        {
            if (BoolSetting.Exists(UnityConstants.UVCS_PLUGIN_IS_ENABLED_KEY_NAME))
                return BoolSetting.Load(UnityConstants.UVCS_PLUGIN_IS_ENABLED_KEY_NAME, true);

            return BoolSetting.Load(UnityConstants.UVCS_PLUGIN_IS_ENABLED_OLD_KEY_NAME, true);
        }

        internal static void Enable()
        {
            BoolSetting.Save(true, UnityConstants.UVCS_PLUGIN_IS_ENABLED_KEY_NAME);
        }

        internal static void Disable()
        {
            BoolSetting.Save(false, UnityConstants.UVCS_PLUGIN_IS_ENABLED_KEY_NAME);
        }
    }
}
