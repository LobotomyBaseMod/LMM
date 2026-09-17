using System;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.Client.Common.Connection;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.FsNodeReaders;
using Codice.Client.Common.FsNodeReaders.Watcher;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.CM.WorkspaceServer;
using Codice.LogWrapper;
using Codice.Utils;
using CodiceApp.EventTracking;
using MacUI;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.AssetMenu;
using Unity.PlasticSCM.Editor.AssetsOverlays;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Inspector;
using Unity.PlasticSCM.Editor.SceneView;
using Unity.PlasticSCM.Editor.Settings;
using Unity.PlasticSCM.Editor.Toolbar;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.Views.BranchExplorer;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Options;
using GluonCheckIncomingChanges = PlasticGui.Gluon.WorkspaceWindow.CheckIncomingChanges;
using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;

namespace Unity.PlasticSCM.Editor
{
    internal class UVCSPlugin :
        CheckIncomingChanges.IAutoRefreshIncomingChangesView,
        CheckIncomingChanges.IUpdateIncomingChanges,
        GluonCheckIncomingChanges.IAutoRefreshIncomingChangesView,
        GluonCheckIncomingChanges.IUpdateIncomingChanges,
        CheckPendingChanges.IPendingChangesView,
        CheckPendingChanges.IUpdatePendingChanges
    {
        internal static UVCSPlugin Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new UVCSPlugin();

                return mInstance;
            }
        }

        internal event Action OnNotificationStatusUpdated;

        internal IAssetStatusCache AssetStatusCache
        {
            get { return mAssetStatusCache; }
        }

        internal INewChangesInWk NewChangesInWk
        {
            get { return mNewChangesInWk; }
        }

        internal WorkspaceOperationsMonitor WorkspaceOperationsMonitor
        {
            get { return mWorkspaceOperationsMonitor; }
        }

        internal UVCSConnectionMonitor ConnectionMonitor
        {
            get { return mUVCSConnectionMonitor; }
        }

        internal PendingChangesUpdater PendingChangesUpdater
        {
            get { return mPendingChangesUpdater; }
        }

        internal IncomingChangesUpdater DeveloperIncomingChangesUpdater
        {
            get { return mDeveloperIncomingChangesUpdater; }
        }

        internal GluonIncomingChangesUpdater GluonIncomingChangesUpdater
        {
            get { return mGluonIncomingChangesUpdater; }
        }

        public UVCSProjectSettingsProvider ActiveUVCSSettingsProvider
        {
            get { return mActiveUVCSSettingsProvider; }
            set { mActiveUVCSSettingsProvider = value; }
        }

        internal static void InitializeIfNeeded()
        {
            if (!FindWorkspace.HasWorkspace(ApplicationDataPath.Get()))
                return;

            if (!UVCSPluginIsEnabledPreference.IsEnabled())
                return;

            new DelayedActionBySecondsRunner(
                    Instance.Enable,
                    UnityConstants.PLUGIN_DELAYED_INITIALIZE_INTERVAL)
                .Run();
        }

        internal static void EnableMonoFsWatcherIfNeeded()
        {
            if (PlatformIdentifier.IsMac())
                return;

            MonoFileSystemWatcher.IsEnabled = true;
        }

        internal static void DisableMonoFsWatcherIfNeeded()
        {
            if (PlatformIdentifier.IsMac())
                return;

            MonoFileSystemWatcher.IsEnabled = false;
        }

        internal bool IsEnabled()
        {
            return mIsEnabled;
        }

        internal bool HasRunningOperation()
        {
            if (IsOperationInProgressInWorkspaceWindow())
                return true;

            if (mWkInfo == null)
                return false;

            return TransactionManager.Get().ExistsAnyWorkspaceTransaction(mWkInfo);
        }

        internal Texture2D GetPluginStatusIcon()
        {
            return mNotificationStatus.GetIcon();
        }

        internal void Enable()
        {
            UVCSToolbar.Controller.Enable();

            if (mIsEnabled)
                return;

            mIsEnabled = true;

            PlasticApp.InitializeIfNeeded();

            mLog.Debug("Enable");

            SetupFsWatcher();

            PlasticApp.Enable();

            if (TestingPreference.IsShowUVCSWelcomeViewEnabled())
                return;

            WorkspaceInfo wkInfo = FindWorkspace.InfoForApplicationPath(
                ApplicationDataPath.Get(), PlasticGui.Plastic.API);

            if (wkInfo == null)
                return;

            EnableForWorkspace(wkInfo);
        }

        internal void EnableForWorkspace(WorkspaceInfo wkInfo)
        {
            if (mIsEnabledForWorkspace)
                return;

            mIsEnabledForWorkspace = true;
            mWkInfo = wkInfo;

            mLog.Debug("EnableForWorkspace " + mWkInfo.ClientPath);

            InitIgnoredRules.ForUnityWorkspace(mWkInfo);

            mIsGluonMode = PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo);

            PlasticGui.Plastic.API.UpgradeWorkspaceMetadataAfterOrgUnificationNeeded(mWkInfo);

            ProjectLoadedCounter.IncrementOnceOnEnable();

            if (!StoreEvent.IsDisabled)
            {
                mPingEventLoop = new PingEventLoop(
                    BuildGetEventExtraInfoFunction.ForPingEvent());
                mPingEventLoop.SetWorkspace(mWkInfo);
                mPingEventLoop.Start();
            }

            HandleCredsAliasAndServerCert.InitializeHostUnreachableExceptionListener(
                mUVCSConnectionMonitor);

            InitializePendingChangesUpdater(mWkInfo);
            InitializeIncomingChangesUpdater(mWkInfo, mIsGluonMode);

            // delay the auto-refresh call in order to give the fswatcher
            // enough time to process the events after regaining the application focus
            // see also UVCSWindow.mDelayedAutoRefreshChangesAction
            mDelayedAutoRefreshChangesAction = new DelayedActionBySecondsRunner(
                () =>
                {
                    if (mPendingChangesUpdater != null)
                    {
                        mPendingChangesUpdater.Start();
                        mPendingChangesUpdater.AutoUpdate();
                    }

                    IncomingChanges.LaunchUpdater(
                        mDeveloperIncomingChangesUpdater,
                        mGluonIncomingChangesUpdater);
                },
                UnityConstants.AUTO_REFRESH_CHANGES_DELAYED_INTERVAL);

            mAssetStatusCache = new AssetStatusCache(mWkInfo, mIsGluonMode);

            UVCSAssetsProcessor uvcsAssetsProcessor = new UVCSAssetsProcessor();

            mWorkspaceOperationsMonitor = BuildWorkspaceOperationsMonitor(
                mWkInfo,
                mAssetStatusCache,
                uvcsAssetsProcessor,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mGluonIncomingChangesUpdater,
                mIsGluonMode);
            mWorkspaceOperationsMonitor.Start();

            UnityCloudProjectLinkMonitor.CheckCloudProjectAlignmentAsync(mWkInfo);

            AssetsProcessors.Enable(
                mWkInfo.ClientPath, uvcsAssetsProcessor, mAssetStatusCache);
            ProjectViewUVCSAssetMenu.Enable(
                mWkInfo, PlasticGui.Plastic.API, mAssetStatusCache);
            DrawProjectOverlay.Enable(
                mWkInfo.ClientPath, mAssetStatusCache);
            DrawInspectorOperations.Enable(
                mWkInfo, PlasticGui.Plastic.API, mAssetStatusCache);
            DrawSceneOperations.Enable(
                mWkInfo, PlasticGui.Plastic.API,
                mWorkspaceOperationsMonitor, mAssetStatusCache);
            HierarchyExtensions.Enable(
                mWkInfo, PlasticGui.Plastic.API, mAssetStatusCache);

            EnsureServerConnectionAsync(mWkInfo, mUVCSConnectionMonitor);

            UVCSToolbar.Controller.SetWorkspace(wkInfo, mIsGluonMode);
        }

        internal void Disable()
        {
            UVCSToolbar.Controller.Disable();

            if (!mIsEnabled)
                return;

            mLog.Debug("Disable");

            mIsEnabled = false;

            DisableForWorkspace();

            PlasticApp.DisposeIfNeeded();
        }

        internal void Shutdown()
        {
            mLog.Debug("Shutdown");

            WorkspaceFsNodeReaderCachesCleaner.Shutdown();

            HandleCredsAliasAndServerCert.CleanHostUnreachableExceptionListener();
            mUVCSConnectionMonitor.Stop();

            Disable();
        }

        internal void OnApplicationActivated()
        {
            mLog.Debug("OnApplicationActivated");

            EnableMonoFsWatcherIfNeeded();

            if (!mUVCSConnectionMonitor.IsConnected)
                return;

            Reload.IfWorkspaceConfigChanged(
                PlasticGui.Plastic.API, mWkInfo, mIsGluonMode,
                ExecuteFullReload);

            if (mWkInfo == null)
                return;

            if (mDelayedAutoRefreshChangesAction != null && !HasRunningOperation())
                mDelayedAutoRefreshChangesAction.Run();

            RefreshAsset.VersionControlCache(mAssetStatusCache);

            UVCSToolbar.Controller.RefreshWorkspaceWorkingInfo();

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.OnApplicationActivated();
        }

        internal void OnApplicationDeactivated()
        {
            mLog.Debug("OnApplicationDeactivated");

            DisableMonoFsWatcherIfNeeded();

            if (mWkInfo == null)
                return;

            if (mPendingChangesUpdater != null)
                mPendingChangesUpdater.Stop();

            IncomingChanges.StopUpdater(
                mDeveloperIncomingChangesUpdater,
                mGluonIncomingChangesUpdater);

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.OnApplicationDeactivated();
        }

        internal void OnSceneOpened()
        {
            mLog.Debug("OnSceneOpened");

            Texture2D statusIcon = GetPluginStatusIcon();

            UVCSToolbar.Controller.UpdateIcon(statusIcon);

            UpdateUVCSWindowIcon(GetWindowIfOpened.UVCS(), statusIcon);
        }

        internal bool OnEditorWantsToQuit()
        {
            mLog.Debug("OnEditorWantsToQuit");

            if (!HasRunningOperation())
                return true;

            return GuiMessage.ShowQuestion(
                PlasticLocalization.GetString(PlasticLocalization.Name.OperationRunning),
                PlasticLocalization.GetString(PlasticLocalization.Name.ConfirmClosingRunningOperation),
                PlasticLocalization.GetString(PlasticLocalization.Name.YesButton));
        }

        bool CheckPendingChanges.IPendingChangesView.HasUnsavedChanges(WorkspaceInfo wkInfo)
        {
            return false;
        }

        void CheckPendingChanges.IPendingChangesView.Refresh(
            WorkspaceInfo wkInfo, PendingChangesStatus pendingChangesStatus)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.RefreshPendingChangesView(pendingChangesStatus);
        }

        void CheckPendingChanges.IUpdatePendingChanges.Show(
            WorkspaceInfo wkInfo, PendingChangesStatus pendingChangesStatus)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UpdateNotificationStatusForPendingChanges(
                mNotificationStatus,
                GetWindowIfOpened.UVCS(),
                OnNotificationStatusUpdated,
                pendingChangesStatus.WorkspaceStatusResult);
        }

        void CheckPendingChanges.IUpdatePendingChanges.Hide(WorkspaceInfo wkInfo)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UpdateNotificationStatusForPendingChanges(
                mNotificationStatus,
                GetWindowIfOpened.UVCS(),
                OnNotificationStatusUpdated);
        }

        void CheckIncomingChanges.IAutoRefreshIncomingChangesView.IfVisible(WorkspaceInfo wkInfo)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.AutoRefreshIncomingChangesView();
        }

        void CheckIncomingChanges.IUpdateIncomingChanges.Show(
            WorkspaceInfo wkInfo,
            string infoText,
            string actionText,
            string tooltipText,
            CheckIncomingChanges.Severity severity,
            CheckIncomingChanges.Action action)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UpdateNotificationStatusForIncomingChanges(
                mNotificationStatus,
                GetWindowIfOpened.UVCS(),
                OnNotificationStatusUpdated,
                GetIncomingChangesStatusFromSeverity(
                    severity == CheckIncomingChanges.Severity.Info,
                    severity == CheckIncomingChanges.Severity.Warning),
                infoText,
                actionText,
                tooltipText,
                action == CheckIncomingChanges.Action.Update);
        }

        void CheckIncomingChanges.IUpdateIncomingChanges.Hide(WorkspaceInfo wkInfo)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UpdateNotificationStatusForIncomingChanges(
                mNotificationStatus,
                GetWindowIfOpened.UVCS(),
                OnNotificationStatusUpdated,
                UVCSNotificationStatus.IncomingChangesStatus.None);
        }

        void GluonCheckIncomingChanges.IAutoRefreshIncomingChangesView.IfVisible(WorkspaceInfo wkInfo)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.AutoRefreshIncomingChangesView();
        }

        void GluonCheckIncomingChanges.IUpdateIncomingChanges.Show(
            WorkspaceInfo wkInfo,
            string infoText,
            string actionText,
            string tooltipText,
            GluonCheckIncomingChanges.Severity severity)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UpdateNotificationStatusForIncomingChanges(
                mNotificationStatus,
                GetWindowIfOpened.UVCS(),
                OnNotificationStatusUpdated,
                GetIncomingChangesStatusFromSeverity(
                    severity == GluonCheckIncomingChanges.Severity.Info,
                    severity == GluonCheckIncomingChanges.Severity.Warning),
                infoText,
                actionText,
                tooltipText,
                false);
        }

        void GluonCheckIncomingChanges.IUpdateIncomingChanges.Hide(WorkspaceInfo wkInfo)
        {
            if (mWkInfo == null || !mWkInfo.Equals(wkInfo))
                return;

            UpdateNotificationStatusForIncomingChanges(
                mNotificationStatus,
                GetWindowIfOpened.UVCS(),
                OnNotificationStatusUpdated,
                UVCSNotificationStatus.IncomingChangesStatus.None);
        }

        void DisableForWorkspace()
        {
            if (!mIsEnabledForWorkspace)
                return;

            mLog.Debug("DisableForWorkspace");

            mIsEnabledForWorkspace = false;

            if (mPingEventLoop != null)
                mPingEventLoop.Stop();

            DisposePendingChangesUpdater();
            DisposeIncomingChangesUpdater();

            mWorkspaceOperationsMonitor.Stop();
            mAssetStatusCache.Cancel();

            AvatarImages.Dispose();
            AssetsProcessors.Disable();
            ProjectViewUVCSAssetMenu.Disable();
            DrawProjectOverlay.Disable();
            DrawInspectorOperations.Disable();
            DrawSceneOperations.Disable();
            HierarchyExtensions.Disable();

            RefreshAsset.VersionControlCache(mAssetStatusCache);

            WorkspaceFsNodeReaderCachesCleaner.CleanWorkspaceFsNodeReader(mWkInfo);

            mNotificationStatus.Clean();

            mWkInfo = null;
            mIsGluonMode = false;

            UVCSToolbar.Controller.SetWorkspace(mWkInfo, mIsGluonMode);

            UpdateUVCSWindowIcon(GetWindowIfOpened.UVCS(), GetPluginStatusIcon());
        }

        void ExecuteFullReload(WorkspaceInfo wkInfo)
        {
            DisableForWorkspace();

            if (wkInfo != null)
                EnableForWorkspace(wkInfo);

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.ExecuteFullReload();
        }

        void InitializePendingChangesUpdater(WorkspaceInfo wkInfo)
        {
            mNewChangesInWk = Codice.Client.Common.FsNodeReaders.NewChangesInWk.Build(
                wkInfo, new BuildWorkspacekIsRelevantNewChange());

            mPendingChangesUpdater = new PendingChangesUpdater(
                wkInfo,
                mNewChangesInWk,
                new UnityPlasticTimerBuilder(),
                this,
                new CheckPendingChanges.CalculatePendingChanges(),
                this);

            mPendingChangesUpdater.Start();
        }

        void InitializeIncomingChangesUpdater(
            WorkspaceInfo wkInfo,
            bool bIsGluonMode)
        {
            if (bIsGluonMode)
            {
                mGluonIncomingChangesUpdater = IncomingChanges.
                    BuildUpdaterForGluon(wkInfo, this, this,
                        new GluonCheckIncomingChanges.CalculateIncomingChanges());
                return;
            }

            mDeveloperIncomingChangesUpdater = IncomingChanges.
                BuildUpdaterForDeveloper(wkInfo, this, this);
        }

        void DisposePendingChangesUpdater()
        {
            if (mPendingChangesUpdater != null)
                mPendingChangesUpdater.Dispose();

            mPendingChangesUpdater = null;
        }

        void DisposeIncomingChangesUpdater()
        {
            IncomingChanges.DisposeUpdater(
                mDeveloperIncomingChangesUpdater,
                mGluonIncomingChangesUpdater);

            mDeveloperIncomingChangesUpdater = null;
            mGluonIncomingChangesUpdater = null;
        }

        static void UpdateNotificationStatusForPendingChanges(
            UVCSNotificationStatus notificationStatus,
            UVCSWindow window,
            Action onNotificationStatusUpdated,
            WorkspaceStatusResult workspaceStatusResult = null)
        {
            notificationStatus.WorkspaceStatusResult = workspaceStatusResult;

            Texture2D pluginStatusIcon = notificationStatus.GetIcon();

            if (window != null)
                window.UpdateWindowIcon(pluginStatusIcon);

            UVCSToolbar.Controller.UpdateIcon(pluginStatusIcon);
            UVCSToolbar.Controller.UpdatePendingChangesInfoTooltipText(
                notificationStatus.GetPendingChangesInfoTooltipText());

            if (onNotificationStatusUpdated != null)
                onNotificationStatusUpdated();
        }

        static void UpdateNotificationStatusForIncomingChanges(
            UVCSNotificationStatus notificationStatus,
            UVCSWindow window,
            Action onNotificationStatusUpdated,
            UVCSNotificationStatus.IncomingChangesStatus incomingChangesStatus,
            string infoText = null,
            string actionText = null,
            string tooltipText = null,
            bool hasUpdateAction = false)
        {
            notificationStatus.IncomingChanges = incomingChangesStatus;

            Texture2D pluginStatusIcon = notificationStatus.GetIcon();

            if (window != null)
            {
                window.UpdateIncomingChangesNotification(
                    incomingChangesStatus, infoText, actionText, tooltipText, hasUpdateAction);
                window.UpdateWindowIcon(pluginStatusIcon);
            }

            UVCSToolbar.Controller.UpdateIcon(pluginStatusIcon);
            UVCSToolbar.Controller.UpdateIncomingChangesInfoTooltipText(infoText);

            if (onNotificationStatusUpdated != null)
                onNotificationStatusUpdated();
        }

        static void SetupFsWatcher()
        {
            if (!PlatformIdentifier.IsMac())
                return;

            WorkspaceWatcherFsNodeReadersCache.Get().SetMacFsWatcherBuilder(
                new MacFsWatcherBuilder());
        }

        static void EnsureServerConnectionAsync(
            WorkspaceInfo wkInfo,
            UVCSConnectionMonitor uvcsConnectionMonitor)
        {
            if (PlasticApp.IsUnitTesting)
                return;

            Task.Run(() =>
            {
                RepositorySpec repSpec = PlasticGui.Plastic.API.GetRepositorySpec(wkInfo);

                uvcsConnectionMonitor.SetRepositorySpecForEventTracking(repSpec);

                try
                {
                    // Assume UVCSConnectionMonitor is connected initially.
                    // Trigger a server connection check to validate this assumption.
                    // If the check fails, UVCSConnectionMonitor.OnConnectionError will:
                    // - Disable the UVCS plugin.
                    // - Start the reconnection mechanism.

                    uvcsConnectionMonitor.SetAsConnected();

                    if (!PlasticGui.Plastic.API.CheckServerConnection(repSpec.Server))
                        throw new Exception(string.Format("Failed to connect to {0}", repSpec.Server));
                }
                catch (Exception ex)
                {
                    uvcsConnectionMonitor.OnConnectionError(ex, repSpec.Server);
                }
            });
        }

        static UVCSNotificationStatus.IncomingChangesStatus GetIncomingChangesStatusFromSeverity(
            bool isInfoSeverity,
            bool isWarningSeverity)
        {
            if (isInfoSeverity)
                return UVCSNotificationStatus.IncomingChangesStatus.Changes;

            if (isWarningSeverity)
                return UVCSNotificationStatus.IncomingChangesStatus.Conflicts;

            return UVCSNotificationStatus.IncomingChangesStatus.None;
        }

        static WorkspaceOperationsMonitor BuildWorkspaceOperationsMonitor(
            WorkspaceInfo wkInfo,
            IAssetStatusCache assetStatusCache,
            UVCSAssetsProcessor uvcsAssetsProcessor,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            bool isGluonMode)
        {
            WorkspaceOperationsMonitor result = new WorkspaceOperationsMonitor(
                wkInfo,
                PlasticGui.Plastic.API,
                assetStatusCache,
                uvcsAssetsProcessor,
                pendingChangesUpdater,
                developerIncomingChangesUpdater,
                gluonIncomingChangesUpdater,
                isGluonMode);
            uvcsAssetsProcessor.SetWorkspaceOperationsMonitor(result);
            return result;
        }

        static bool IsOperationInProgressInWorkspaceWindow()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return false;

            if (window.IWorkspaceWindow == null)
                return false;

            return window.IWorkspaceWindow.IsOperationInProgress();
        }

        void UpdateUVCSWindowIcon(UVCSWindow window, Texture2D icon)
        {
            if (window == null)
                return;

            window.UpdateWindowIcon(icon);
        }

        static class Reload
        {
            internal static void IfWorkspaceConfigChanged(
                IPlasticAPI plasticApi,
                WorkspaceInfo lastWkInfo,
                bool lastIsGluonMode,
                Action<WorkspaceInfo> reloadAction)
            {
                string applicationPath = ApplicationDataPath.Get();

                bool isGluonMode = false;
                WorkspaceInfo wkInfo = null;

                IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
                waiter.Execute(
                    /*threadOperationDelegate*/ delegate
                    {
                        wkInfo = FindWorkspace.InfoForApplicationPath(
                            applicationPath, plasticApi);

                        if (wkInfo == null)
                            return;

                        isGluonMode = plasticApi.IsGluonWorkspace(wkInfo);
                    },
                    /*afterOperationDelegate*/ delegate
                    {
                        if (waiter.Exception != null)
                            return;

                        if (!IsWorkspaceConfigChanged(
                                lastWkInfo, wkInfo,
                                lastIsGluonMode, isGluonMode))
                            return;

                        reloadAction(wkInfo);
                    });
            }

            static bool IsWorkspaceConfigChanged(
                WorkspaceInfo lastWkInfo,
                WorkspaceInfo currentWkInfo,
                bool lastIsGluonMode,
                bool currentIsGluonMode)
            {
                if (lastIsGluonMode != currentIsGluonMode)
                    return true;

                if (lastWkInfo == null)
                    return currentWkInfo != null;

                return !lastWkInfo.Equals(currentWkInfo);
            }
        }

        UVCSPlugin()
        {
            mUVCSConnectionMonitor = new UVCSConnectionMonitor(this);
        }

        static UVCSPlugin mInstance;

        DelayedActionBySecondsRunner mDelayedAutoRefreshChangesAction;
        UVCSProjectSettingsProvider mActiveUVCSSettingsProvider;
        WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        IAssetStatusCache mAssetStatusCache;
        GluonIncomingChangesUpdater mGluonIncomingChangesUpdater;
        IncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        PendingChangesUpdater mPendingChangesUpdater;
        INewChangesInWk mNewChangesInWk;
        PingEventLoop mPingEventLoop;
        WorkspaceInfo mWkInfo;
        bool mIsGluonMode;
        bool mIsEnabledForWorkspace;
        bool mIsEnabled;

        readonly UVCSNotificationStatus mNotificationStatus = new UVCSNotificationStatus();
        readonly UVCSConnectionMonitor mUVCSConnectionMonitor;

        static readonly ILog mLog = PlasticApp.GetLogger("UVCSPlugin");
    }
}
