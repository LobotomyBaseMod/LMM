using System.Threading;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.Views.Welcome;

namespace Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace.Welcome
{
    internal class WelcomeView : DownloadAndInstallOperation.INotify, AutoLogin.IWelcomeView
    {
        AutoLogin.State AutoLogin.IWelcomeView.AutoLoginState { get; set; }

        internal interface ICloudDriveWindow
        {
            void ShowWorkspacesView();
        }

        internal WelcomeView(
            EditorWindow parentWindow,
            CreateWorkspaceView.ICreateWorkspaceListener listener,
            ICloudDriveWindow cloudDriveWindow,
            IPlasticAPI plasticApi,
            IPlasticWebRestApi plasticWebRestApi)
        {
            mParentWindow = parentWindow;
            mCreateWorkspaceListener = listener;
            mCloudDriveWindow = cloudDriveWindow;
            mPlasticApi = plasticApi;
            mPlasticWebRestApi = plasticWebRestApi;

            mConfigureProgress = new ProgressControlsForViews();
            ((AutoLogin.IWelcomeView)this).AutoLoginState = AutoLogin.State.Off;

            CheckCloudDrive();

            if (mIsCloudDriveRunning)
                return;

            mCheckCloudDriveTimer = new UnityPlasticTimer(
                UnityConstants.CHECK_CLOUD_DRIVE_EXE_DELAYED_INTERVAL_MS,
                CheckCloudDrive);
            mCheckCloudDriveTimer.Start();
        }

        internal void Update()
        {
            if (mCreateWorkspaceView != null)
                mCreateWorkspaceView.Update();

            if (mIsDownloading)
                mConfigureProgress.UpdateProgress(mParentWindow);
        }

        internal void OnDisable()
        {
            if (mCheckCloudDriveTimer != null)
                mCheckCloudDriveTimer.Stop();
        }

