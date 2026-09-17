using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Toolbar
{
    internal class UVCSToolbarButtonData
    {
        internal bool IsVisible { get; set; }
        internal string Text { get; set; }
        internal string Tooltip { get; set; }
        internal Texture Icon { get; set; }

        internal static UVCSToolbarButtonData BuildDefault()
        {
            return new UVCSToolbarButtonData
            {
                Text = PlasticLocalization.Name.UnityVCS.GetString(),
                Tooltip = PlasticLocalization.Name.UseUnityVersionControlToManageYourProject.GetString(),
                Icon = Images.GetPlasticViewIcon(),
                IsVisible = UVCSToolbarButtonIsShownPreference.IsEnabled() &&
                            UVCSPluginIsEnabledPreference.IsEnabled(),
            };
        }
    }
}
