using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal static class UVCSToolbarButtonIsShownPreference
    {
        internal static bool IsEnabled()
        {
            return BoolSetting.Load(UnityConstants.SHOW_UVCS_TOOLBAR_BUTTON_KEY_NAME, true);
        }

        internal static void Enable()
        {
            BoolSetting.Save(true, UnityConstants.SHOW_UVCS_TOOLBAR_BUTTON_KEY_NAME);
        }

        internal static void Disable()
        {
            BoolSetting.Save(false, UnityConstants.SHOW_UVCS_TOOLBAR_BUTTON_KEY_NAME);
        }
    }
}
