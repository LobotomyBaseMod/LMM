using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;

using Codice;
using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Commands.WkTree;
using Codice.CM.Common;
using Codice.LogWrapper;
using Codice.Utils;
using GluonGui;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.UI;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;

namespace Unity.PlasticSCM.Editor.AssetUtils.Processor
{
    internal interface IWorkspaceOperationsMonitor
    {
        void AddAssetsProcessorPathsToAdd(List<string> paths);

        void AddAssetsProcessorPathsToDelete(List<string> paths);

        void AddAssetsProcessorPathsToCheckout(List<string> paths);

        void AddAssetsProcessorPathsToMove(List<UVCSAssetPostprocessor.PathToMove> paths);

        void AddPathsToCheckout(List<string> paths);
    }

    internal class WorkspaceOperationsMonitor : IWorkspaceOperationsMonitor
    {
        // Internal usage. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public interface IDisableAssetsProcessor
        {
            void Disable();
        }

        internal WorkspaceOperationsMonitor(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            IAssetStatusCache assetStatusCache,
            IDisableAssetsProcessor disableAssetsProcessor,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mPlasticAPI = plasticApi;
            mAssetStatusCache = assetStatusCache;
            mDisableAssetsProcessor = disableAssetsProcessor;
            mPendingChangesUpdater = pendingChangesUpdater;
            mDeveloperIncomingChangesUpdater = developerIncomingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mIsGluonMode = isGluonMode;
        }

        internal void RegisterWindow(
            IWorkspaceWindow workspaceWindow,
            ViewHost viewHost)
        {
            mWorkspaceWindow = workspaceWindow;
            mViewHost = viewHost;
        }

        internal void UnRegisterWindow()
        {
            mWorkspaceWindow = null;
            mViewHost = null;
        }

        internal void Start()
        {
            mLog.Debug("Start");

            mCancelToken = new CancelToken();
            mIsEnabled = true;

            Thread thread = new Thread(TaskLoopThread);
            thread.IsBackground = true;
            thread.Name = "Plastic SCM Workspace Operations Monitor thread";
            thread.Start();
        }

        internal void Stop()
        {
            mLog.Debug("Stop");

            SetAsFinished();
        }

        internal void Disable()
        {
            mIsEnabled = false;

            mLog.Debug("Disabled");
        }

        internal void Enable()
        {
            mIsEnabled = true;

            mLog.Debug("Enabled");
        }

        void IWorkspaceOperationsMonitor.AddAssetsProcessorPathsToAdd(
            List<string> paths)
        {
            AddPathsToProcess(
                mAssetsProcessorPathsToAdd, paths,
                mLock, mResetEvent, mIsEnabled);
        }

        void IWorkspaceOperationsMonitor.AddAssetsProcessorPathsToDelete(
            List<string> paths)
        {
            AddPathsToProcess(
                mAssetsProcessorPathsToDelete, paths,
                mLock, mResetEvent, mIsEnabled);
        }

        void IWorkspaceOperationsMonitor.AddAssetsProcessorPathsToCheckout(
            List<string> paths)
        {
            AddPathsToProcess(
                mAssetsProcessorPathsToCheckout, paths,
                mLock, mResetEvent, mIsEnabled);
        }

        void IWorkspaceOperationsMonitor.AddAssetsProcessorPathsToMove(
            List<UVCSAssetPostprocessor.PathToMove> paths)
        {
            AddPathsToMoveToProcess(
                mAssetsProcessorPathsToMove, paths,
                mLock, mResetEvent, mIsEnabled);
        }

        void IWorkspaceOperationsMonitor.AddPathsToCheckout(
            List<string> paths)
        {
            AddPathsToProcess(
                mPathsToCheckout, paths,
                mLock, mResetEvent, mIsEnabled);
        }

