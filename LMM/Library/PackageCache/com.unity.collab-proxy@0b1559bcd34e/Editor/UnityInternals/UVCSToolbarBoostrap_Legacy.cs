#if !UNITY_6000_3_OR_NEWER
using UnityEditor;

using Unity.PlasticSCM.Editor;
using Unity.PlasticSCM.Editor.Configuration;
using Unity.PlasticSCM.Editor.Toolbar;

namespace Unity.Cloud.Collaborate
{
    [InitializeOnLoad]
    static class UVCSToolbarBoostrap
    {
        static UVCSToolbarBoostrap()
        {
            mDropDownButton = new UVCSToolbarButton(
                UVCSToolbar.Controller.PopupClicked,
                Toolbar.RepaintToolbar);

            Toolbar.AddSubToolbar(mDropDownButton);

            UVCSToolbar.Controller.OnToolbarInvalidated += ToolbarInvalidated;
            UVCSToolbar.Controller.OnToolbarButtonInvalidated += ButtonInvalidated;

            // Wait for editor to be ready to invalidate the button
            Execute.WhenEditorIsReady(ButtonInvalidated);
        }

        static void ToolbarInvalidated()
        {
            Toolbar.RepaintToolbar();
        }

        static void ButtonInvalidated()
        {
            UVCSToolbarButtonData buttonData = UVCSToolbar.Controller.GetButtonData();

            mDropDownButton.BeginUpdate();

            try
            {
                mDropDownButton.Text = buttonData.Text;
                mDropDownButton.Tooltip = buttonData.Tooltip;
                mDropDownButton.Icon = buttonData.Icon;
                mDropDownButton.IsVisible = buttonData.IsVisible;
            }
            finally
            {
                mDropDownButton.EndUpdate();
            }
        }

        static UVCSToolbarButton mDropDownButton;
    }
}
#endif
