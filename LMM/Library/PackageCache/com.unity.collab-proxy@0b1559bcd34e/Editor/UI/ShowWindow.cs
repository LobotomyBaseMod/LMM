using System;

using UnityEditor;

using Codice.Client.Common.EventTracking;

using Plugins.PlasticSCM.Editor.Diff;
using Unity.PlasticSCM.Editor.CloudDrive;
using Unity.PlasticSCM.Editor.Views.BranchExplorer;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class ShowWindow
    {
        // This legacy method is kept for backward compatibility with the Build Automation package
        // (it was the only method up to com.unity.collab-proxy "2.8.2")
        // UUM-115391 "Failed to open UVCS Window" error is thrown [...] settings in Build Profiles
        internal static UVCSWindow Plastic()
        {
            return UVCS();
        }

        internal static UVCSWindow UVCS()
        {
            UVCSWindow window = EditorWindow.GetWindow<UVCSWindow>(
                UnityConstants.UVCS_WINDOW_TITLE,
                true,
                mConsoleWindowType,
                mProjectBrowserType);

            window.titleContent.image = UVCSPlugin.Instance.GetPluginStatusIcon();

            return window;
        }

        internal static BranchExplorerWindow BranchExplorer()
        {
            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(
                    FindWorkspace.InfoForApplicationPath(
                        ApplicationDataPath.Get(), PlasticGui.Plastic.API)),
                TrackFeatureUseEvent.Features.UnityPackage.OpenBranchExplorerView);

            BranchExplorerWindow window = EditorWindow.GetWindow<BranchExplorerWindow>(
                UnityConstants.BREX_WINDOW_TITLE,
                true,
                mGameViewType);

            window.titleContent.image = Images.GetBranchExplorerIcon();

            return window;
        }

        internal static CloudDriveWindow CloudDrive()
        {
            CloudDriveWindow window = EditorWindow.GetWindow<CloudDriveWindow>(
                UnityConstants.CloudDrive.WINDOW_TITLE,
                true,
                mConsoleWindowType,
                mProjectBrowserType);

            window.titleContent.image = Images.GetCloudDriveViewIcon();

            return window;
        }

        internal static DiffWindow Diff()
        {
            DiffWindow window = EditorWindow.GetWindow<DiffWindow>(
                UnityConstants.DIFF_WINDOW_TITLE,
                true,
                mGameViewType);

            return window;
        }

        static Type mConsoleWindowType = typeof(EditorWindow).
            Assembly.GetType("UnityEditor.ConsoleWindow");
        static Type mProjectBrowserType = typeof(EditorWindow).
            Assembly.GetType("UnityEditor.ProjectBrowser");
        static Type mGameViewType = typeof(EditorWindow).
            Assembly.GetType("UnityEditor.GameView");
    }
}
