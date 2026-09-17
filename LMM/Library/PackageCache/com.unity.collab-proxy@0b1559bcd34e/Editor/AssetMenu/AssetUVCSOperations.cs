using System.IO;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.VersionControl;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Commands.WkTree;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.Gluon;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Diff;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.AssetMenu.Dialogs;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
using GluonCheckoutOperation = GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer.Operations.CheckoutOperation;
using GluonUndoCheckoutOperation = GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer.Operations.UndoCheckoutOperation;
using GluonAddoperation = GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer.Operations.AddOperation;

namespace Unity.PlasticSCM.Editor.AssetMenu
{
    internal class AssetUVCSOperations :
        IAssetMenuUVCSOperations,
        IAssetFilesFilterPatternsMenuOperations
    {
        internal interface IAssetSelection
        {
            AssetList GetSelectedAssets();
        }

        internal AssetUVCSOperations(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            ViewHost viewHost,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IGluonViewSwitcher gluonViewSwitcher,
            IMergeViewLauncher mergeViewLauncher,
            IHistoryViewLauncher historyViewLauncher,
            IAssetStatusCache assetStatusCache,
            IAssetSelection assetSelection,
            ISaveAssets saveAssets,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            PendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            ShelvedChangesUpdater shelvedChangesUpdater,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mPlasticAPI = plasticApi;
            mViewHost = viewHost;
            mWorkspaceWindow = workspaceWindow;
            mViewSwitcher = viewSwitcher;
            mGluonViewSwitcher = gluonViewSwitcher;
            mMergeViewLauncher = mergeViewLauncher;
            mHistoryViewLauncher = historyViewLauncher;
            mAssetStatusCache = assetStatusCache;
            mAssetSelection = assetSelection;
            mSaveAssets = saveAssets;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            mPendingChangesUpdater = pendingChangesUpdater;
            mDeveloperIncomingChangesUpdater = developerIncomingChangesUpdater;
            mGluonIncomingChangesUpdater = gluonIncomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mIsGluonMode = isGluonMode;

            mGuiMessage = new UnityPlasticGuiMessage();
            mProgressControls = new EditorProgressControls(
                mGuiMessage, UnityConstants.UVCS_WINDOW_TITLE);
        }

        void IAssetMenuUVCSOperations.ShowPendingChanges()
        {
            mViewSwitcher.ShowPendingChanges();
        }

        void IAssetMenuUVCSOperations.Add()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo,
                mAssetSelection.GetSelectedAssets(),
                mPlasticAPI,
                mAssetStatusCache,
                AssetMenuOperations.Add);

            if (mIsGluonMode)
            {
                GluonAddoperation.Add(
                    mWkInfo,
                    mViewHost,
                    mProgressControls,
                    mGuiMessage,
                    selectedPaths.ToArray(),
                    false,
                    mPendingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    () => RefreshAsset.VersionControlCache(mAssetStatusCache));
                return;
            }