        void TaskLoopThread()
        {
            while (true)
            {
                try
                {
                    if (mCancelToken.IsCancelled())
                        break;

                    if (!mIsEnabled)
                    {
                        SleepUntilNextWorkload();
                        continue;
                    }

                    bool hasAssetProcessorOpsPending = false;
                    bool hasCheckoutOpsPending = false;
                    ProcessOperations(
                        mWkInfo,
                        mPlasticAPI,
                        mAssetStatusCache,
                        FilterManager.Get().GetIgnoredFilter(),
                        FilterManager.Get().GetHiddenChangesFilter(),
                        mWorkspaceWindow,
                        mViewHost,
                        mPendingChangesUpdater,
                        mDeveloperIncomingChangesUpdater,
                        mGluonIncomingChangesUpdater,
                        mAssetsProcessorPathsToAdd,
                        mAssetsProcessorPathsToDelete,
                        mAssetsProcessorPathsToCheckout,
                        mAssetsProcessorPathsToMove,
                        mPathsToCheckout,
                        mCancelToken,
                        mLock,
                        mDisableAssetsProcessor,
                        mIsGluonMode,
                        out hasAssetProcessorOpsPending,
                        out hasCheckoutOpsPending);

                    if (mCancelToken.IsCancelled())
                        break;

                    if (hasAssetProcessorOpsPending ||
                        hasCheckoutOpsPending)
                        continue;

                    SleepUntilNextWorkload();
                }
                catch (Exception e)
                {
                    mLog.ErrorFormat(
                        "Error running the tasks loop : {0}", e.Message);
                    mLog.DebugFormat(
                        "Stacktrace: {0}", e.StackTrace);
                }
            }
        }

        void SetAsFinished()
        {
            if (mCancelToken.IsCancelled())
                return;

            mCancelToken.Cancel();

            CleanPendingOperationsToProcess(
                mAssetsProcessorPathsToAdd,
                mAssetsProcessorPathsToDelete,
                mAssetsProcessorPathsToCheckout,
                mAssetsProcessorPathsToMove,
                mPathsToCheckout,
                mLock);

            mResetEvent.Set();
        }

        void SleepUntilNextWorkload()
        {
            mLog.Debug("SleepUntilNextWorkload");

            mResetEvent.Reset();
            mResetEvent.WaitOne();
        }

        static void CleanPendingOperationsToProcess(
            List<string> assetsProcessorPathsToAdd,
            List<string> assetsProcessorPathsToDelete,
            List<string> assetsProcessorPathsToCheckout,
            List<UVCSAssetPostprocessor.PathToMove> assetsProcessorPathsToMove,
            List<string> pathsToCheckout,
            object lockObj)
        {
            CleanPathsToProcess(assetsProcessorPathsToAdd, lockObj);
            CleanPathsToProcess(assetsProcessorPathsToDelete, lockObj);
            CleanPathsToProcess(assetsProcessorPathsToCheckout, lockObj);
            CleanPathsToMoveToProcess(assetsProcessorPathsToMove, lockObj);

            CleanPathsToProcess(pathsToCheckout, lockObj);
        }

        static void ProcessOperations(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            IAssetStatusCache assetStatusCache,
            IIgnoredFilter ignoredFilter,
            IHiddenChangesFilter hiddenChangesFilter,
            IWorkspaceWindow workspaceWindow,
            ViewHost viewHost,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            List<string> assetsProcessorPathsToAdd,
            List<string> assetsProcessorPathsToDelete,
            List<string> assetsProcessorPathsToCheckout,
            List<UVCSAssetPostprocessor.PathToMove> assetsProcessorPathsToMove,
            List<string> pathsToCheckout,
            CancelToken cancelToken,
            object lockObj,
            IDisableAssetsProcessor disableAssetsProcessor,
            bool isGluonMode,
            out bool hasAssetProcessorOpsPending,
            out bool hasCheckoutOpsPending)
        {
            hasAssetProcessorOpsPending = false;
            hasCheckoutOpsPending = false;

            mLog.Debug("Starting process operations...");

            bool hasAssetProcessorOpsProcessed =
                ProcessAssetProcessorOperations(
                    wkInfo,
                    plasticApi,
                    ignoredFilter,
                    hiddenChangesFilter,
                    assetsProcessorPathsToAdd,
                    assetsProcessorPathsToDelete,
                    assetsProcessorPathsToCheckout,
                    assetsProcessorPathsToMove,
                    cancelToken,
                    lockObj,
                    disableAssetsProcessor);

            if (cancelToken.IsCancelled())
                return;

            bool hasCheckoutOpsProcessed =
                ProcessCheckoutOperation(
                    wkInfo,
                    plasticApi,
                    pathsToCheckout,
                    cancelToken,
                    lockObj);

            mLog.Debug("ProcessOperations - Processed paths ? " +
                (hasAssetProcessorOpsProcessed || hasCheckoutOpsProcessed));

            if (cancelToken.IsCancelled())
                return;

            HasPendingOperationsToProcess(
                assetsProcessorPathsToAdd,
                assetsProcessorPathsToDelete,
                assetsProcessorPathsToCheckout,
                assetsProcessorPathsToMove,
                pathsToCheckout,
                lockObj,
                out hasAssetProcessorOpsPending,
                out hasCheckoutOpsPending);

            bool isAfterAssetProcessorOpNeeded =
                hasAssetProcessorOpsProcessed &&
                !hasAssetProcessorOpsPending;

            bool isAfterCheckoutOpNeeded =
                hasCheckoutOpsProcessed &&
                !hasCheckoutOpsPending;

            if (!isAfterAssetProcessorOpNeeded &&
                !isAfterCheckoutOpNeeded)
                return;

            if (cancelToken.IsCancelled())
                return;

            EditorDispatcher.Dispatch(() =>
            {
                AfterProcessOperations(
                    assetStatusCache,
                    workspaceWindow,
                    viewHost,
                    pendingChangesUpdater,
                    developerIncomingChangesUpdater,
                    gluonIncomingChangesUpdater,
                    isGluonMode);
            });
        }

