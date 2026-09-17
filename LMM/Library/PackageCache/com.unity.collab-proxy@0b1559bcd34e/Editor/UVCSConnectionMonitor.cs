using System;
using System.Threading;

using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Connection;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticPipe;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal class UVCSConnectionMonitor :
        HandleCredsAliasAndServerCert.IHostUnreachableExceptionListener
    {
        internal bool IsTryingReconnection { get { return mIsTryingReconnection; } }

        internal bool IsConnected { get { return PlasticApp.IsUnitTesting || mIsConnected; } }

        internal UVCSConnectionMonitor(UVCSPlugin uvcsPlugin)
        {
            mUVCSPlugin = uvcsPlugin;
        }

        internal void CheckConnection()
        {
            mResetEvent.Set();
        }

        internal void SetAsConnected()
        {
            mIsConnected = true;
        }

        internal void Stop()
        {
            if (!mIsMonitoringServerConnection)
                return;

            mLog.Debug("Stop");

            mIsMonitoringServerConnection = false;
            mResetEvent.Set();
        }

        internal void SetRepositorySpecForEventTracking(RepositorySpec repSpec)
        {
            mRepSpecForEventTracking = repSpec;
        }

        internal void OnConnectionError(Exception ex, string server)
        {
            lock (mOnConnectionErrorLock)
            {
                LogConnectionError(ex, mIsConnected);

                if (!mIsConnected)
                    return;

                mIsConnected = false;
            }

            HandleConnectionLost(
                mRepSpecForEventTracking,
                mUVCSPlugin,
                () => StartMonitoring(server));
        }

        void HandleCredsAliasAndServerCert.IHostUnreachableExceptionListener.OnHostUnreachableException(
            Exception ex,
            PlasticServer plasticServer)
        {
            OnConnectionError(ex, plasticServer.OriginalUrl);
        }

        void StartMonitoring(string server)
        {
            mLog.Debug("StartMonitoring");

            mIsMonitoringServerConnection = true;

            Thread thread = new Thread(MonitorServerConnection);
            thread.IsBackground = true;
            thread.Name = "Plastic SCM Connection Monitor thread";
            thread.Start(server);
        }

        void MonitorServerConnection(object obj)
        {
            string server = (string)obj;

            while (true)
            {
                if (!mIsMonitoringServerConnection)
                    break;

                try
                {
                    mResetEvent.Reset();

                    mIsTryingReconnection = true;

                    if (HasConnectionToServer(server))
                    {
                        mIsConnected = true;
                        HandleConnectionRestored(mRepSpecForEventTracking, mUVCSPlugin);
                        break;
                    }

                    mIsTryingReconnection = false;

                    RepaintUVCSWindowIfOpened();

                    mResetEvent.WaitOne(CONNECTION_POLL_TIME_MS);
                }
                catch (Exception ex)
                {
                    mLog.Error("Error checking network connectivity", ex);
                    mLog.DebugFormat("Stacktrace: {0}", ex.StackTrace);
                }
                finally
                {
                    mIsTryingReconnection = false;
                }
            }
        }

        static void HandleConnectionLost(
            RepositorySpec repSpecForEventTracking,
            UVCSPlugin uvcsPlugin,
            Action startMonitoringAction)
        {
            TrackConnectionLostEvent(repSpecForEventTracking);

            EditorDispatcher.Dispatch(() =>
            {
                uvcsPlugin.Disable();

                startMonitoringAction();

                UVCSWindow window = GetWindowIfOpened.UVCS();

                if (window != null)
                    window.Repaint();
            });
        }

        static void HandleConnectionRestored(
            RepositorySpec repSpecForEventTracking,
            UVCSPlugin uvcsPlugin)
        {
            TrackConnectionRestoredEvent(repSpecForEventTracking);

            EditorDispatcher.Dispatch(() =>
            {
                uvcsPlugin.Enable();

                UVCSWindow window = GetWindowIfOpened.UVCS();

                if (window != null)
                    window.RefreshWorkspaceUI();
            });
        }

        static void RepaintUVCSWindowIfOpened()
        {
            EditorDispatcher.Dispatch(() =>
            {
                UVCSWindow uvcsWindow = GetWindowIfOpened.UVCS();

                if (uvcsWindow != null)
                    uvcsWindow.Repaint();
            });
        }

        static void LogConnectionError(Exception ex, bool isConnected)
        {
            mLog.WarnFormat(isConnected ?
                "A network exception will cause the plugin to go offline" :
                "A network exception happened while the plugin was offline!");

            ExceptionsHandler.LogException("UVCSConnectionMonitor", ex);
        }

        static void TrackConnectionLostEvent(RepositorySpec repSpec)
        {
            if (repSpec == null)
                return;

            TrackFeatureUseEvent.For(
                repSpec,
                TrackFeatureUseEvent.Features.UnityPackage.DisableAutomatically);
        }

        static void TrackConnectionRestoredEvent(RepositorySpec repSpec)
        {
            if (repSpec == null)
                return;

            TrackFeatureUseEvent.For(
                repSpec,
                TrackFeatureUseEvent.Features.UnityPackage.EnableAutomatically);
        }

        static bool HasConnectionToServer(string server)
        {
            try
            {
                mLog.DebugFormat("Checking connection to {0}...", server);

                return PlasticGui.Plastic.API.CheckServerConnection(server);
            }
            catch (Exception ex)
            {
                mLog.DebugFormat("Checking connection to {0} failed: {1}",
                    server,
                    ex.Message);
                return false;
            }
        }

        volatile bool mIsMonitoringServerConnection;
        volatile bool mIsTryingReconnection;
        volatile bool mIsConnected = true;

        RepositorySpec mRepSpecForEventTracking;

        readonly object mOnConnectionErrorLock = new object();
        readonly ManualResetEvent mResetEvent = new ManualResetEvent(false);
        readonly UVCSPlugin mUVCSPlugin;

        const int CONNECTION_POLL_TIME_MS = 30000;

        static readonly ILog mLog = PlasticApp.GetLogger("UVCSConnectionMonitor");
    }
}
