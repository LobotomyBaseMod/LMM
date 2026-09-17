using System.Diagnostics;
using System.Threading;

using UnityEditor;

using Codice.LogWrapper;
using Codice.Client.Common;
using PlasticGui;
using PlasticPipe.Client;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal static class WaitForPendingOperations
    {
        internal static void ClearProgressBarAndEnterPlayModeIfNeeded()
        {
            // If play mode is pending (we were waiting for operations when domain reload hit),
            // clear the progress bar and resume entering play mode.
            if (!SessionState.GetBool(PLAY_MODE_PENDING_KEY, false))
                return;

            mLog.Debug("Clear progress bar and enter play mode after domain reload");

            // Wait for editor to be ready to clear the progress bar and enter play mode
            Execute.WhenEditorIsReady(ClearProgressBarAndEnterPlayMode);
        }

        internal static void BeforeAssemblyReload()
        {
            mLog.Debug("BeforeAssemblyReload started");

            CancelPlayModeWaitIfNeeded();

            // If we already timed out waiting for play mode, skip the wait loop entirely,
            // to avoid waiting and showing a progress bar again.
            if (mPlayModeWaitTimeoutReached)
            {
                mPlayModeWaitTimeoutReached = false;
                return;
            }

            mLastLoggedSecond = -1;
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool progressBarShown = false;

            try
            {
                while (true)
                {
                    // Tick manually the EditorDispatcher to allow UVCS operations
                    // to report completion in the main thread through the afterOperationDelegate.
                    EditorDispatcher.Update();

                    if (!HasRunningOperations())
                    {
                        mLog.DebugFormat(
                            "All operations finished after {0}ms",
                            stopwatch.ElapsedMilliseconds);
                        break;
                    }

                    bool hasLongRunningOperations = HasLongRunningOperations();

                    // Always wait for UVCS operations without timeout.
                    // Only apply timeout to ThreadWaiters and InUseConnections.
                    bool timeoutReached = stopwatch.ElapsedMilliseconds >= MAX_WAIT_TIMEOUT_MS;
                    if (timeoutReached && !hasLongRunningOperations)
                    {
                        LogTimeoutReached(stopwatch.ElapsedMilliseconds, "Forcing shutdown.");
                        break;
                    }

                    // Show progress bar after the delay to avoid flicker for quick operations
                    if (!progressBarShown && stopwatch.ElapsedMilliseconds >= PROGRESS_BAR_DELAY_MS)
                    {
                        DisplayProgressBar();
                        progressBarShown = true;
                    }

                    LogWaitingForOperations(stopwatch.ElapsedMilliseconds);

                    Thread.Sleep(POLL_INTERVAL_MS);
                }
            }
            finally
            {
                if (progressBarShown)
                {
                    EditorUtility.ClearProgressBar();
                }

                mLog.DebugFormat(
                    "WaitForPendingOperations completed in {0}ms",
                    stopwatch.ElapsedMilliseconds);
            }
        }

        internal static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode)
                return;

            bool isDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled &&
                                          (EditorSettings.enterPlayModeOptions &
                                           EnterPlayModeOptions.DisableDomainReload) != 0;

            if (isDomainReloadDisabled)
                return;

            if (!HasRunningOperations())
                return;

            // If we already timed out, don't delay play mode again to avoid infinite loop.
            // This happens when FinishWaitAndEnterPlayMode() calls EnterPlaymode() after timeout,
            // which triggers this callback again while operations are still running.
            // Note: mPlayModeWaitTimeoutReached is reset in BeforeAssemblyReload().
            if (mPlayModeWaitTimeoutReached)
                return;

            mLog.Debug("OnPlayModeStateChanged delaying play mode");

            // Cancel entering play mode to wait for operations to finish
            EditorApplication.ExitPlaymode();

            // Prevent reentrancy: the ProgressBar doesn't lock the UI.
            if (mPlayModeWaitStopwatch != null)
                return;

            // Mark play mode as pending. Cleared on success, or used to resume after reload.
            SessionState.SetBool(PLAY_MODE_PENDING_KEY, true);

            mLastLoggedSecond = -1;
            mPlayModeWaitStopwatch = Stopwatch.StartNew();
            EditorApplication.update += EnterPlayModeWhenOperationsFinish;
        }

        static void EnterPlayModeWhenOperationsFinish()
        {
            if (mPlayModeWaitStopwatch == null)
                return;

            // Show the progress bar on every frame to ensure it stays displayed regardless of any
            // interruptions (like a popup on Editor Wants To Quit, or taking a screenshot!)
            DisplayProgressBar();

            bool hasLongRunningOperations = HasLongRunningOperations();

            // Always wait for UVCS operations without timeout.
            // Only apply timeout to ThreadWaiters and InUseConnections.
            mPlayModeWaitTimeoutReached = mPlayModeWaitStopwatch.ElapsedMilliseconds >= MAX_WAIT_TIMEOUT_MS;
            if (mPlayModeWaitTimeoutReached && !hasLongRunningOperations)
            {
                LogTimeoutReached(
                    mPlayModeWaitStopwatch.ElapsedMilliseconds,
                    "Forcing play mode.");
                FinishWaitAndEnterPlayMode();
                return;
            }

            if (HasRunningOperations())
            {
                LogWaitingForOperations(mPlayModeWaitStopwatch.ElapsedMilliseconds);
                return;
            }

            FinishWaitAndEnterPlayMode();
        }

        static void FinishWaitAndEnterPlayMode()
        {
            mLog.Debug("FinishWaitAndEnterPlayMode");

            EditorApplication.update -= EnterPlayModeWhenOperationsFinish;
            mPlayModeWaitStopwatch = null;

            ClearProgressBarAndEnterPlayMode();
        }

        static void ClearProgressBarAndEnterPlayMode()
        {
            ClearProgressBar();

            mLog.Debug("Enter play mode");
            EditorApplication.EnterPlaymode();

            SessionState.SetBool(PLAY_MODE_PENDING_KEY, false);
        }

        static void CancelPlayModeWaitIfNeeded()
        {
            if (mPlayModeWaitStopwatch == null)
                return;

            mLog.Debug("CancelPlayModeWait");

            EditorApplication.update -= EnterPlayModeWhenOperationsFinish;
            mPlayModeWaitStopwatch = null;
        }

        static bool HasRunningOperations()
        {
            return HasLowLevelRunningOperations() || HasLongRunningOperations();
        }

        static bool HasLowLevelRunningOperations()
        {
            return ThreadWaiterRegistry.HasRunningOperations() ||
                   CmConnection.HasInUseConnections();
        }

        static bool HasLongRunningOperations()
        {
            return UVCSPlugin.Instance.HasRunningOperation();
        }

        static void DisplayProgressBar()
        {
            EditorUtility.DisplayProgressBar(
                UnityConstants.UVCS_WINDOW_TITLE,
                PlasticLocalization.Name.WaitingForPendingOperationsToFinish.GetString(),
                1f);
        }

        static void ClearProgressBar()
        {
            mLog.Debug("ClearProgressBar");

            EditorUtility.ClearProgressBar();
        }

        static void LogTimeoutReached(long elapsedMs, string action)
        {
            mLog.WarnFormat(
                "Timeout reached after {0}ms. {1} " +
                "Remaining: ThreadWaiters={2}, InUseConnections={3}",
                elapsedMs,
                action,
                ThreadWaiterRegistry.GetRunningOperationsCount(),
                CmConnection.GetInUseConnectionsCount());
        }

        static void LogWaitingForOperations(long elapsedMs)
        {
            // Only log once per second to avoid excessive log spam
            long currentSecond = elapsedMs / 1000;
            if (currentSecond == mLastLoggedSecond)
                return;

            mLastLoggedSecond = currentSecond;

            mLog.DebugFormat(
                "Waiting: ThreadWaiters={0}, InUseConnections={1}, " +
                "UVCSOperations={2}, elapsed={3}ms",
                ThreadWaiterRegistry.GetRunningOperationsCount(),
                CmConnection.GetInUseConnectionsCount(),
                UVCSPlugin.Instance.HasRunningOperation(),
                elapsedMs);
        }

        const int MAX_WAIT_TIMEOUT_MS = 10000;
        const int PROGRESS_BAR_DELAY_MS = 1000;
        const int POLL_INTERVAL_MS = 50;
        const string PLAY_MODE_PENDING_KEY = "WaitForPendingOperations.PlayModePending";

        static long mLastLoggedSecond = -1;
        static bool mPlayModeWaitTimeoutReached;
        static Stopwatch mPlayModeWaitStopwatch;

        static readonly ILog mLog = PlasticApp.GetLogger("WaitForPendingOperations");
    }
}
