using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.Toolbar;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Settings
{
    internal class UVCSProjectSettingsProvider : SettingsProvider
    {
        [SettingsProvider]
        internal static SettingsProvider CreateSettingsProvider()
        {
            PlasticApp.InitializeIfNeeded();

            return new UVCSProjectSettingsProvider(
                UnityConstants.PROJECT_SETTINGS_TAB_PATH,
                SettingsScope.Project,
                UVCSPlugin.Instance,
                UVCSToolbar.Controller);
        }

        UVCSProjectSettingsProvider(
            string path,
            SettingsScope scope,
            UVCSPlugin uvcsPlugin,
            IUpdateToolbarButtonVisibility updateToolbarButtonVisibility)
            : base(path, scope)
        {
            mUVCSPlugin = uvcsPlugin;
            mUpdateToolbarButtonVisibility = updateToolbarButtonVisibility;

            label = UnityConstants.PROJECT_SETTINGS_TAB_TITLE;

            OpenAllFoldouts();
        }

        public override void OnActivate(
            string searchContext,
            VisualElement rootElement)
        {
            mUVCSPlugin.ActiveUVCSSettingsProvider = this;

            ReloadSettings();

            if (mWkInfo == null)
                return;

            mIsProjectSettingsActivated = true;

            mPendingChangesOptionsFoldout.OnActivate(mWkInfo, mUVCSPlugin.PendingChangesUpdater);
            mDiffAndMergeOptionsFoldout.OnActivate();
            mShelveAndSwitchOptionsFoldout.OnActivate();
            mOtherOptionsFoldout.OnActivate();
        }

        public override void OnDeactivate()
        {
            mUVCSPlugin.ActiveUVCSSettingsProvider = null;

            if (!mIsProjectSettingsActivated)
                return;

            mIsProjectSettingsActivated = false;

            mPendingChangesOptionsFoldout.OnDeactivate(mUVCSPlugin.PendingChangesUpdater);
            mDiffAndMergeOptionsFoldout.OnDeactivate();
            mShelveAndSwitchOptionsFoldout.OnDeactivate();
            mOtherOptionsFoldout.OnDeactivate();
        }

        public override void OnGUI(string searchContext)
        {
            DrawSettingsSection(DoIsEnabledSetting);

            if (!mIsUVCSPluginEnabled)
                return;

#if !UNITY_6000_3_OR_NEWER
            DrawSettingsSection(DoShowToolbarButtonSetting);
#endif

            if (mWkInfo == null)
                return;

            mIsPendingChangesFoldoutOpen = DrawFoldout(
                mIsPendingChangesFoldoutOpen,
                PlasticLocalization.Name.PendingChangesOptionsSectionTitle.GetString(),
                mPendingChangesOptionsFoldout.OnGUI);

            mIsDiffAndMergeFoldoutOpen = DrawFoldout(
                mIsDiffAndMergeFoldoutOpen,
                PlasticLocalization.Name.DiffAndMergeOptionsSectionTitle.GetString(),
                mDiffAndMergeOptionsFoldout.OnGUI);

            mIsShelveAndSwitchFoldoutOpen = DrawFoldout(
                mIsShelveAndSwitchFoldoutOpen,
                PlasticLocalization.Name.ShelveAndSwitchOptionsSectionTitle.GetString(),
                mShelveAndSwitchOptionsFoldout.OnGUI);

            mIsOtherFoldoutOpen = DrawFoldout(
                mIsOtherFoldoutOpen,
                PlasticLocalization.Name.OtherOptionsSectionTitle.GetString(),
                mOtherOptionsFoldout.OnGUI);
        }

        internal void ReloadSettings()
        {
            mIsUVCSPluginEnabled = UVCSPluginIsEnabledPreference.IsEnabled();

            mShowToolbarButton = UVCSToolbarButtonIsShownPreference.IsEnabled();

            mWkInfo = FindWorkspace.InfoForApplicationPath(
                ApplicationDataPath.Get(), PlasticGui.Plastic.API);
        }

        internal void OpenAllFoldouts()
        {
            mIsPendingChangesFoldoutOpen = true;
            mIsDiffAndMergeFoldoutOpen = true;
            mIsShelveAndSwitchFoldoutOpen = true;
            mIsOtherFoldoutOpen = true;
        }

        internal void OpenDiffAndMergeFoldout()
        {
            mIsDiffAndMergeFoldoutOpen = true;
            mIsPendingChangesFoldoutOpen = false;
            mIsShelveAndSwitchFoldoutOpen = false;
            mIsOtherFoldoutOpen = false;
        }

        internal void OpenShelveAndSwitchFoldout()
        {
            mIsShelveAndSwitchFoldoutOpen = true;
            mIsPendingChangesFoldoutOpen = false;
            mIsDiffAndMergeFoldoutOpen = false;
            mIsOtherFoldoutOpen = false;
        }

        internal void OpenOtherFoldout()
        {
            mIsOtherFoldoutOpen = true;
            mIsPendingChangesFoldoutOpen = false;
            mIsDiffAndMergeFoldoutOpen = false;
            mIsShelveAndSwitchFoldoutOpen = false;
        }

        internal void ShowToolbarButtonCheckboxRaiseCheckedForTesting()
        {
            mShowToolbarButton = true;
        }

        internal void ShowToolbarButtonCheckboxRaiseUncheckedForTesting()
        {
            mShowToolbarButton = false;
        }

        void DoIsEnabledSetting()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string message = PlasticLocalization.GetString(
                    mIsUVCSPluginEnabled ?
                        PlasticLocalization.Name.UnityVCSIsEnabled :
                        PlasticLocalization.Name.UnityVCSIsDisabled);

                GUILayout.Label(
                    message,
                    EditorStyles.boldLabel,
                    GUILayout.Height(20));

                EditorGUILayout.Space(8);

                DoIsEnabledButton();

                GUILayout.FlexibleSpace();
            }
        }

        void DoIsEnabledButton()
        {
            if (!GUILayout.Button(PlasticLocalization.GetString(
                    mIsUVCSPluginEnabled ?
                        PlasticLocalization.Name.DisableButton :
                        PlasticLocalization.Name.EnableButton),
                    UnityStyles.ProjectSettings.ToggleOn))
            {
                return;
            }

            EditorWindow currentWindow = EditorWindow.focusedWindow;

            if (mIsUVCSPluginEnabled)
            {
                mIsUVCSPluginEnabled = false;

                TrackFeatureEvent(
                    mWkInfo,
                    TrackFeatureUseEvent.Features.UnityPackage.DisableManually);

                SwitchUVCSPlugin.Off(mUVCSPlugin);
            }
            else
            {
                mIsUVCSPluginEnabled = true;

                TrackFeatureEvent(
                    mWkInfo,
                    TrackFeatureUseEvent.Features.UnityPackage.EnableManually);

                SwitchUVCSPlugin.On(mUVCSPlugin);
            }

            currentWindow.Focus();
        }

        void DoShowToolbarButtonSetting()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool wasChecked = mShowToolbarButton;

                bool isChecked = EditorGUILayout.Toggle(Styles.ShowToolbarButton, wasChecked);

                if (!wasChecked && isChecked)
                {
                    mShowToolbarButton = true;
                    mUpdateToolbarButtonVisibility.Show();
                    return;
                }

                if (wasChecked && !isChecked)
                {
                    mShowToolbarButton = false;
                    mUpdateToolbarButtonVisibility.Hide();
                    return;
                }
            }
        }

        static void DrawSettingsSection(Action drawSettings)
        {
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            try
            {
                EditorGUIUtility.labelWidth = UnityConstants.SETTINGS_GUI_WIDTH_MAIN_SECTION;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(10);

                        drawSettings();

                        GUILayout.Space(10);
                    }

                    GUILayout.Space(10);
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = originalLabelWidth;
            }
        }

        static bool DrawFoldout(
            bool isFoldoutOpen,
            string title,
            Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool result =
                EditorGUILayout.BeginFoldoutHeaderGroup(
                    isFoldoutOpen,
                    title,
                    UnityStyles.ProjectSettings.FoldoutHeader);

            if (result)
                drawContent();

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();

            return result;
        }

        static void TrackFeatureEvent(WorkspaceInfo wkInfo, string eventName)
        {
            if (wkInfo == null)
                return;

            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(wkInfo),
                eventName);
        }

        class Styles
        {
            internal static GUIContent ShowToolbarButton =
                new GUIContent(
                    PlasticLocalization.Name.ShowUnityVersionControlToolbarButton.GetString());
        }

        bool mIsPendingChangesFoldoutOpen;
        bool mIsDiffAndMergeFoldoutOpen;
        bool mIsShelveAndSwitchFoldoutOpen;
        bool mIsOtherFoldoutOpen;
        bool mIsProjectSettingsActivated;
        bool mShowToolbarButton;
        bool mIsUVCSPluginEnabled;

        WorkspaceInfo mWkInfo;

        PendingChangesOptionsFoldout mPendingChangesOptionsFoldout = new PendingChangesOptionsFoldout();
        DiffAndMergeOptionsFoldout mDiffAndMergeOptionsFoldout = new DiffAndMergeOptionsFoldout();
        ShelveAndSwitchOptionsFoldout mShelveAndSwitchOptionsFoldout = new ShelveAndSwitchOptionsFoldout();
        OtherOptionsFoldout mOtherOptionsFoldout = new OtherOptionsFoldout();

        readonly IUpdateToolbarButtonVisibility mUpdateToolbarButtonVisibility;
        readonly UVCSPlugin mUVCSPlugin;
    }
}