        static void AfterProcessOperations(
            IAssetStatusCache assetStatusCache,
            IWorkspaceWindow workspaceWindow,
            ViewHost viewHost,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            bool isGluonMode)
        {
            mLog.Debug("AfterProcessOperations");

            RefreshAsset.VersionControlCache(assetStatusCache);

            if (isGluonMode)
            {
                RefreshViewsAfterProcessOperationsForGluon(
                    viewHost, pendingChangesUpdater, gluonIncomingChangesUpdater);
                return;
            }

            RefreshViewsAfterProcessOperationsForDeveloper(
                workspaceWindow, pendingChangesUpdater, developerIncomingChangesUpdater);
        }

        static bool ProcessAssetProcessorOperations(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            IIgnoredFilter ignoredFilter,
            IHiddenChangesFilter hiddenChangesFilter,
            List<string> assetsProcessorPathsToAdd,
            List<string> assetsProcessorPathsToDelete,
            List<string> assetsProcessorPathsToCheckout,
            List<UVCSAssetPostprocessor.PathToMove> assetsProcessorPathsToMove,
            CancelToken cancelToken,
            object lockObj,
            IDisableAssetsProcessor disableAssetsProcessor)
        {
            bool hasProcessedPaths = false;

            try
            {
                hasProcessedPaths = AssetsProcessorOperations.
                    AddIfNotControlled(
                        wkInfo,
                        plasticApi,
                        ignoredFilter,
                        ExtractPathsToProcess(assetsProcessorPathsToAdd, lockObj),
                        cancelToken);

                if (cancelToken.IsCancelled())
                    return false;

                hasProcessedPaths |= AssetsProcessorOperations.
                    DeleteIfControlled(
                        wkInfo,
                        plasticApi,
                        ExtractPathsToProcess(assetsProcessorPathsToDelete, lockObj),
                        cancelToken);

                if (cancelToken.IsCancelled())
                    return false;

                hasProcessedPaths |= AssetsProcessorOperations.
                    MoveIfControlled(
                        wkInfo,
                        plasticApi,
                        ExtractPathsToMoveToProcess(assetsProcessorPathsToMove, lockObj).AsReadOnly(),
                        cancelToken);

                if (cancelToken.IsCancelled())
                    return false;

                hasProcessedPaths |= AssetsProcessorOperations.
                    CheckoutIfControlledAndChanged(
                        wkInfo,
                        plasticApi,
                        hiddenChangesFilter,
                        ExtractPathsToProcess(assetsProcessorPathsToCheckout, lockObj),
                        cancelToken);
            }
            catch (Exception ex)
            {
                LogException(ex);

                disableAssetsProcessor.Disable();
            }

            return hasProcessedPaths;
        }

