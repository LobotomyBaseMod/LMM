#if UNITY_6000_3_OR_NEWER
using UnityEditor;
using UnityEngine;
using UnityEditor.Toolbars;

using Unity.PlasticSCM.Editor.Toolbar;

namespace Assets.Plugins.PlasticSCM.Editor.Toolbar
{
    [InitializeOnLoad]
    static class UVCSToolbarBoostrap
    {
        static UVCSToolbarBoostrap()
        {
            UVCSToolbar.Controller.OnToolbarInvalidated += RebuildToolbarButton;
            UVCSToolbar.Controller.OnToolbarButtonInvalidated += RebuildToolbarButton;
        }

        [MainToolbarElement(ToolbarController.ToolbarButtonPath, defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = 13)]
        [UnityOnlyMainToolbarPreset]
        static MainToolbarDropdown CreateControl()
        {
            UVCSToolbarButtonData buttonData = UVCSToolbar.Controller.GetButtonData();

            if (!buttonData.IsVisible)
                return null;

            return new MainToolbarDropdown(
                new MainToolbarContent(
                    Truncate(buttonData.Text, mMaxTextLength),
                    buttonData.Icon as Texture2D,
                    buttonData.Tooltip),
                OpenDropdown);
        }

        static string Truncate(string text, int maxTextLength)
        {
            const string ellipsis = "...";

            if (text.Length <= maxTextLength)
                return text;

            return string.Concat(text.Substring(0, maxTextLength - ellipsis.Length), ellipsis);
        }

        static void RebuildToolbarButton()
        {
            MainToolbar.Refresh(ToolbarController.ToolbarButtonPath);
        }

        static void OpenDropdown(Rect rect)
        {
            UVCSToolbar.Controller.PopupClicked(rect);
        }

        const int mMaxTextLength = 35;
    }
}
#endif
