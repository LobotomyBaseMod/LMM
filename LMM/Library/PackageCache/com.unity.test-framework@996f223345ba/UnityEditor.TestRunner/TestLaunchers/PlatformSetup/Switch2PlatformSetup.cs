using System;
using UnityEditor;
using UnityEditor.Build.Profile;
using System.Reflection;

namespace UnityEditor.TestTools.TestRunner
{
    internal class Switch2PlatformSetup : IPlatformSetup
    {
        public void Setup()
        {
            EditorUserBuildSettings.development = true;

            // either method is good, PrepareForNativeAndSRPTestRun() is probably more efficent.
#if UNITY_SWITCH2
            UnityEditor.Switch2.EditorUserBuildSettingsUtility.PrepareForNativeAndSRPTestRun();
            // UnityEditor.Switch2.Switch2Settings.createRomFile = true;
            // UnityEditor.Switch2.Switch2Settings.nvnDrawValidation_Heavy = true;
            // UnityEditor.Switch2.Switch2Settings.nvnDrawValidation_Light = false;
            // UnityEditor.Switch2.Switch2Settings.enableHostIO = true;
#endif

        }

        public void PostBuildAction()
        {
        }

        public void PostSuccessfulBuildAction()
        {
        }

        public void PostSuccessfulLaunchAction()
        {
        }

        public void CleanUp()
        {
        }
    }
}