        static bool ProcessCheckoutOperation(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            List<string> pathsToProcess,
            CancelToken cancelToken,
            object lockObj)
        {
            List<string> paths = ExtractPathsToProcess(
                pathsToProcess, lockObj);

            List<string> result = new List<string>();

            foreach (string path in paths)
            {
                if (cancelToken.IsCancelled())
                    return false;

                WorkspaceTreeNode node =
                    plasticApi.GetWorkspaceTreeNode(wkInfo, path);

                if (node != null &&
                    !CheckWorkspaceTreeNodeStatus.IsCheckedOut(node))
                    result.Add(path);
            }

            if (cancelToken.IsCancelled())
                return false;

            bool hasPathsToProcess = result.Count > 0;

            if (hasPathsToProcess)
            {
                plasticApi.Checkout(
                    wkInfo,
                    result.ToArray(),
                    CheckoutModifiers.ProcessSymlinks);
            }

            LogProcessedPaths("ProcessCheckoutOperation", result);

            return hasPathsToProcess;
        }

        static void AddPathsToProcess(
            List<string> pathsToProcess,
            List<string> paths,
            object lockObj,
            ManualResetEvent resetEvent,
            bool isEnabled)
        {
            if (!isEnabled)
                return;

            lock (lockObj)
            {
                pathsToProcess.AddRange(paths);
            }

            resetEvent.Set();
        }

        static void AddPathsToMoveToProcess(
            List<UVCSAssetPostprocessor.PathToMove> pathsToProcess,
            List<UVCSAssetPostprocessor.PathToMove> paths,
            object lockObj,
            ManualResetEvent resetEvent,
            bool isEnabled)
        {
            if (!isEnabled)
                return;

            lock (lockObj)
            {
                pathsToProcess.AddRange(paths);
            }

            resetEvent.Set();
        }

        static void CleanPathsToProcess(
            List<string> pathsToProcess,
            object lockObj)
        {
            lock (lockObj)
            {
                pathsToProcess.Clear();
            }
        }

        static void CleanPathsToMoveToProcess(
            List<UVCSAssetPostprocessor.PathToMove> pathsToProcess,
            object lockObj)
        {
            lock (lockObj)
            {
                pathsToProcess.Clear();
            }
        }

        static List<string> ExtractPathsToProcess(
            List<string> pathsToProcess,
            object lockObj)
        {
            List<string> result;

            lock (lockObj)
            {
                result = new List<string>(pathsToProcess);
                pathsToProcess.Clear();
            }

            return result;
        }

        static List<UVCSAssetPostprocessor.PathToMove> ExtractPathsToMoveToProcess(
            List<UVCSAssetPostprocessor.PathToMove> pathsToProcess,
            object lockObj)
        {
            List<UVCSAssetPostprocessor.PathToMove> result;

            lock (lockObj)
            {
                result = new List<UVCSAssetPostprocessor.PathToMove>(pathsToProcess);
                pathsToProcess.Clear();
            }

            return result;
        }

        static void HasPendingOperationsToProcess(
            List<string> assetsProcessorPathsToAdd,
            List<string> assetsProcessorPathsToDelete,
            List<string> assetsProcessorPathsToCheckout,
            List<UVCSAssetPostprocessor.PathToMove> assetsProcessorPathsToMove,
            List<string> pathsToCheckout,
            object lockObj,
            out bool hasAssetProcessorOperations,
            out bool hasCheckoutOperations)
        {
            lock (lockObj)
            {
                hasAssetProcessorOperations =
                    assetsProcessorPathsToAdd.Count > 0 ||
                    assetsProcessorPathsToDelete.Count > 0 ||
                    assetsProcessorPathsToCheckout.Count > 0 ||
                    assetsProcessorPathsToMove.Count > 0;

                hasCheckoutOperations =
                    pathsToCheckout.Count > 0;
            }
        }

        static void RefreshViewsAfterProcessOperationsForDeveloper(
            IWorkspaceWindow workspaceWindow,
            IPendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater)
        {
            if (pendingChangesUpdater != null)
                pendingChangesUpdater.Update(DateTime.Now);

            if (developerIncomingChangesUpdater != null)
                developerIncomingChangesUpdater.Update(DateTime.Now);

            if (workspaceWindow == null)
                return;

            workspaceWindow.RefreshView(ViewType.BranchExplorerView);
            workspaceWindow.RefreshView(ViewType.HistoryView);
            workspaceWindow.RefreshView(ViewType.LocksView);
        }