        internal void OnGUI(Rect rect, bool clientNeedsConfiguration)
        {
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();

            GUILayout.Space(HORIZONTAL_MARGIN);

            DoContentViewArea(
                clientNeedsConfiguration,
                mIsCreateWorkspaceButtonClicked);

            GUILayout.Space(HORIZONTAL_MARGIN);

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void DownloadAndInstallOperation.INotify.InstallationStarted()
        {
            mIsInstalling = true;
        }

        void DownloadAndInstallOperation.INotify.InstallationFinished()
        {
            mIsInstalling = false;
        }

        void DownloadAndInstallOperation.INotify.DownloadStarted()
        {
            mIsDownloading = true;
        }

        void DownloadAndInstallOperation.INotify.DownloadFinished()
        {
            mIsDownloading = false;
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

            if (isCreateWorkspaceButtonClicked ||
                !PlasticGuiConfig.Get().Configuration.ShowCloudDriveWelcomeView)
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
            using (new GUILayout.VerticalScope(GUILayout.MaxWidth(MAX_WIDTH)))
            {
                DoTitleArea();

                DoSubTitleArea();

                if (!clientNeedsConfiguration)
                    mIsStep1Completed = true;
                bool isStep2Completed = mIsCloudDriveExeAvailable && !mIsInstalling;
                bool isStep3Completed = mIsCloudDriveRunning;

                DoStepsArea(
                    mIsStep1Completed,
                    isStep2Completed,
                    isStep3Completed,
                    configureProgress.ProgressData);

                GUILayout.Space(BUTTON_MARGIN);

                using (new GUILayout.HorizontalScope())
                {
                    GUI.enabled = !configureProgress.IsOperationRunning();

                    DoActionButton(
                        mIsStep1Completed,
                        isStep2Completed,
                        isStep3Completed,
                        configureProgress);

                    GUI.enabled = true;

                    GUILayout.Space(BUTTON_MARGIN);

                    DoNotificationArea(configureProgress.ProgressData, mIsDownloading);
                }
            }
        }

        void DoActionButton(
            bool isStep1Completed,
            bool isStep2Completed,
            bool isStep3Completed,
            ProgressControlsForViews configureProgress)
        {
            if (!isStep1Completed)
            {
                DoConfigureButton(configureProgress);
                return;
            }

            if (!isStep2Completed)
            {
                if (GUILayout.Button(
                    PlasticLocalization.Name.InstallCloudDriveButton.GetString(),
                    GUILayout.Width(BUTTON_WIDTH)))
                {
                    DownloadAndInstallOperation.Run(
                        Edition.Cloud,
                        configureProgress,
                        new CancellationToken(),
                        this);
                }
                return;
            }

            if (!isStep3Completed)
            {
                if (GUILayout.Button(
                    PlasticLocalization.Name.StartCloudDriveButton.GetString(),
                    GUILayout.Width(BUTTON_WIDTH)))
                {
                    ProcessExecutor.Execute(
                        GetProcessName.ForUnityCloudDrive(),
                        string.Empty,
                        false,
                        false);
                    ((IProgressControls)configureProgress).ShowProgress(
                        PlasticLocalization.Name.StartCloudDriveProgress.GetString());
                }
                return;
            }

            if (GUILayout.Button(
                    PlasticLocalization.Name.GetStartedButton.GetString(),
                    GUILayout.Width(BUTTON_WIDTH)))
            {
                PlasticGuiConfig.Get().Configuration.ShowCloudDriveWelcomeView = false;
                PlasticGuiConfig.Get().Save();

                if (CloudDriveWindow.HasCloudDriveWorkspaces())
                {
                    mCloudDriveWindow.ShowWorkspacesView();
                    return;
                }

                mIsCreateWorkspaceButtonClicked = true;
            }
        }

        void DoConfigureButton(ProgressControlsForViews configureProgress)
        {
            bool isAutoLoginRunning =
                ((AutoLogin.IWelcomeView)this).AutoLoginState >= AutoLogin.State.Running &&
                ((AutoLogin.IWelcomeView)this).AutoLoginState <= AutoLogin.State.ResponseSuccess;
            GUI.enabled = !(configureProgress.IsOperationRunning() || isAutoLoginRunning);

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

            GUILayout.FlexibleSpace();

            GUI.enabled = true;
        }

        void CheckCloudDrive()
        {
            mIsCloudDriveExeAvailable = IsExeAvailable.ForCloudDrive();

            if (!mIsCloudDriveExeAvailable)
                return;

            mIsCloudDriveRunning = UnityCloudDriveProcess.IsRunning();

            if (!mIsCloudDriveRunning)
                return;

            if (mCheckCloudDriveTimer != null)
                mCheckCloudDriveTimer.Stop();

            ((IProgressControls)mConfigureProgress).HideProgress();
        }

        CreateWorkspaceView GetCreateWorkspaceView()
        {
            if (mCreateWorkspaceView != null)
                return mCreateWorkspaceView;

            mCreateWorkspaceView = new CreateWorkspaceView(
                mPlasticWebRestApi,
                mPlasticApi,
                mCreateWorkspaceListener,
                mParentWindow);

            return mCreateWorkspaceView;
        }

        static void DoTitleArea()
        {
            using (new GUILayout.HorizontalScope())
            {
                Rect imageRect = GUILayoutUtility.GetRect(
                    32, 32, GUILayout.Width(32), GUILayout.Height(32));
                GUI.DrawTexture(imageRect, Images.GetCloudIcon(), ScaleMode.ScaleToFit);

                GUILayout.Label(
                    PlasticLocalization.Name.UnityCloudDriveTitle.GetString(),
                    UnityStyles.CloudDrive.Title);

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(10);

            Rect result = GUILayoutUtility.GetRect(MAX_WIDTH, 1);
            EditorGUI.DrawRect(result, UnityStyles.Colors.BarBorder);

            GUILayout.Space(10);
        }

        static void DoSubTitleArea()
        {
            GUILayout.Label(
                PlasticLocalization.Name.GetStartedTitle.GetString(),
                UnityStyles.Dialog.Title);

            GUILayout.Space(5);

            GUILayout.Label(
                PlasticLocalization.Name.GetStartedExplanation.GetString(),
                UnityStyles.Paragraph);

            GUILayout.Space(10);
        }

        static void DoStepsArea(
            bool isStep1Completed,
            bool isStep2Completed,
            bool isStep3Completed,
            ProgressControlsForViews.Data configureProgressData)
        {
            DoStep(
                isStep1Completed,
                Images.GetStep1Icon(),
                GetConfigurationStepText(
                    PlasticLocalization.Name.LoginCloudDriveStep.GetString(),
                    configureProgressData,
                    !isStep1Completed));

            DoStep(
                isStep2Completed,
                Images.GetStep2Icon(),
                GetConfigurationStepText(
                    PlasticLocalization.Name.DownloadCloudDriveStep.GetString(),
                    configureProgressData,
                    isStep1Completed && !isStep2Completed));

            DoStep(
                isStep3Completed,
                Images.GetStep3Icon(),
                GetConfigurationStepText(
                    PlasticLocalization.Name.StartCloudDriveStep.GetString(),
                    configureProgressData,
                    isStep2Completed && !isStep3Completed));
        }

        static void DoStep(
            bool isStepCompleted,
            Texture2D stepIcon,
            string stepText)
        {
            Texture2D stepImage = isStepCompleted ? Images.GetStepOkIcon() : stepIcon;

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            GUILayout.BeginHorizontal();

            DoStepLabel(stepText, stepImage, style);

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

        static void DoNotificationArea(
            ProgressControlsForViews.Data configureProgressData,
            bool isDownloading)
        {
            if (!configureProgressData.IsOperationRunning)
                return;

            if (isDownloading)
            {
                DrawProgressForViews.ForDeterminateProgressBar(configureProgressData);
                return;
            }

            DrawProgressForViews.ForIndeterminateProgressSpinner(configureProgressData);
        }

        static string GetConfigurationStepText(
            string text,
            ProgressControlsForViews.Data progressData,
            bool isCurrentStep)
        {
            if (!isCurrentStep)
                return text;

            if (!progressData.IsOperationRunning)
                return text;

            return string.Format("<b>{0}</b>", text);
        }

        bool mIsCloudDriveRunning;
        bool mIsCloudDriveExeAvailable;
        bool mIsCreateWorkspaceButtonClicked;
        bool mIsInstalling;
        bool mIsDownloading;
        bool mIsStep1Completed;

        CreateWorkspaceView mCreateWorkspaceView;

        readonly IPlasticTimer mCheckCloudDriveTimer;

        readonly ProgressControlsForViews mConfigureProgress;
        readonly IPlasticAPI mPlasticApi;
        readonly IPlasticWebRestApi mPlasticWebRestApi;
        readonly CreateWorkspaceView.ICreateWorkspaceListener mCreateWorkspaceListener;
        readonly ICloudDriveWindow mCloudDriveWindow;
        readonly EditorWindow mParentWindow;

        const int HORIZONTAL_MARGIN = 30;
        const int TOP_MARGIN = 20;
        const int STEPS_LEFT_MARGIN = 12;
        const int BUTTON_MARGIN = 10;
        const int STEP_LABEL_HEIGHT = 20;
        const int BUTTON_WIDTH = 170;
        const float MAX_WIDTH = 800;
    }
}
