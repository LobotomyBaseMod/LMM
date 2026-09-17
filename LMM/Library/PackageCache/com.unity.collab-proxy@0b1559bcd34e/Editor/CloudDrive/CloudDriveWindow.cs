using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticGui;
using PlasticGui.CloudDrive.Workspaces;
using Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace;
using Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace.Welcome;
using Unity.PlasticSCM.Editor.CloudDrive.Workspaces;
using Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.CloudDrive
{
    internal class CloudDriveWindow : EditorWindow,
        CreateWorkspaceView.ICreateWorkspaceListener,
        WelcomeView.ICloudDriveWindow
    {
        internal CloudWorkspacesView CloudWorkspacesView { get { return mCloudWorkspacesView; } }

        internal void OnApplicationActivated()
        {
            mLog.Debug("OnApplicationActivated");

            if (mException != null)
                return;

            if (TestingPreference.IsShowCloudDriveWelcomeViewEnabled())
                return;

            if (mCloudWorkspacesView == null)
                return;

            if (!mCloudWorkspacesView.IsOperationRunning())
                mCloudWorkspacesView.AutoRefresh();
        }

        internal void CopyPaths(
            string organization,
            string project,
            WorkspaceInfo wkInfo,
            string[] assetPaths,
            string dstRelativePath)
        {
            if (mCloudWorkspacesView == null)
                return;

            mCloudWorkspacesView.SelectWorkspaceAndCopyPaths(
                organization, project, wkInfo, assetPaths, dstRelativePath);
        }

        void CreateWorkspaceView.ICreateWorkspaceListener.OnWorkspaceCreated(
            WorkspaceCreationData wkCreationData,
            WorkspaceInfo createdWorkspace)
        {
            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(createdWorkspace),
                TrackFeatureUseEvent.Features.UnityPackage.CloudDriveCreateWorkspaceFromWelcomeView);

            mWelcomeView = null;

            mHasCloudDriveWorkspaces = true;

            InitializeCloudDrive(
                wkCreationData.CloudServer,
                CloudProjectRepository.GetProjectName(wkCreationData.WorkspaceName),
                createdWorkspace);

            Repaint();
        }

        void WelcomeView.ICloudDriveWindow.ShowWorkspacesView()
        {
            mWelcomeView = null;

            InitializeCloudDrive();

            Repaint();
        }

        internal static bool HasCloudDriveWorkspaces()
        {
            List<WorkspaceInfo> workspaces =
                PlasticGui.Plastic.API.GetAllCloudDriveWorkspaces();

            return workspaces != null && workspaces.Count > 0;
        }

        internal WelcomeView GetWelcomeView()
        {
            if (mWelcomeView != null)
                return mWelcomeView;

            mWelcomeView = new WelcomeView(
                this,
                this,
                this,
                PlasticGui.Plastic.API,
                PlasticGui.Plastic.WebRestAPI);

            return mWelcomeView;
        }

        void OnEnable()
        {
            // Note: This log entry is not visible if the window is opened automatically
            // at startup, since the log initialization is deferred for performance
            // reasons until CloudDrivePlugin.Enable() is called
            mLog.Debug("OnEnable");

            titleContent.image = Images.GetCloudDriveViewIcon();

            mCloudDrivePlugin = CloudDrivePlugin.Instance;

            mCloudDrivePlugin.Enable();

            InitializeCloudDrive();
        }

        void OnDisable()
        {
            mLog.Debug("OnDisable");

            mCloudDrivePlugin.Disable();

            if (mException != null)
                return;

            CloseCloudDrive(this);
        }

        void OnDestroy()
        {
            mLog.Debug("OnDestroy");

            if (mException != null)
                return;

            if (!mCloudDrivePlugin.HasRunningOperation())
                return;

            bool bCloseWindow = GuiMessage.ShowQuestion(
                PlasticLocalization.Name.OperationRunning.GetString(),
                PlasticLocalization.Name.ConfirmClosingRunningOperation.GetString(),
                PlasticLocalization.Name.YesButton.GetString());

            if (bCloseWindow)
                return;

            mLog.Debug(
                "Show window again because the user doesn't want " +
                "to quit it due to there is an operation running");

            mForceToReOpen = true;

            ReOpenWindow(this);
        }

        void OnFocus()
        {
            mHasFocus = true;

            if (mException != null)
                return;

            // We don't want to auto-refresh the view when the window
            // is focused due to a right mouse button click because
            // if there is no internet connection a dialog appears and
            // it prevents being able to open the context menu in order
            // to close the Cloud Drive window
            if (Mouse.IsRightMouseButtonPressed(Event.current))
                return;

            if (mCloudWorkspacesView == null)
                return;

            if (!mCloudWorkspacesView.IsOperationRunning())
                mCloudWorkspacesView.AutoRefresh();
        }

        void OnLostFocus()
        {
            mHasFocus = false;

            EditorApplication.update += RepaintOnLostFocus;
        }

        void OnGUI()
        {
            if (mException != null)
            {
                DoExceptionErrorArea();
                return;
            }

            try
            {
                DoContentViewArea(
                    GetContentAreaRect(position),
                    GetWelcomeView(),
                    mCloudWorkspacesView,
                    mHasCloudDriveWorkspaces,
                    mHasFocus);

                WindowStatusBar.OnGUI(
                    GetStatusBarRect(position),
                    mProgressControls.ProgressData);
            }
            catch (Exception ex)
            {
                if (CheckUnityException.IsExitGUIException(ex))
                    throw;

                GUI.enabled = true;

                if (CheckUnityException.IsIMGUIPaintException(ex))
                {
                    ExceptionsHandler.LogException("InitializeCloudDrive", ex);
                    return;
                }

                mException = ex;

                DoExceptionErrorArea();

                ExceptionsHandler.HandleException("OnGUI", ex);
            }
        }

        void Update()
        {
            if (mException != null)
                return;

            try
            {
                if (mWelcomeView != null)
                    mWelcomeView.Update();

                if (mCloudWorkspacesView != null)
                    mCloudWorkspacesView.Update(this);

                if (mProgressControls != null)
                    mProgressControls.UpdateProgress(this);
            }
            catch (Exception ex)
            {
                mException = ex;

                ExceptionsHandler.HandleException("Update", ex);
            }
        }

        void InitializeCloudDrive()
        {
            InitializeCloudDrive(string.Empty, string.Empty, null);
        }

        void InitializeCloudDrive(
            string proposedCloudServer,
            string proposedProject,
            WorkspaceInfo workspaceToSelect)
        {
            if (mForceToReOpen)
            {
                mForceToReOpen = false;
                return;
            }

            try
            {
                minSize = new Vector2(
                    CloudWorkspacesView.MIN_WIDTH,
                    minSize.y);

                mHasCloudDriveWorkspaces |= HasCloudDriveWorkspaces();

                mProgressControls = new ProgressControlsForWindow();
                mCloudWorkspacesView = new CloudWorkspacesView(
                    proposedCloudServer,
                    proposedProject,
                    workspaceToSelect,
                    PlasticGui.Plastic.WebRestAPI,
                    PlasticGui.Plastic.API,
                    mProgressControls,
                    this);
            }
            catch (Exception ex)
            {
                mException = ex;

                ExceptionsHandler.HandleException("InitializeCloudDrive", ex);
            }
        }

        void ExecuteFullReload()
        {
            mException = null;

            CloseCloudDrive(this);

            InitializeCloudDrive();
        }

        void RepaintOnLostFocus()
        {
            EditorApplication.update -= RepaintOnLostFocus;

            Repaint();
        }

        void DoExceptionErrorArea()
        {
            string labelText = PlasticLocalization.GetString(
                PlasticLocalization.Name.UnexpectedError);

            string buttonText = PlasticLocalization.GetString(
                PlasticLocalization.Name.ReloadButton);

            DrawActionHelpBox.For(
                Images.GetErrorDialogIcon(), labelText, buttonText,
                ExecuteFullReload);
        }

        static void DoContentViewArea(
            Rect rect,
            WelcomeView welcomeView,
            CloudWorkspacesView cloudWorkspacesView,
            bool hasCloudDriveWorkspaces,
            bool hasFocus)
        {
            bool clientNeedsConfiguration = UnityConfigurationChecker.NeedsConfiguration();

            bool shouldShowWelcomeView =
                PlasticGuiConfig.Get().Configuration.ShowCloudDriveWelcomeView ||
                clientNeedsConfiguration ||
                !hasCloudDriveWorkspaces ||
                TestingPreference.IsShowCloudDriveWelcomeViewEnabled();

            if (shouldShowWelcomeView)
            {
                if (((AutoLogin.IWelcomeView)welcomeView).AutoLoginState == AutoLogin.State.Off)
                    ((AutoLogin.IWelcomeView)welcomeView).AutoLoginState = AutoLogin.State.Started;

                welcomeView.OnGUI(rect, clientNeedsConfiguration);
                return;
            }

            cloudWorkspacesView.OnGUI(rect, hasFocus);
        }

        static void InitializeCloudDriveOnForceToReOpen(CloudDriveWindow window)
        {
            if (window.mCloudWorkspacesView != null)
                window.mCloudWorkspacesView.OnEnable();
        }

        static void CloseCloudDrive(CloudDriveWindow window)
        {
            if (window.mCloudWorkspacesView != null)
                window.mCloudWorkspacesView.OnDisable();

            if (window.mWelcomeView != null)
                window.mWelcomeView.OnDisable();
        }

        static void ReOpenWindow(CloudDriveWindow closedWindow)
        {
            EditorWindow dockWindow = FindEditorWindow.ToDock<CloudDriveWindow>();

            CloudDriveWindow newWindow = InstantiateFrom(closedWindow);

            InitializeCloudDriveOnForceToReOpen(newWindow);

            DockEditorWindow.To(dockWindow, newWindow);

            newWindow.Show();
            newWindow.Focus();
        }

        static CloudDriveWindow InstantiateFrom(CloudDriveWindow window)
        {
            CloudDriveWindow result = Instantiate(window);
            result.mHasFocus = window.mHasFocus;
            result.mHasCloudDriveWorkspaces = window.mHasCloudDriveWorkspaces;
            result.mException = window.mException;
            result.mWelcomeView = window.mWelcomeView;
            result.mCloudWorkspacesView = window.mCloudWorkspacesView;
            result.mProgressControls = window.mProgressControls;
            result.mCloudDrivePlugin = window.mCloudDrivePlugin;
            return result;
        }

        static Rect GetContentAreaRect(Rect windowPosition)
        {
            return new Rect(
                0,
                0,
                windowPosition.width,
                windowPosition.height - UnityConstants.STATUS_BAR_HEIGHT);
        }

        static Rect GetStatusBarRect(Rect windowPosition)
        {
            return new Rect(
                0,
                windowPosition.height - UnityConstants.STATUS_BAR_HEIGHT,
                windowPosition.width,
                UnityConstants.STATUS_BAR_HEIGHT);
        }

        [SerializeField]
        bool mForceToReOpen;
        bool mHasFocus;
        bool mHasCloudDriveWorkspaces;
        Exception mException;
        WelcomeView mWelcomeView;
        CloudWorkspacesView mCloudWorkspacesView;
        ProgressControlsForWindow mProgressControls;
        CloudDrivePlugin mCloudDrivePlugin;

        static readonly ILog mLog = PlasticApp.GetLogger("CloudDriveWindow");
    }
}
