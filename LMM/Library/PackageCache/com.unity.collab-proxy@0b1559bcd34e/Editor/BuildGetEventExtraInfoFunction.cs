using System;

using UnityEngine;

using Codice.Client.Common;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal static class BuildGetEventExtraInfoFunction
    {
        internal static Func<string> ForPingEvent()
        {
            return () =>
            {
                PlasticGuiConfigData plasticGuiConfigData = PlasticGuiConfig.Get().Configuration;

                ClientConfigData clientConfigData = ClientConfig.Get().GetClientConfigData();

                WorkspaceInfo wkInfo = FindWorkspace.InfoForApplicationPath(
                    ApplicationDataPath.Get(), PlasticGui.Plastic.API);

                return GetScreenResolution() +
                       GetUnityEditorVersion() +
                       GetUnityPluginVersion() +
                       GetPingEventExtraInfo.GetChangelistsEnabled(plasticGuiConfigData) +
                       GetPingEventExtraInfo.GetSwitchBehaviorWithChangedItems(clientConfigData) +
                       GetPingEventExtraInfo.GetBranchExplorerDisplayOptions(wkInfo);
            };
        }

        static string GetScreenResolution()
        {
            string resolution = null;

            GUIActionRunner.RunGUIAction(delegate
            {
                resolution = ScreenResolution.Get();
            });

            if (resolution == null)
                return string.Empty;

            return string.Format(":screenresolution={0}", resolution);
        }

        static string GetUnityEditorVersion()
        {
            string unityEditorVersion = Application.unityVersion;

            if (string.IsNullOrEmpty(unityEditorVersion))
                return string.Empty;

            return string.Format(":unityEditorVersion={0}",
                unityEditorVersion);
        }

        static string GetUnityPluginVersion()
        {
            string unityPluginVersion = PackageInfo.Data.Version;

            if (string.IsNullOrEmpty(unityPluginVersion))
                return string.Empty;

            return string.Format(":unityPluginVersion={0}",
                unityPluginVersion);
        }
    }
}