        static void RefreshViewsAfterProcessOperationsForGluon(
            ViewHost viewHost,
            IPendingChangesUpdater pendingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater)
        {
            if (pendingChangesUpdater != null)
                pendingChangesUpdater.Update(DateTime.Now);

            if (gluonIncomingChangesUpdater != null)
                gluonIncomingChangesUpdater.Update(DateTime.Now);

            if (viewHost == null)
                return;

            viewHost.RefreshView(ViewType.HistoryView);
            viewHost.RefreshView(ViewType.LocksView);
        }

        static void LogProcessedPaths(
            string operation,
            List<string> paths)
        {
            if (paths.Count == 0)
            {
                mLog.DebugFormat(
                    "{0} - There are no processed paths.",
                    operation);
                return;
            }

            mLog.DebugFormat(
                "{0} - Processed paths: {1}{2}",
                operation, Environment.NewLine,
                string.Join(Environment.NewLine, paths));
        }

        static void LogException(Exception ex)
        {
            mLog.WarnFormat("Message: {0}", ex.Message);

            mLog.DebugFormat(
                "StackTrace:{0}{1}",
                Environment.NewLine, ex.StackTrace);
        }

        internal static class AssetsProcessorOperations
        {
            internal static bool AddIfNotControlled(
                WorkspaceInfo wkInfo,
                IPlasticAPI plasticApi,
                IIgnoredFilter ignoredFilter,
                List<string> paths,
                CancelToken cancelToken)
            {
                List<string> result = new List<string>();

                foreach (string path in paths)
                {
                    if (cancelToken.IsCancelled())
                        return false;

                    string metaPath = MetaPath.GetMetaPath(path);

                    if (plasticApi.GetWorkspaceTreeNode(wkInfo, path) == null &&
                        !ignoredFilter.IsIgnored(path))
                    {
                            result.Add(path);
                    }

                    if (File.Exists(metaPath) &&
                        plasticApi.GetWorkspaceTreeNode(wkInfo, metaPath) == null &&
                        !ignoredFilter.IsIgnored(metaPath))
                    {
                        result.Add(metaPath);
                    }
                }

                if (cancelToken.IsCancelled())
                    return false;

                bool hasPathsToProcess = result.Count > 0;

                if (hasPathsToProcess)
                {
                    IList checkouts;
                    plasticApi.Add(wkInfo, result.ToArray(), GetDefaultAddOptions(), out checkouts);
                }

                LogProcessedPaths("AddIfNotControlled", result);

                return hasPathsToProcess;
            }

            internal static bool DeleteIfControlled(
                WorkspaceInfo wkInfo,
                IPlasticAPI plasticApi,
                List<string> paths,
                CancelToken cancelToken)
            {
                List<string> processedPaths = new List<string>(paths.Count);

                foreach (string path in paths)
                {
                    if (cancelToken.IsCancelled())
                        return false;

                    string metaPath = MetaPath.GetMetaPath(path);

                    if (plasticApi.GetWorkspaceTreeNode(wkInfo, path) != null)
                        processedPaths.Add(path);

                    if (plasticApi.GetWorkspaceTreeNode(wkInfo, metaPath) != null)
                        processedPaths.Add(metaPath);
                }

                if (cancelToken.IsCancelled())
                    return false;

                plasticApi.DeleteControlled(
                    wkInfo, processedPaths, DeleteModifiers.None, null);

                LogProcessedPaths("DeleteIfControlled", processedPaths);

                return processedPaths.Count > 0;
            }

