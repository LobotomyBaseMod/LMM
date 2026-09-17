using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using CodiceApp.EventTracking.Plastic;
using CodiceApp.EventTracking;
using GluonGui;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Merge;
using PlasticGui.WorkspaceWindow.NotificationBar;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetMenu;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Configuration;
using Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome;
using Unity.PlasticSCM.Editor.Developer;
using Unity.PlasticSCM.Editor.Inspector;
using Unity.PlasticSCM.Editor.Settings;
using Unity.PlasticSCM.Editor.StatusBar;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Toolbar;
using Unity.PlasticSCM.Editor.Topbar;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.Views.CreateWorkspace;
using Unity.PlasticSCM.Editor.Views.Welcome;
using Unity.PlasticSCM.Editor.WebApi;
using GluonShelvedChangesNotification = Unity.PlasticSCM.Editor.Gluon.ShelvedChangesNotification;
using IGluonWorkspaceStatusChangeListener = PlasticGui.Gluon.IWorkspaceStatusChangeListener;
using ShelvedChangesNotification = Unity.PlasticSCM.Editor.Developer.ShelvedChangesNotification;

namespace Unity.PlasticSCM.Editor
{
    internal class UVCSWindow : EditorWindow,
        CheckShelvedChanges.IAutoRefreshApplyShelveView,
        CreateWorkspaceView.ICreateWorkspaceListener
    {
        internal WelcomeView WelcomeViewForTesting { get { return mWelcomeView; } }

        internal WorkspaceWindow WorkspaceWindowForTesting { get { return mWorkspaceWindow; } }

        internal ViewSwitcher ViewSwitcherForTesting { get { return mViewSwitcher; } }

        internal IViewSwitcher IViewSwitcher { get { return mViewSwitcher; } }

        internal IMergeViewLauncher IMergeViewLauncher { get { return mViewSwitcher; } }

        internal IHistoryViewLauncher IHistoryViewLauncher { get { return mViewSwitcher; } }

        internal IWorkspaceWindow IWorkspaceWindow { get { return mWorkspaceWindow; } }

        internal IRefreshView IRefreshView { get { return mWorkspaceWindow; } }

        internal IGluonWorkspaceStatusChangeListener IWorkspaceStatusChangeListener { get { return mWorkspaceWindow; } }

        internal ViewHost ViewHost { get { return mViewHost; } }

        internal CmConnection CmConnectionForTesting { get { return CmConnection.Get(); } }

        internal IShelvedChangesUpdater ShelvedChangesUpdater { get { return mShelvedChangesUpdater; } }

        internal NotificationsArea.IIncomingChangesNotification IncomingChangesNotification { get { return mIncomingChangesNotification; } }

        internal RepositorySpec RepSpec { get { return mRepSpec; } }

        internal WelcomeView GetWelcomeView()
        {
            if (mWelcomeView != null)
                return mWelcomeView;

            mWelcomeView = new WelcomeView(
                this,
                this,
                PlasticGui.Plastic.API,
                PlasticGui.Plastic.WebRestAPI);

            return mWelcomeView;
        }

        internal PendingChangesOptionsFoldout.IAutoRefreshView GetPendingChangesView()
        {
            return mViewSwitcher != null ? mViewSwitcher.PendingChangesTab : null;
        }

        internal void UpdateWindowIcon(Texture2D windowIcon)
        {
            if (titleContent.image == windowIcon)
                return;

            titleContent.image = windowIcon;
            Repaint();
        }

        internal void UpdateIncomingChangesNotification(
            UVCSNotificationStatus.IncomingChangesStatus status,
            string infoText,
            string actionText,
            string tooltipText,
            bool hasUpdateAction)
        {
            if (mIncomingChangesNotification == null)
                return;

            if (status == UVCSNotificationStatus.IncomingChangesStatus.None)
            {
                mIncomingChangesNotification.Hide();
                Repaint();
                return;
            }

            mIncomingChangesNotification.Show(
                infoText, actionText, tooltipText, hasUpdateAction, status);
            Repaint();
        }

        internal void RefreshWorkspaceUI()
        {
            InitializePlastic();
            Repaint();

            OnFocus();
        }

        internal void RefreshPendingChangesView(PendingChangesStatus pendingChangesStatus)
        {
            if (mViewSwitcher == null)
                return;

            mViewSwitcher.RefreshPendingChangesView(pendingChangesStatus);
        }

        internal void AutoRefreshIncomingChangesView()
        {
            if (mViewSwitcher == null)
                return;

            mViewSwitcher.AutoRefreshIncomingChangesView();
        }

        internal void ShowPendingChangesView()
        {
            if (mViewSwitcher == null)
                return;

            mViewSwitcher.ShowPendingChangesView();
        }

        internal void ShowIncomingChangesView()
        {
            if (mViewSwitcher == null)
                return;

            mViewSwitcher.ShowIncomingChangesView();
        }

        internal void InitializePlastic()
        {
            if (mForceToReOpen)
            {
                mForceToReOpen = false;
                return;
            }

            try
            {
                if (UnityConfigurationChecker.NeedsConfiguration() ||
                    TestingPreference.IsShowUVCSWelcomeViewEnabled())
                    return;

                mWkInfo = FindWorkspace.InfoForApplicationPath(
                    ApplicationDataPath.Get(), PlasticGui.Plastic.API);

                if (mWkInfo == null)
                    return;

                mUVCSPlugin.EnableForWorkspace(mWkInfo);

                // UVCSPlugin.EnableForWorkspace may trigger a workspace metadata
                // upgrade that modifies the repSpec. So, we need to calculate the repSpec
                // after calling it to ensure it is up-to-date.
                mRepSpec = PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo);

                DisableVCSBuiltInPluginIfEnabled(mWkInfo.ClientPath);

                mIsGluonMode = PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo);

                mViewHost = new ViewHost();

                mViewHost.AddRefreshableView(
                    ViewType.BranchesListPopup,
                    UVCSToolbar.Controller);

                mWindowStatusBar = new WindowStatusBar();

                mViewSwitcher = new ViewSwitcher(
                    mRepSpec,
                    mWkInfo,
                    mViewHost,
                    mIsGluonMode,
                    mUVCSPlugin,
                    mUVCSPlugin.AssetStatusCache,
                    mSaveAssets,
                    mShowDownloadPlasticExeWindow,
                    mProcessExecutor,
                    mUVCSPlugin.WorkspaceOperationsMonitor,
                    mWindowStatusBar,
                    this,
                    mUVCSPlugin.NewChangesInWk,
                    mUVCSPlugin.PendingChangesUpdater,
                    mUVCSPlugin.DeveloperIncomingChangesUpdater,
                    mUVCSPlugin.GluonIncomingChangesUpdater,
                    mUVCSPlugin);

                InitializeIncomingChanges(
                    mWkInfo, mUVCSPlugin, mViewSwitcher, mIsGluonMode);

                InitializeShelvedChanges(
                    mWkInfo,
                    mRepSpec,
                    mViewSwitcher,
                    mUVCSPlugin.AssetStatusCache,
                    mShowDownloadPlasticExeWindow,
                    mIsGluonMode);

                // Create a DelayedActionBySecondsRunner to make the auto-refresh changes delayed.
                // In this way, we cover the following scenario:
                // * When Unity Editor window is activated it writes some files to its Temp
                //   folder. This causes the fswatcher to process those events.
                // * We need to wait until the fswatcher finishes processing the events,
                //   otherwise the NewChangesInWk method will return TRUE because there
                //   are pending events to process, which causes an unwanted 'get pending
                //   changes' operation when there are no new changes.
                // * So, we need to delay the auto-refresh call in order
                //   to give the fswatcher enough time to process the events.
                mDelayedAutoRefreshChangesAction = new DelayedActionBySecondsRunner(
                    () =>
                    {
                        mViewSwitcher.AutoRefreshPendingChangesView();
                        mViewSwitcher.AutoRefreshIncomingChangesView();
                    },
                    UnityConstants.AUTO_REFRESH_CHANGES_DELAYED_INTERVAL);

                mWorkspaceWindow = new WorkspaceWindow(
                    mWkInfo,
                    mViewHost,
                    mViewSwitcher,
                    mWindowStatusBar,
                    mUVCSPlugin.AssetStatusCache,
                    mViewSwitcher,
                    mUVCSPlugin.PendingChangesUpdater,
                    mUVCSPlugin.DeveloperIncomingChangesUpdater,
                    mUVCSPlugin.GluonIncomingChangesUpdater,
                    mShelvedChangesUpdater,
                    this);

                mViewSwitcher.SetWorkspaceWindow(mWorkspaceWindow);

                mTopbar.Initialize(
                    mWorkspaceWindow,
                    mIncomingChangesNotification,
                    mShelvedChangesNotification);

                mViewSwitcher.InitializeFromState(mViewSwitcherState);

                mUVCSPlugin.WorkspaceOperationsMonitor.RegisterWindow(
                    mWorkspaceWindow,
                    mViewHost);

                UnityStyles.Initialize(Repaint);

                ProjectViewUVCSAssetMenu.BuildOperations(
                    mWkInfo,
                    PlasticGui.Plastic.API,
                    mViewHost,
                    mWorkspaceWindow,
                    mViewSwitcher,
                    mViewSwitcher,
                    mViewSwitcher,
                    mViewSwitcher,
                    mUVCSPlugin.AssetStatusCache,
                    mSaveAssets,
                    mShowDownloadPlasticExeWindow,
                    mUVCSPlugin.WorkspaceOperationsMonitor,
                    mUVCSPlugin.PendingChangesUpdater,
                    mUVCSPlugin.DeveloperIncomingChangesUpdater,
                    mUVCSPlugin.GluonIncomingChangesUpdater,
                    mShelvedChangesUpdater,
                    mIsGluonMode);

                HierarchyViewAssetMenu.BuildOperations(
                    mWkInfo,
                    PlasticGui.Plastic.API,
                    mViewHost,
                    mWorkspaceWindow,
                    mViewSwitcher,
                    mViewSwitcher,
                    mViewSwitcher,
                    mViewSwitcher,
                    mUVCSPlugin.AssetStatusCache,
                    mSaveAssets,
                    mShowDownloadPlasticExeWindow,
                    mUVCSPlugin.WorkspaceOperationsMonitor,
                    mUVCSPlugin.PendingChangesUpdater,
                    mUVCSPlugin.DeveloperIncomingChangesUpdater,
                    mUVCSPlugin.GluonIncomingChangesUpdater,
                    mShelvedChangesUpdater,
                    mIsGluonMode);

                DrawInspectorOperations.BuildOperations(
                    mWkInfo,
                    PlasticGui.Plastic.API,
                    mViewHost,
                    mWorkspaceWindow,
                    mViewSwitcher,
                    mViewSwitcher,
                    mViewSwitcher,
                    mViewSwitcher,
                    mUVCSPlugin.AssetStatusCache,
                    mSaveAssets,
                    mShowDownloadPlasticExeWindow,
                    mUVCSPlugin.WorkspaceOperationsMonitor,
                    mUVCSPlugin.PendingChangesUpdater,
                    mUVCSPlugin.DeveloperIncomingChangesUpdater,
                    mUVCSPlugin.GluonIncomingChangesUpdater,
                    mShelvedChangesUpdater,
                    mIsGluonMode);

                mLastUpdateTime = EditorApplication.timeSinceStartup;

                MergeInProgress.ShowIfNeeded(mWkInfo, mViewSwitcher);

                // Note: this need to be initialized regardless of the type of the UVCS Edition installed
                InitializeCloudSubscriptionData();
                InitializeCurrentUser();

                new DelayedActionByFramesRunner(
                        DelayedRecommendToEnableManualCheckout,
                        UnityConstants.RECOMMEND_MANUAL_CHECKOUT_DELAYED_FRAMES)
                    .Run();

                if (!EditionToken.IsCloudEdition())
                    return;

                InitializeNotificationBarUpdater(
                    mWkInfo, mWindowStatusBar.NotificationBar);
            }
            catch (Exception ex)
            {
                mException = ex;

                ExceptionsHandler.HandleException("InitializePlastic", ex);
            }
        }

        internal void ExecuteFullReload()
        {
            mException = null;

            ClosePlastic(this);

            InitializePlastic();
        }

        internal void OnApplicationActivated()
        {
            mLog.Debug("OnApplicationActivated");

            if (mException != null)
                return;

            if (UnityConfigurationChecker.NeedsConfiguration() ||
                TestingPreference.IsShowUVCSWelcomeViewEnabled())
                return;

            if (mWkInfo == null)
                return;

            mShelvedChangesUpdater.Start();
            mShelvedChangesUpdater.AutoUpdate();

            ((IWorkspaceWindow)mWorkspaceWindow).UpdateTitle();
        }

        internal void OnApplicationDeactivated()
        {
            mLog.Debug("OnApplicationDeactivated");

            if (mException != null)
                return;

            if (mWkInfo == null)
                return;

            mShelvedChangesUpdater.Stop();
        }

        void CheckShelvedChanges.IAutoRefreshApplyShelveView.IfVisible(WorkspaceInfo wkInfo)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            mViewSwitcher.AutoRefreshMergeView();
        }

        void CreateWorkspaceView.ICreateWorkspaceListener.OnWorkspaceCreated(
            WorkspaceInfo wkInfo, bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mRepSpec = PlasticGui.Plastic.API.GetRepositorySpec(wkInfo);
            mIsGluonMode = isGluonMode;
            mWelcomeView = null;

            mUVCSPlugin.Enable();

            if (mIsGluonMode)
                ConfigurePartialWorkspace.AsFullyChecked(mWkInfo);

            InitializePlastic();
            Repaint();
        }

        void OnEnable()
        {
            // Note: This log entry is not visible if the window is opened automatically
            // at startup, since the log initialization is deferred for performance
            // reasons until UVCSPlugin.Enable() is called
            mLog.Debug("OnEnable");

            wantsMouseMove = true;

            if (mException != null)
                return;

            minSize = new Vector2(
                UnityConstants.UVCS_WINDOW_MIN_SIZE_WIDTH,
                UnityConstants.UVCS_WINDOW_MIN_SIZE_HEIGHT);

            mUVCSPlugin = UVCSPlugin.Instance;

            titleContent = EditorGUIUtility.TrTextContent(UnityConstants.UVCS_WINDOW_TITLE);
            UpdateWindowIcon(mUVCSPlugin.GetPluginStatusIcon());

            if (!mUVCSPlugin.ConnectionMonitor.IsConnected)
                return;

            mUVCSPlugin.Enable();

            InitializePlastic();
        }

        void OnDisable()
        {
            mLog.Debug("OnDisable");

            // We need to disable MonoFSWatcher because otherwise it hangs
            // when you move the window between monitors with different scale
            UVCSPlugin.DisableMonoFsWatcherIfNeeded();

            if (mException != null)
                return;

            ClosePlastic(this);
        }

        void OnDestroy()
        {
            mLog.Debug("OnDestroy");

            if (mException != null)
                return;

            if (mWkInfo == null)
                return;

            if (!mUVCSPlugin.HasRunningOperation())
                return;

            bool bCloseWindow = GuiMessage.ShowQuestion(
                PlasticLocalization.GetString(PlasticLocalization.Name.OperationRunning),
                PlasticLocalization.GetString(PlasticLocalization.Name.ConfirmClosingRunningOperation),
                PlasticLocalization.GetString(PlasticLocalization.Name.YesButton));

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
            mLog.Debug("OnFocus");

            if (mException != null)
                return;

            if (mWkInfo == null)
                return;

            if (!mUVCSPlugin.ConnectionMonitor.IsConnected)
                return;

            // We don't want to auto-refresh the views when the window
            // is focused due to a right mouse button click because
            // if there is no internet connection a dialog appears and
            // it prevents being able to open the context menu in order
            // to close the UVCS window
            if (Mouse.IsRightMouseButtonPressed(Event.current))
                return;

            if (!mUVCSPlugin.HasRunningOperation())
                mDelayedAutoRefreshChangesAction.Run();
        }

        void OnGUI()
        {
            if (!mUVCSPlugin.ConnectionMonitor.IsConnected)
            {
                DoNotConnectedArea(mUVCSPlugin);
                return;
            }

            if (mException != null)
            {
                DoExceptionErrorArea();
                return;
            }

            try
            {
                bool clientNeedsConfiguration =
                    UnityConfigurationChecker.NeedsConfiguration() ||
                    TestingPreference.IsShowUVCSWelcomeViewEnabled();

                WelcomeView welcomeView = GetWelcomeView();

                if (clientNeedsConfiguration && ((AutoLogin.IWelcomeView)welcomeView).AutoLoginState == AutoLogin.State.Off)
                {
                    ((AutoLogin.IWelcomeView)welcomeView).AutoLoginState = AutoLogin.State.Started;
                }

                if (NeedsToDisplayWelcomeView(clientNeedsConfiguration, mWkInfo))
                {
                    welcomeView.OnGUI(clientNeedsConfiguration);
                    return;
                }

                //TODO: Codice - beta: hide the switcher until the update dialog is implemented
                //DrawGuiModeSwitcher.ForMode(
                //    isGluonMode, plasticClient, changesTreeView, editorWindow);

                mTopbar.OnGUI(
                    mWkInfo,
                    mRepSpec,
                    mShowDownloadPlasticExeWindow,
                    mProcessExecutor,
                    mWorkspaceWindow.WorkingObjectName,
                    mWorkspaceWindow.WorkingObjectFullSpec,
                    mWorkspaceWindow.WorkingObjectComment,
                    mWorkspaceWindow.WorkingObjectType,
                    mIsGluonMode,
                    mIsCloudOrganization,
                    mIsUnityOrganization,
                    mIsUGOSubscription,
                    PackageInfo.NAME,
                    PackageInfo.Data);

                DoContent();

                if (mWorkspaceWindow.IsOperationInProgress())
                    DrawProgressForOperations.For(
                        mWorkspaceWindow, mWorkspaceWindow.Progress,
                        position.width);

                mWindowStatusBar.OnGUI();
            }
            catch (Exception ex)
            {
                if (CheckUnityException.IsExitGUIException(ex))
                    throw;

                GUI.enabled = true;

                if (CheckUnityException.IsIMGUIPaintException(ex))
                {
                    ExceptionsHandler.LogException("UVCSWindow", ex);
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

            if (mWkInfo == null)
                return;

            try
            {
                double currentUpdateTime = EditorApplication.timeSinceStartup;
                double elapsedSeconds = currentUpdateTime - mLastUpdateTime;

                mViewSwitcher.Update();
                mWorkspaceWindow.OnParentUpdated(elapsedSeconds);

                if (mWelcomeView != null)
                    mWelcomeView.Update();

                mLastUpdateTime = currentUpdateTime;
            }
            catch (Exception ex)
            {
                mException = ex;

                ExceptionsHandler.HandleException("Update", ex);
            }
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

        void InitializeCloudSubscriptionData()
        {
            mIsCloudOrganization = false;
            mIsUnityOrganization = false;
            mIsUGOSubscription = false;

            if (mRepSpec == null)
                return;

            mIsCloudOrganization = PlasticGui.Plastic.API.IsCloud(mRepSpec.Server);

            if (!mIsCloudOrganization)
                return;

            mIsUnityOrganization = OrganizationsInformation.IsUnityOrganization(mRepSpec.Server);

            string organizationName = ServerOrganizationParser.GetOrganizationFromServer(mRepSpec.Server);

            Task.Run(
                () =>
                {
                    string authToken = AuthToken.GetForServer(mRepSpec.Server);

                    if (string.IsNullOrEmpty(authToken))
                        return null;

                    return WebRestApiClient.PlasticScm.GetSubscriptionDetails(
                        organizationName, authToken);
                }).ContinueWith(
                t =>
                {
                    if (t.Result == null)
                    {
                        mLog.DebugFormat(
                            "Error getting Subscription details for organization {0}",
                            organizationName);
                        return;
                    }

                    mIsUGOSubscription = t.Result.OrderSource == UGO_ORDER_SOURCE;
                });
        }

        void InitializeCurrentUser()
        {
            PlasticThreadPool.Run(new WaitCallback(delegate
            {
                try
                {
                    SetCurrentUser(PlasticGui.Plastic.
                        API.GetCurrentUser(mRepSpec.Server));
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat("Error loading the current user: {0}", ex.Message);
                    mLog.DebugFormat("Stack trace: {0}", ex.StackTrace);
                }
            }));
        }

        void InitializeIncomingChanges(
            WorkspaceInfo wkInfo,
            UVCSPlugin uvcsPlugin,
            ViewSwitcher viewSwitcher,
            bool bIsGluonMode)
        {
            mIncomingChangesNotification = bIsGluonMode ?
                new Gluon.IncomingChangesNotification(wkInfo, viewSwitcher) :
                new IncomingChangesNotification(wkInfo, viewSwitcher);
        }

        void InitializeShelvedChanges(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ViewSwitcher viewSwitcher,
            IAssetStatusCache assetStatusCache,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            bool bIsGluonMode)
        {
            mShelvedChangesNotification = bIsGluonMode ?
                new GluonShelvedChangesNotification(
                    wkInfo,
                    repSpec,
                    viewSwitcher,
                    assetStatusCache,
                    showDownloadPlasticExeWindow,
                    this) :
                new ShelvedChangesNotification(
                    wkInfo,
                    repSpec,
                    viewSwitcher,
                    this);

            mShelvedChangesUpdater = new ShelvedChangesUpdater(
                wkInfo,
                new UnityPlasticTimerBuilder(),
                this,
                new CalculateShelvedChanges(new BaseCommandsImpl()),
                mShelvedChangesNotification);

            viewSwitcher.SetShelvedChanges(mShelvedChangesUpdater, mShelvedChangesNotification);
            mShelvedChangesNotification.SetShelvedChangesUpdater(mShelvedChangesUpdater);

            mShelvedChangesUpdater.Start();
        }

        void InitializeNotificationBarUpdater(
            WorkspaceInfo wkInfo,
            INotificationBar notificationBar)
        {
            mNotificationBarUpdater = new NotificationBarUpdater(
                notificationBar,
                PlasticGui.Plastic.WebRestAPI,
                new UnityPlasticTimerBuilder(),
                new NotificationBarUpdater.NotificationBarConfig(),
                BuildEventModel.CurrentApplicationString,
                PackageInfo.Data.Version,
                BuildEvent.CurrentPlatform.ToString());
            mNotificationBarUpdater.Start();
            mNotificationBarUpdater.SetWorkspace(wkInfo);
        }

        void DelayedRecommendToEnableManualCheckout()
        {
            RecommendToEnableManualCheckout.IfHasLockRulesFor(mRepSpec);
        }

        void SetCurrentUser(ResolvedUser currentUser)
        {
            lock (mCurrentUserLock)
            {
                mCurrentUser = currentUser;
            }
        }

        ResolvedUser GetCurrentUser()
        {
            lock (mCurrentUserLock)
            {
                return mCurrentUser;
            }
        }

        static void DoNotConnectedArea(UVCSPlugin uvcsPlugin)
        {
            string labelText = PlasticLocalization.GetString(
                PlasticLocalization.Name.NotConnectedTryingToReconnect);

            string buttonText = PlasticLocalization.GetString(
                PlasticLocalization.Name.TryNowButton);

            GUI.enabled = !uvcsPlugin.ConnectionMonitor.IsTryingReconnection;

            DrawActionHelpBox.For(
                Images.GetInfoDialogIcon(), labelText, buttonText,
                uvcsPlugin.ConnectionMonitor.CheckConnection);

            GUI.enabled = true;
        }

        void DoContent()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            mViewSwitcher.SidebarButtonsGUI();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            mViewSwitcher.SelectedViewGUI(GetCurrentUser());
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        static void DisableVCSBuiltInPluginIfEnabled(string projectPath)
        {
            if (!VCSBuiltInPlugin.IsEnabled())
                return;

            VCSBuiltInPlugin.Disable();

            mLog.DebugFormat("Disabled VCS Built-In Plugin on Project: {0}",
                projectPath);
        }

        static void DisposeShelvedChanges(UVCSWindow window)
        {
            if (window.mShelvedChangesUpdater == null)
                return;

            window.mShelvedChangesUpdater.Dispose();
            window.mShelvedChangesUpdater = null;
        }

        static void DisposeNotificationBarUpdater(UVCSWindow window)
        {
            if (window.mNotificationBarUpdater == null)
                return;

            window.mNotificationBarUpdater.Dispose();
            window.mNotificationBarUpdater = null;
        }

        static void InitializePlasticOnForceToReOpen(UVCSWindow window)
        {
            if (window.mWkInfo == null)
                return;

            window.mViewSwitcher.OnEnable();

            window.InitializeIncomingChanges(
                window.mWkInfo,
                window.mUVCSPlugin,
                window.mViewSwitcher,
                window.mIsGluonMode);
            window.InitializeShelvedChanges(
                window.mWkInfo,
                window.mRepSpec,
                window.mViewSwitcher,
                window.mUVCSPlugin.AssetStatusCache,
                window.mShowDownloadPlasticExeWindow,
                window.mIsGluonMode);

            window.mUVCSPlugin.WorkspaceOperationsMonitor.RegisterWindow(
                window.mWorkspaceWindow,
                window.mViewHost);

            if (!EditionToken.IsCloudEdition())
                return;

            window.InitializeNotificationBarUpdater(
                window.mWkInfo,
                window.mWindowStatusBar.NotificationBar);
        }

        static void ClosePlastic(UVCSWindow window)
        {
            if (window.mViewSwitcher != null)
                window.mViewSwitcher.OnDisable();

            if (window.mUVCSPlugin.WorkspaceOperationsMonitor != null)
                window.mUVCSPlugin.WorkspaceOperationsMonitor.UnRegisterWindow();

            DisposeShelvedChanges(window);

            DisposeNotificationBarUpdater(window);
        }

        static void ReOpenWindow(UVCSWindow closedWindow)
        {
            EditorWindow dockWindow = FindEditorWindow.ToDock<UVCSWindow>();

            UVCSWindow newWindow = InstantiateFrom(closedWindow);

            InitializePlasticOnForceToReOpen(newWindow);

            DockEditorWindow.To(dockWindow, newWindow);

            newWindow.Show();
            newWindow.Focus();
        }

        static bool NeedsToDisplayWelcomeView(
            bool clientNeedsConfiguration,
            WorkspaceInfo wkInfo)
        {
            if (clientNeedsConfiguration)
                return true;

            if (wkInfo == null)
                return true;

            return false;
        }

        static UVCSWindow InstantiateFrom(UVCSWindow window)
        {
            UVCSWindow result = Instantiate(window);
            result.mIsGluonMode = window.mIsGluonMode;
            result.mIsCloudOrganization = window.mIsCloudOrganization;
            result.mIsUnityOrganization = window.mIsUnityOrganization;
            result.mIsUGOSubscription = window.mIsUGOSubscription;
            result.mLastUpdateTime = window.mLastUpdateTime;
            result.mWkInfo = window.mWkInfo;
            result.mRepSpec = window.mRepSpec;
            result.mCurrentUser = window.mCurrentUser;
            result.mViewSwitcherState = window.mViewSwitcherState;
            result.mException = window.mException;
            result.mUVCSPlugin = window.mUVCSPlugin;
            result.mWelcomeView = window.mWelcomeView;
            result.mViewHost = window.mViewHost;
            result.mViewSwitcher = window.mViewSwitcher;
            result.mWorkspaceWindow = window.mWorkspaceWindow;
            result.mWindowStatusBar = window.mWindowStatusBar;
            result.mIncomingChangesNotification = window.mIncomingChangesNotification;
            result.mShelvedChangesNotification = window.mShelvedChangesNotification;
            result.mShelvedChangesUpdater = window.mShelvedChangesUpdater;
            result.mNotificationBarUpdater = window.mNotificationBarUpdater;
            result.mDelayedAutoRefreshChangesAction = window.mDelayedAutoRefreshChangesAction;
            return result;
        }

        object mCurrentUserLock = new object();

        [SerializeField]
        bool mForceToReOpen;
        bool mIsGluonMode;
        bool mIsCloudOrganization;
        bool mIsUnityOrganization;
        bool mIsUGOSubscription;
        double mLastUpdateTime = 0f;
        [NonSerialized]
        WorkspaceInfo mWkInfo;
        RepositorySpec mRepSpec;
        ResolvedUser mCurrentUser;
        SerializableViewSwitcherState mViewSwitcherState = new SerializableViewSwitcherState();
        Exception mException;
        NotificationBarUpdater mNotificationBarUpdater;
        ShelvedChangesUpdater mShelvedChangesUpdater;
        NotificationsArea.IShelvedChangesNotification mShelvedChangesNotification;
        NotificationsArea.IIncomingChangesNotification mIncomingChangesNotification;
        WindowStatusBar mWindowStatusBar;
        Topbar.Topbar mTopbar = new Topbar.Topbar();
        ViewSwitcher mViewSwitcher;
        WorkspaceWindow mWorkspaceWindow;
        DelayedActionBySecondsRunner mDelayedAutoRefreshChangesAction;
        ViewHost mViewHost;
        WelcomeView mWelcomeView;
        UVCSPlugin mUVCSPlugin;

        ISaveAssets mSaveAssets = new SaveAssets();
        LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow =
            new LaunchTool.ShowDownloadPlasticExeWindow();
        LaunchTool.IProcessExecutor mProcessExecutor =
            new LaunchTool.ProcessExecutor();

        const string UGO_ORDER_SOURCE = "UGO";

        static readonly ILog mLog = PlasticApp.GetLogger("UVCSWindow");
    }
}
