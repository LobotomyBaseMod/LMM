using Codice.Utils;
using Unity.PlasticSCM.Editor.Settings;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.Operations
{
    internal class UncontrolledPopupOperations
    {
        internal UncontrolledPopupOperations(
            UVCSPlugin uvcsPlugin,
            IUpdateToolbarButtonVisibility updateToolbarButtonVisibility)
        {
            mUVCSPlugin = uvcsPlugin;
            mUpdateToolbarButtonVisibility = updateToolbarButtonVisibility;
        }

        internal void ShowUVCSWindow()
        {
            SwitchUVCSPlugin.OnIfNeeded(mUVCSPlugin);
        }

        internal void ShowUVCSSettings()
        {
            OpenUVCSProjectSettings.ByDefault();
        }

        internal void HideUVCSToolbarButton()
        {
            mUpdateToolbarButtonVisibility.Hide();
        }

        internal void OpenUVCSLandingPageInBrowser()
        {
            OpenBrowser.TryOpen("https://unity.com/solutions/version-control");
        }

        readonly IUpdateToolbarButtonVisibility mUpdateToolbarButtonVisibility;
        readonly UVCSPlugin mUVCSPlugin;
    }
}
