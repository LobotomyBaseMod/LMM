using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.Client.Common.WebApi;
using PlasticGui;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome;
using Unity.PlasticSCM.Editor.Configuration.TeamEdition;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.Views.CreateWorkspace;

using HubCreateWorkspace = Unity.PlasticSCM.Editor.Hub.Operations.CreateWorkspace;

namespace Unity.PlasticSCM.Editor.Views.Welcome
{
    internal class WelcomeView : AutoLogin.IWelcomeView
    {
        AutoLogin.State AutoLogin.IWelcomeView.AutoLoginState { get; set; }

        internal WelcomeView(
            UVCSWindow parentWindow,
            CreateWorkspaceView.ICreateWorkspaceListener createWorkspaceListener,
            IPlasticAPI plasticApi,
            IPlasticWebRestApi plasticWebRestApi)
        {
            mParentWindow = parentWindow;
            mCreateWorkspaceListener = createWorkspaceListener;
            mPlasticApi = plasticApi;
            mPlasticWebRestApi = plasticWebRestApi;

            mConfigureProgress = new ProgressControlsForViews();
            ((AutoLogin.IWelcomeView)this).AutoLoginState = AutoLogin.State.Off;
        }

        internal void Update()
        {
            if (mCreateWorkspaceView != null)
                mCreateWorkspaceView.Update();

            mConfigureProgress.UpdateProgress(mParentWindow);
        }

        internal void OnGUI(bool clientNeedsConfiguration)
        {
            if (HubCreateWorkspace.IsOperationInProgress)
            {
                DoHubWorkspaceCreationProgress();
                return;
            }

            GUILayout.BeginHorizontal();

            GUILayout.Space(HORIZONTAL_MARGIN);

            DoContentViewArea(
                clientNeedsConfiguration,
                mIsCreateWorkspaceButtonClicked);

            GUILayout.Space(HORIZONTAL_MARGIN);

            GUILayout.EndHorizontal();
        }

        void DoHubWorkspaceCreationProgress()
        {
            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            OverlayProgress.DoOverlayProgress(
                viewRect,
                -1f,
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.CreatingWorkspaceProgress));

            mParentWindow.Repaint();
        }

        void AutoLogin.IWelcomeView.OnUserClosedConfigurationWindow()
        {
            ((IProgressControls)mConfigureProgress).HideProgress();

            ClientConfig.Reset();
            CmConnection.ResetForTesting();
            ClientHandlers.Register();
        }

        void DoContentViewArea(
            bool clientNeedsConfiguration,
            bool isCreateWorkspaceButtonClicked)
        {
            GUILayout.BeginVertical();

            GUILayout.Space(TOP_MARGIN);

            if (isCreateWorkspaceButtonClicked)
                GetCreateWorkspaceView().OnGUI();
            else
                DoSetupViewArea(
                    clientNeedsConfiguration,
                    mConfigureProgress);

            GUILayout.EndVertical();
        }

        void DoSetupViewArea(
            bool clientNeedsConfiguration,
            ProgressControlsForViews configureProgress)
        {
            DoTitleLabel();

            GUILayout.Space(STEPS_TOP_MARGIN);

            bool isStep1Completed =
                !clientNeedsConfiguration &&
                !configureProgress.ProgressData.IsOperationRunning;

            DoStepsArea(isStep1Completed, configureProgress.ProgressData);

            GUILayout.Space(BUTTON_MARGIN);

            DoActionButtonsArea(
                isStep1Completed,
                configureProgress);

            DoNotificationArea(configureProgress.ProgressData);
        }

        void DoActionButtonsArea(
            bool isStep1Completed,
            ProgressControlsForViews configureProgress)
        {
            DoActionButton(
                isStep1Completed,
                configureProgress);
        }

        void DoActionButton(
            bool isStep1Completed,
            ProgressControlsForViews configureProgress)
        {
            if (!isStep1Completed)
            {
                DoConfigureButton(configureProgress);
                return;
            }

            if (GUILayout.Button(
                PlasticLocalization.Name.CreateWorkspaceUnityVCS.GetString(),
                GUILayout.Width(BUTTON_WIDTH)))
                mIsCreateWorkspaceButtonClicked = true;
        }

