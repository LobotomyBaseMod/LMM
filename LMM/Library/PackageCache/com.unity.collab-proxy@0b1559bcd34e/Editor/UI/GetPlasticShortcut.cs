using Codice.Utils;
using PlasticGui;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class GetPlasticShortcut
    {
        internal static string ForOpen()
        {
            return PlasticLocalization.GetString(
                PlasticLocalization.Name.UnityOpenShortcut);
        }

        internal static string ForDelete()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityDeleteShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityDeleteShortcutForMacOS);

            return string.Empty;
        }

        internal static string ForDiff()
        {
            return PlasticLocalization.GetString(
                PlasticLocalization.Name.UnityDiffShortcut);
        }

        internal static string ForAssetDiff()
        {
            return PlasticLocalization.GetString(
                PlasticLocalization.Name.UnityAssetDiffShortcut);
        }

        internal static string ForHistory()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityHistoryShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityHistoryShortcutForMacOS);

            return string.Empty;
        }

        internal static string ForMerge()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityMergeShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityMergeShortcutForMacOS);

            return string.Empty;
        }

        internal static string ForHideUnhide()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityHideUnhideShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityHideUnhideShortcutForMacOS);

            return string.Empty;
        }

        internal static string ForLabel()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityLabelShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityLabelShortcutForMacOS);

            return string.Empty;
        }

        internal static string ForSwitch()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnitySwitchShortcutForWindows);
            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnitySwitchShortcutForMacOS);
            return string.Empty;
        }

        internal static string ForRename()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityRenameShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityRenameShortcutForMacOS);

            return string.Empty;
        }

        internal static string ForShowInExplorer()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityShowInExplorerShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityShowInExplorerShortcutForMacOS);

            return string.Empty;
        }

        internal static string ForEnter()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityEnterShortcutForWindows);

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.UnityEnterShortcutForMacOS);

            return string.Empty;
        }
    }
}
