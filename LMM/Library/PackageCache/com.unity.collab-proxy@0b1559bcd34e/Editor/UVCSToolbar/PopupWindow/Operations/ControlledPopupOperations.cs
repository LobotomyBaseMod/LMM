using System;

using UnityEditor;
using UnityEditor.ShortcutManagement;

using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Headless;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;
using Unity.PlasticSCM.Editor.Views.Merge;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.Operations
{
    internal class ControlledPopupOperations : IQueryRefreshableView
    {
        internal ControlledPopupOperations(
            WorkspaceInfo wkInfo,
            UVCSPlugin uvcsPlugin,
            bool isGluonMode,
            Action refreshWorkspaceWorkingInfo,
            IRefreshableView branchesListPopupPanel,
            Action<WorkingObjectInfo> setWorkingBranch,
            Func<RepositorySpec> fetchRepSpec,
            Func<BranchInfo> fetchMainBranch,
            Func<BranchInfo> fetchWorkingBranch)
        {
            mWkInfo = wkInfo;
            mUVCSPlugin = uvcsPlugin;
            mIsGluonMode = isGluonMode;

            mRefreshWorkspaceWorkingInfo = refreshWorkspaceWorkingInfo;
            mBranchesListPopupPanel = branchesListPopupPanel;
            mFetchRepSpec = fetchRepSpec;
            mFetchMainBranch = fetchMainBranch;
            mFetchWorkingBranch = fetchWorkingBranch;
            mWindow = FindEditorWindow.FirstAvailableWindow();

            mSaveAssets = new SaveAssets();
            mShelvePendingChangesQuestionerBuilder = new ShelvePendingChangesQuestionerBuilder(mWindow);
            mEnableSwitchAndShelveFeatureDialog = new EnableSwitchAndShelveFeature(null, mWindow);
            mProgressControls = new HeadlessProgressControls();
            mViewHost = new HeadlessGluonViewHost(branchesListPopupPanel);

            mBranchOperations = new BranchOperations(
                wkInfo,
                new HeadlessWorkspaceWindow(refreshWorkspaceWorkingInfo, setWorkingBranch),
                new HeadlessMergeViewLauncher(uvcsPlugin),
                this,
                ViewType.BranchesListPopup,
                mProgressControls,
                new HeadlessUpdateReport(),
                new ApplyShelveReport(mWindow),
                new ContinueWithPendingChangesQuestionerBuilder(new HeadlessViewSwitcher(), mWindow),
                mShelvePendingChangesQuestionerBuilder,
                new ApplyShelveWithConflictsQuestionerBuilder(),
                mUVCSPlugin.PendingChangesUpdater,
                mUVCSPlugin.DeveloperIncomingChangesUpdater,
                GetShelvedChangesUpdater(),
                mEnableSwitchAndShelveFeatureDialog);
        }

        internal void ShowPendingChangesView()
        {
            DoShowPendingChangesView(mUVCSPlugin);
        }

        internal void ShowIncomingChangesView()
        {
            DoShowIncomingChangesView(mUVCSPlugin);
        }

        internal void SwitchToBranch(
            BranchInfo branchInfo,
            RepositorySpec repSpec)
        {
            TrackFeatureUseEvent.For(
                repSpec,
                TrackFeatureUseEvent.Features.Toolbar.SwitchToBranch);

            bool isCancelled;
            mSaveAssets.UnderWorkspaceWithConfirmation(
                mWkInfo.ClientPath,
                mUVCSPlugin.WorkspaceOperationsMonitor,
                out isCancelled);

            if (isCancelled)
                return;

            if (mIsGluonMode)
            {
                SwitchToBranchForGluonMode(branchInfo, repSpec);
                return;
            }

            SwitchToBranchForDeveloperMode(branchInfo, repSpec);
        }

        internal void CreateBranch(string proposedBranchName)
        {
            RepositorySpec repSpec = mFetchRepSpec();
            BranchInfo mainBranch = mFetchMainBranch();
            BranchInfo workingBranch = mFetchWorkingBranch();

            if (repSpec == null)
                return;

            TrackFeatureUseEvent.For(
                repSpec,
                TrackFeatureUseEvent.Features.Toolbar.CreateBranch);

            if (workingBranch != null)
            {
                CreateBranch(repSpec, mainBranch, workingBranch, proposedBranchName);
                return;
            }

            CalculateBranchBaseAndCreateBranch(repSpec, mainBranch, proposedBranchName);
        }

        void CalculateBranchBaseAndCreateBranch(
            RepositorySpec repSpec,
            BranchInfo mainBranch,
            string proposedBranchName)
        {
            BranchInfo baseBranchInfo = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    baseBranchInfo = PlasticGui.Plastic.API.GetWorkingBranch(mWkInfo);
                },
                /*afterOperationDelegate*/ delegate
                {
                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.LogException(typeof(ControlledPopupOperations).Name, waiter.Exception);
                        return;
                    }

                    if (baseBranchInfo == null)
                    {
                        return;
                    }

                    CreateBranch(repSpec, mainBranch, baseBranchInfo, proposedBranchName);
                });
        }

        void CreateBranch(
            RepositorySpec repSpec,
            BranchInfo mainBranch,
            BranchInfo workingBranch,
            string proposedBranchName)
        {
            BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromMainOrCurrentBranch(
                mWindow,
                repSpec,
                mainBranch,
                workingBranch,
                proposedBranchName);

            CreateBranchForMode(branchCreationData);
        }

        void CreateBranchForMode(BranchCreationData branchCreationData)
        {
            if (mIsGluonMode)
            {
                CreateBranchForGluonMode(branchCreationData);
                return;
            }

            CreateBranchForDeveloperMode(branchCreationData);
        }

        void CreateBranchForGluonMode(BranchCreationData branchCreationData)
        {
            CreateBranchOperation.CreateBranch(
                mWkInfo,
                branchCreationData,
                mViewHost.ViewHost,
                mUVCSPlugin.PendingChangesUpdater,
                mUVCSPlugin.GluonIncomingChangesUpdater,
                new UnityPlasticGuiMessage(),
                mProgressControls,
                new HeadlessGluonUpdateProgress(),
                new HeadlessGluonUpdateReport(),
                new HeadlessWorkspaceStatusChangeListener(mRefreshWorkspaceWorkingInfo),
                mShelvePendingChangesQuestionerBuilder,
                GetShelvedChangesUpdater(),
                mEnableSwitchAndShelveFeatureDialog,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mUVCSPlugin.AssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromPaths(mWkInfo, items)));
        }

        void CreateBranchForDeveloperMode(BranchCreationData branchCreationData)
        {
            mBranchOperations.CreateBranch(
                branchCreationData,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mUVCSPlugin.AssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void IQueryRefreshableView.RefreshAndSelect(RepObjectInfo repObj)
        {
            //TODO: The repObj is not used
            mBranchesListPopupPanel.Refresh();
        }

        void SwitchToBranchForGluonMode(
            BranchInfo branchInfo,
            RepositorySpec repSpec)
        {
            new SwitchToUIOperation().SwitchToBranch(
                mWkInfo,
                branchInfo,
                mViewHost.ViewHost,
                mUVCSPlugin.PendingChangesUpdater,
                mUVCSPlugin.GluonIncomingChangesUpdater,
                new UnityPlasticGuiMessage(),
                mProgressControls,
                new HeadlessGluonUpdateProgress(),
                new HeadlessGluonUpdateReport(),
                new HeadlessWorkspaceStatusChangeListener(mRefreshWorkspaceWorkingInfo),
                mShelvePendingChangesQuestionerBuilder,
                GetShelvedChangesUpdater(),
                mEnableSwitchAndShelveFeatureDialog,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mUVCSPlugin.AssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromPaths(mWkInfo, items)));
        }

        void SwitchToBranchForDeveloperMode(
            BranchInfo branchInfo,
            RepositorySpec repSpec)
        {
            mBranchOperations.SwitchToBranch(
                repSpec,
                branchInfo,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mUVCSPlugin.AssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        [Shortcut("UVCS/ShowPendingChangesView",
          ToolbarOperationsShortcut.PendingChangesShortcutKey,
          ToolbarOperationsShortcut.PendingChangesShortcutModifiers)]
        static void ExecuteShowPendingChangesViewShortcut()
        {
            if (!UVCSPlugin.Instance.IsEnabled())
                return;

            if (!UVCSToolbar.Controller.IsControlledProject())
                return;

            DoShowPendingChangesView(UVCSPlugin.Instance);
        }

        [Shortcut("UVCS/ShowIncomingChangesView",
          ToolbarOperationsShortcut.IncomingChangesShortcutKey,
          ToolbarOperationsShortcut.IncomingChangesShortcutModifiers)]
        static void ExecuteShowIncomingViewShortcut()
        {
            if (!UVCSPlugin.Instance.IsEnabled())
                return;

            if (!UVCSToolbar.Controller.IsControlledProject())
                return;

            DoShowIncomingChangesView(UVCSPlugin.Instance);
        }

        static void DoShowPendingChangesView(UVCSPlugin uvcsPlugin)
        {
            UVCSWindow window = SwitchUVCSPlugin.OnIfNeeded(uvcsPlugin);

            TrackFeatureUseEvent.For(
                window.RepSpec,
                TrackFeatureUseEvent.Features.Toolbar.ShowPendingChangesView);

            window.ShowPendingChangesView();
        }

        static void DoShowIncomingChangesView(UVCSPlugin uvcsPlugin)
        {
            UVCSWindow window = SwitchUVCSPlugin.OnIfNeeded(uvcsPlugin);

            TrackFeatureUseEvent.For(
                window.RepSpec,
                TrackFeatureUseEvent.Features.Toolbar.ShowIncomingChangesView);

            window.ShowIncomingChangesView();
        }

        static IShelvedChangesUpdater GetShelvedChangesUpdater()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return null;

            return window.ShelvedChangesUpdater;
        }

        readonly WorkspaceInfo mWkInfo;
        readonly UVCSPlugin mUVCSPlugin;
        readonly bool mIsGluonMode;
        readonly Action mRefreshWorkspaceWorkingInfo;
        readonly IRefreshableView mBranchesListPopupPanel;
        readonly ISaveAssets mSaveAssets;
        readonly ShelvePendingChangesQuestionerBuilder mShelvePendingChangesQuestionerBuilder;
        readonly EnableSwitchAndShelveFeature mEnableSwitchAndShelveFeatureDialog;
        readonly IProgressControls mProgressControls;
        readonly Func<RepositorySpec> mFetchRepSpec;
        readonly BranchOperations mBranchOperations;
        readonly Func<BranchInfo> mFetchMainBranch;
        readonly Func<BranchInfo> mFetchWorkingBranch;
        readonly HeadlessGluonViewHost mViewHost;
        readonly EditorWindow mWindow;
    }
}