            internal static bool MoveIfControlled(
                WorkspaceInfo wkInfo,
                IPlasticAPI plasticApi,
                ReadOnlyCollection<UVCSAssetPostprocessor.PathToMove> paths,
                CancelToken cancelToken)
            {
                List<string> processedPaths = new List<string>(paths.Count);

                foreach (UVCSAssetPostprocessor.PathToMove pathToMove in paths)
                {
                    if (cancelToken.IsCancelled())
                        return false;

                    if (plasticApi.GetWorkspaceTreeNode(wkInfo, pathToMove.SrcPath) != null)
                    {
                        plasticApi.Move(
                            wkInfo, pathToMove.SrcPath, pathToMove.DstPath,
                            MoveModifiers.None);

                        processedPaths.Add(string.Format("{0} to {1}",
                            pathToMove.SrcPath, pathToMove.DstPath));
                    }

                    if (cancelToken.IsCancelled())
                        return false;

                    string srcMetaPath = MetaPath.GetMetaPath(pathToMove.SrcPath);
                    string dstMetaPath = MetaPath.GetMetaPath(pathToMove.DstPath);

                    if (plasticApi.GetWorkspaceTreeNode(wkInfo, srcMetaPath) != null)
                    {
                        plasticApi.Move(
                            wkInfo, srcMetaPath, dstMetaPath,
                            MoveModifiers.None);

                        processedPaths.Add(string.Format("{0} to {1}",
                            srcMetaPath, dstMetaPath));
                    }
                }

                LogProcessedPaths("MoveIfControlled", processedPaths);

                return processedPaths.Count > 0;
            }

            internal static bool CheckoutIfControlledAndChanged(
                WorkspaceInfo wkInfo,
                IPlasticAPI plasticApi,
                IHiddenChangesFilter hiddenChangesFilter,
                List<string> paths,
                CancelToken cancelToken)
            {
                List<string> result = new List<string>();

                foreach (string path in paths)
                {
                    if (cancelToken.IsCancelled())
                        return false;

                    string metaPath = MetaPath.GetMetaPath(path);

                    WorkspaceTreeNode node = plasticApi.GetWorkspaceTreeNode(wkInfo, path);
                    WorkspaceTreeNode nodeMeta = plasticApi.GetWorkspaceTreeNode(wkInfo, metaPath);

                    if (node != null &&
                        !CheckWorkspaceTreeNodeStatus.IsCheckedOut(node) &&
                        !hiddenChangesFilter.IsHiddenChanged(path) &&
                        ChangedFileChecker.IsChanged(node.LocalInfo, path, wkInfo.IsDynamic, true))
                    {
                        result.Add(path);
                    }

                    if (nodeMeta != null &&
                        !CheckWorkspaceTreeNodeStatus.IsCheckedOut(nodeMeta) &&
                        !hiddenChangesFilter.IsHiddenChanged(metaPath) &&
                        ChangedFileChecker.IsChanged(nodeMeta.LocalInfo, metaPath, wkInfo.IsDynamic, true))
                    {
                        result.Add(metaPath);
                    }
                }

                if (cancelToken.IsCancelled())
                    return false;

                bool hasPathsToProcess = result.Count > 0;

                if (hasPathsToProcess)
                {
                    plasticApi.Checkout(wkInfo, result.ToArray(), CheckoutModifiers.None);
                }

                LogProcessedPaths("CheckoutIfControlledAndChanged", result);

                return hasPathsToProcess;
            }

            static AddOptions GetDefaultAddOptions()
            {
                AddOptions options = new AddOptions();
                options.AddPrivateParents = true;
                options.NeedCheckPlatformPath = true;
                return options;
            }
        }

        object mLock = new object();

        List<string> mAssetsProcessorPathsToAdd = new List<string>();
        List<string> mAssetsProcessorPathsToDelete = new List<string>();
        List<string> mAssetsProcessorPathsToCheckout = new List<string>();
        List<UVCSAssetPostprocessor.PathToMove> mAssetsProcessorPathsToMove = new List<UVCSAssetPostprocessor.PathToMove>();
        List<string> mPathsToCheckout = new List<string>();

        IWorkspaceWindow mWorkspaceWindow;
        ViewHost mViewHost;

        volatile bool mIsEnabled;
        volatile ManualResetEvent mResetEvent = new ManualResetEvent(false);
        CancelToken mCancelToken = new CancelToken();

        readonly bool mIsGluonMode = false;
        readonly GluonIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly IncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IDisableAssetsProcessor mDisableAssetsProcessor;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IPlasticAPI mPlasticAPI;
        readonly WorkspaceInfo mWkInfo;

        static readonly ILog mLog = PlasticApp.GetLogger("WorkspaceOperationsMonitor");
    }
}