            AddOperation.Run(
                mWkInfo,
                mWorkspaceWindow,
                mProgressControls,
                null,
                null,
                selectedPaths,
                false,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mShelvedChangesUpdater,
                () => RefreshAsset.VersionControlCache(mAssetStatusCache));
        }

        void IAssetMenuUVCSOperations.Checkout()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo,
                mAssetSelection.GetSelectedAssets(),
                mPlasticAPI,
                mAssetStatusCache,
                AssetMenuOperations.Checkout);

            if (mIsGluonMode)
            {
                GluonCheckoutOperation.Checkout(
                    mViewHost,
                    mProgressControls,
                    mGuiMessage,
                    selectedPaths.ToArray(),
                    false,
                    mPendingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    () => RefreshAsset.VersionControlCache(mAssetStatusCache),
                    mWkInfo);
                return;
            }

            CheckoutOperation.Checkout(
                mWorkspaceWindow,
                null,
                mProgressControls,
                selectedPaths,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mShelvedChangesUpdater,
                () => RefreshAsset.VersionControlCache(mAssetStatusCache),
                mWkInfo);
        }

        void IAssetMenuUVCSOperations.Checkin()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo,
                mAssetSelection.GetSelectedAssets(),
                mPlasticAPI,
                mAssetStatusCache,
                AssetMenuOperations.Checkin);

            if (!CheckinDialog.CheckinPaths(
                    mWkInfo,
                    selectedPaths,
                    mAssetStatusCache,
                    mIsGluonMode,
                    mWorkspaceWindow,
                    mViewHost,
                    mWorkspaceOperationsMonitor,
                    mPendingChangesUpdater,
                    mSaveAssets,
                    mGuiMessage,
                    mMergeViewLauncher,
                    mGluonViewSwitcher))
                return;

            RefreshAsset.UnityAssetDatabase(mAssetStatusCache);
        }

        void IAssetMenuUVCSOperations.Undo()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo,
                mAssetSelection.GetSelectedAssets(),
                mPlasticAPI,
                mAssetStatusCache,
                AssetMenuOperations.Undo);

            mSaveAssets.ForPathsWithoutConfirmation(
                mWkInfo.ClientPath, selectedPaths, mWorkspaceOperationsMonitor);

            if (mIsGluonMode)
            {
                GluonUndoCheckoutOperation.UndoCheckout(
                    mWkInfo,
                    mViewHost,
                    mProgressControls,
                    selectedPaths.ToArray(),
                    false,
                    mPendingChangesUpdater,
                    mGluonIncomingChangesUpdater,
                    () => RefreshAsset.UnityAssetDatabase(mAssetStatusCache));
                return;
            }

            UndoCheckoutOperation.Run(
                mWorkspaceWindow,
                null,
                mProgressControls,
                selectedPaths,
                mPendingChangesUpdater,
                mDeveloperIncomingChangesUpdater,
                mShelvedChangesUpdater,
                () => RefreshAsset.UnityAssetDatabase(mAssetStatusCache));
        }

        void IAssetMenuUVCSOperations.ShowDiff()
        {
            string selectedPath = AssetsSelection.GetSelectedPath(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets());

            DiffInfo diffInfo = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    string symbolicName = GetSymbolicName(mWkInfo, selectedPath);
                    string extension = Path.GetExtension(selectedPath);

                    diffInfo = PlasticGui.Plastic.API.BuildDiffInfoForDiffWithPrevious(
                        selectedPath, symbolicName, selectedPath, extension, mWkInfo);
                },
                /*afterOperationDelegate*/ delegate
                {
                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    DiffOperation.DiffWithPrevious(
                        diffInfo,
                        PlasticExeLauncher.BuildForShowDiff(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                        null);
                });
        }

        void IAssetMenuUVCSOperations.ShowHistory()
        {
            Asset selectedAsset = AssetsSelection.GetSelectedAsset(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets());

            string selectedPath = AssetsPath.GetFullPath.ForPath(selectedAsset.path);

            WorkspaceTreeNode node = PlasticGui.Plastic.API.
                GetWorkspaceTreeNode(mWkInfo, selectedPath);

            mHistoryViewLauncher.ShowHistoryView(
                node.RepSpec,
                node.RevInfo.ItemId,
                selectedPath,
                selectedAsset.isFolder);
        }

        void IAssetFilesFilterPatternsMenuOperations.AddFilesFilterPatterns(
            FilterTypes type,
            FilterActions action,
            FilterOperationType operation)
        {
            List<string> selectedPaths = AssetsSelection.GetSelectedPaths(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets());

            string[] rules = FilterRulesGenerator.GenerateRules(
                selectedPaths, mWkInfo.ClientPath, action, operation);

            bool isApplicableToAllWorkspaces = !mIsGluonMode;
            bool isAddOperation = operation == FilterOperationType.Add;

            FilterRulesConfirmationData filterRulesConfirmationData =
                FilterRulesConfirmationDialog.AskForConfirmation(
                    rules, isAddOperation, isApplicableToAllWorkspaces, EditorWindow.focusedWindow);

            AddFilesFilterPatternsOperation.Run(
                mWkInfo,
                mWorkspaceWindow,
                type,
                operation,
                filterRulesConfirmationData,
                mPendingChangesUpdater);
        }

        static string GetSymbolicName(WorkspaceInfo wkInfo, string selectedPath)
        {
            WorkspaceTreeNode node = PlasticGui.Plastic.API.
                GetWorkspaceTreeNode(wkInfo, selectedPath);

            string branchName = string.Empty;
            BranchInfoCache.TryGetBranchName(
                node.RepSpec, node.RevInfo.BranchId, out branchName);

            string userName = PlasticGui.Plastic.API.GetUserName(
                node.RepSpec.Server, node.RevInfo.Owner);

            string symbolicName = string.Format(
                "cs:{0}@{1} {2} {3}",
                node.RevInfo.Changeset,
                string.Format("br:{0}", branchName),
                userName,
                "Workspace Revision");

            return symbolicName;
        }

        readonly WorkspaceInfo mWkInfo;
        readonly IPlasticAPI mPlasticAPI;
        readonly ViewHost mViewHost;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly IViewSwitcher mViewSwitcher;
        readonly IGluonViewSwitcher mGluonViewSwitcher;
        readonly IMergeViewLauncher mMergeViewLauncher;
        readonly IHistoryViewLauncher mHistoryViewLauncher;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IAssetSelection mAssetSelection;
        readonly ISaveAssets mSaveAssets;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        readonly PendingChangesUpdater mPendingChangesUpdater;
        readonly IncomingChangesUpdater mDeveloperIncomingChangesUpdater;
        readonly GluonIncomingChangesUpdater mGluonIncomingChangesUpdater;
        readonly ShelvedChangesUpdater mShelvedChangesUpdater;
        readonly bool mIsGluonMode;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly EditorProgressControls mProgressControls;
    }
}