        void DoConfigureButton(ProgressControlsForViews configureProgress)
        {
            bool isAutoLoginRunning =
                ((AutoLogin.IWelcomeView)this).AutoLoginState >= AutoLogin.State.Running &&
                ((AutoLogin.IWelcomeView)this).AutoLoginState <= AutoLogin.State.ResponseSuccess;
            GUI.enabled = !(configureProgress.ProgressData.IsOperationRunning || isAutoLoginRunning);

            if (GUILayout.Button(PlasticLocalization.GetString(
                PlasticLocalization.Name.LoginOrSignUp),
                GUILayout.Width(BUTTON_WIDTH)))
            {
                if (new AutoLogin().Run(this))
                {
                    return;
                }

                // If AutoLogin failed with No Token the Login button opens the manual Cloud sign up
                ((IProgressControls)configureProgress).ShowProgress(string.Empty);

                CloudEditionWelcomeWindow.ShowWindow(mPlasticWebRestApi, this);

                GUIUtility.ExitGUI();
            }

            // If client configuration cannot be determined, keep login button default as Cloud
            // sign in window, but show Enterprise option as well
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    PlasticLocalization.Name.NeedEnterprise.GetString(),
                    UnityStyles.LinkLabel,
                    GUILayout.Width(BUTTON_WIDTH),
                    GUILayout.Height(20)))
            {
                TeamEditionConfigurationWindow.ShowWindow(mPlasticWebRestApi, this);
            }

            GUILayout.Space(BUTTON_MARGIN);

            GUI.enabled = true;
        }

        static void DoStepsArea(
            bool isStep1Completed,
            ProgressControlsForViews.Data configureProgressData)
        {
            DoLoginOrSignUpStep(isStep1Completed, configureProgressData);

            DoCreatePlasticWorkspaceStep();
        }

        static void DoLoginOrSignUpStep(
            bool isStep1Completed,
            ProgressControlsForViews.Data progressData)
        {
            Texture2D stepImage = (isStep1Completed) ? Images.GetStepOkIcon() : Images.GetStep1Icon();

            string stepText = GetConfigurationStepText(progressData, isStep1Completed);

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            GUILayout.BeginHorizontal();

            DoStepLabel(stepText, stepImage, style);

            GUILayout.EndHorizontal();
        }

        static void DoCreatePlasticWorkspaceStep()
        {
            GUILayout.BeginHorizontal();

            DoStepLabel(
                PlasticLocalization.GetString(PlasticLocalization.Name.CreateAUnityVersionControlWorkspace),
                Images.GetStep2Icon(),
                EditorStyles.label);

            GUILayout.EndHorizontal();
        }

        static void DoStepLabel(
            string text,
            Texture2D image,
            GUIStyle style)
        {
            GUILayout.Space(STEPS_LEFT_MARGIN);

            GUIContent stepLabelContent = new GUIContent(
                string.Format(" {0}", text),
                image);

            GUILayout.Label(
                stepLabelContent,
                style,
                GUILayout.Height(STEP_LABEL_HEIGHT));
        }

        static void DoTitleLabel()
        {
            GUIContent labelContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.NextStepsToSetup),
                Images.GetInfoIcon());

            GUILayout.Label(labelContent, EditorStyles.boldLabel);
        }

        static void DoNotificationArea(ProgressControlsForViews.Data configureProgressData)
        {
            if (!string.IsNullOrEmpty(configureProgressData.NotificationMessage))
                DrawProgressForViews.ForNotificationArea(configureProgressData);
        }

        static string GetConfigurationStepText(
            ProgressControlsForViews.Data progressData,
            bool isStep1Completed)
        {
            string result = PlasticLocalization.GetString(
                PlasticLocalization.Name.LoginOrSignUpUnityVersionControl);

            if (isStep1Completed)
                return result;

            if (!progressData.IsOperationRunning)
                return result;

            return string.Format("<b>{0}</b>", result);
        }

        CreateWorkspaceView GetCreateWorkspaceView()
        {
            if (mCreateWorkspaceView != null)
                return mCreateWorkspaceView;

            mCreateWorkspaceView = new CreateWorkspaceView(
                mParentWindow,
                mCreateWorkspaceListener,
                mPlasticWebRestApi,
                mPlasticApi,
                ProjectPath.Get());

            return mCreateWorkspaceView;
        }

        bool mIsCreateWorkspaceButtonClicked = false;

        CreateWorkspaceView mCreateWorkspaceView;
        readonly ProgressControlsForViews mConfigureProgress;
        readonly IPlasticAPI mPlasticApi;
        readonly IPlasticWebRestApi mPlasticWebRestApi;
        readonly CreateWorkspaceView.ICreateWorkspaceListener mCreateWorkspaceListener;
        readonly UVCSWindow mParentWindow;

        const int HORIZONTAL_MARGIN = 30;
        const int TOP_MARGIN = 20;
        const int STEPS_TOP_MARGIN = 5;
        const int STEPS_LEFT_MARGIN = 12;
        const int BUTTON_MARGIN = 10;
        const int STEP_LABEL_HEIGHT = 20;
        const int BUTTON_WIDTH = 170;
    }
}
