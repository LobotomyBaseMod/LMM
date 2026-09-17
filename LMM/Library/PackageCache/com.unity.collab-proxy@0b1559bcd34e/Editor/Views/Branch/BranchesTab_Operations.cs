using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;

namespace Unity.PlasticSCM.Editor.Views.Branches
{
    internal partial class BranchesTab
    {
        void SwitchToBranchForMode()
        {
            bool isCancelled;
            mSaveAssets.UnderWorkspaceWithConfirmation(
                mWkInfo.ClientPath, mWorkspaceOperationsMonitor,
                out isCancelled);

            if (isCancelled)
                return;

            if (mIsGluonMode)
            {
                SwitchToBranchForGluon();
                return;
            }

            SwitchToBranchForDeveloper();
        }

        void SwitchToBranchForDeveloper()
        {
            RepositorySpec repSpec = BranchesSelection.GetSelectedRepository(mBranchesListView);
            BranchInfo branchInfo = BranchesSelection.GetSelectedBranch(mBranchesListView);

            mBranchOperations.SwitchToBranch(
                repSpec,
                branchInfo,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void SwitchToBranchForGluon()
        {
            BranchInfo branchInfo = BranchesSelection.GetSelectedBranch(mBranchesListView);

            new SwitchToUIOperation().SwitchToBranch(
                mWkInfo,
                branchInfo,
                mViewHost,
                mPendingChangesUpdater,
                mGluonIncomingChangesUpdater,
                new UnityPlasticGuiMessage(),
                mProgressControls,
                mWorkspaceWindow.GluonProgressOperationHandler,
                mGluonUpdateReport,
                mWorkspaceWindow,
                mShelvePendingChangesQuestionerBuilder,
                mShelvedChangesUpdater,
                mEnableSwitchAndShelveFeatureDialog,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromPaths(mWkInfo, items)));
        }

        void CreateBranch()
        {
            if (BranchesSelection.GetSelectedVisibleBranchesCount(mBranchesListView) > 0)
            {
                CreateBranchFromSelectedBranch();
                return;
            }

            CreateBranchFromMainOrCurrentBranch();
        }

        void CreateBranchFromSelectedBranch()
        {
            RepositorySpec repSpec = BranchesSelection.GetSelectedRepository(mBranchesListView);
            BranchInfo branchInfo = BranchesSelection.GetSelectedBranch(mBranchesListView);

            BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromLastParentBranchChangeset(
                mParentWindow, repSpec, branchInfo, null);

            CreateBranchForMode(branchCreationData);
        }

        void CreateBranchFromMainOrCurrentBranch()
        {
            RepositorySpec repSpec = null;
            BranchInfo currentBranchInfo = null;
            BranchInfo mainBranchInfo = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    repSpec = PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo);
                    currentBranchInfo = PlasticGui.Plastic.API.GetWorkingBranch(mWkInfo);
                    mainBranchInfo = PlasticGui.Plastic.API.GetMainBranch(mWkInfo);
                },
                /*afterOperationDelegate*/ delegate
                {
                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.LogException(typeof(BranchesTab).Name, waiter.Exception);
                        return;
                    }

                    if (repSpec == null || currentBranchInfo == null || mainBranchInfo == null)
                    {
                        mLog.DebugFormat("Error obtaining the base information for the branch creation");
                        return;
                    }

                    BranchCreationData branchCreationData =  CreateBranchDialog.CreateBranchFromMainOrCurrentBranch(
                        mParentWindow, repSpec, mainBranchInfo, currentBranchInfo, null);

                    CreateBranchForMode(branchCreationData);
                });
        }

        void CreateBranchForMode(BranchCreationData branchCreationData)
        {
            if (mIsGluonMode)
            {
                CreateBranchForGluon(branchCreationData);
                return;
            }

            CreateBranchForDeveloper(branchCreationData);
        }

        void CreateBranchForDeveloper(BranchCreationData branchCreationData)
        {
            mBranchOperations.CreateBranch(
                branchCreationData,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void CreateBranchForGluon(BranchCreationData branchCreationData)
        {
            CreateBranchOperation.CreateBranch(
                mWkInfo,
                branchCreationData,
                mViewHost,
                mPendingChangesUpdater,
                mGluonIncomingChangesUpdater,
                new UnityPlasticGuiMessage(),
                mProgressControls,
                mWorkspaceWindow.GluonProgressOperationHandler,
                mGluonUpdateReport,
                mWorkspaceWindow,
                mShelvePendingChangesQuestionerBuilder,
                mShelvedChangesUpdater,
                mEnableSwitchAndShelveFeatureDialog,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromPaths(mWkInfo, items)));
        }

        static readonly ILog mLog = PlasticApp.GetLogger("BranchesTab");
    }
}
