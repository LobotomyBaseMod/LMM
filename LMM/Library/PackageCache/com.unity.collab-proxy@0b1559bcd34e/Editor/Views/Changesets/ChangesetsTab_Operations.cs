using Codice.CM.Common;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal partial class ChangesetsTab
    {
        void SwitchToChangesetForMode()
        {
            bool isCancelled;
            mSaveAssets.UnderWorkspaceWithConfirmation(
                mWkInfo.ClientPath, mWorkspaceOperationsMonitor,
                out isCancelled);

            if (isCancelled)
                return;

            if (mIsGluonMode)
            {
                SwitchToChangesetForGluon();
                return;
            }

            SwitchToChangesetForDeveloper();
        }

        void SwitchToChangesetForDeveloper()
        {
            mChangesetOperations.SwitchToChangeset(
                ChangesetsSelection.GetSelectedRepository(mChangesetsListView),
                ChangesetsSelection.GetSelectedChangeset(mChangesetsListView),
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void SwitchToChangesetForGluon()
        {
            ChangesetExtendedInfo csetInfo = ChangesetsSelection.GetSelectedChangeset(mChangesetsListView);

            new SwitchToUIOperation().SwitchToChangeset(
                mWkInfo,
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                csetInfo.BranchName,
                csetInfo.ChangesetId,
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

        void CreateBranchForMode()
        {
            if (mIsGluonMode)
            {
                CreateBranchForGluon();
                return;
            }

            CreateBranchForDeveloper();
        }

        void CreateBranchForDeveloper()
        {
            RepositorySpec repSpec = ChangesetsSelection.GetSelectedRepository(mChangesetsListView);
            ChangesetExtendedInfo csetInfo = ChangesetsSelection.GetSelectedChangeset(mChangesetsListView);

            BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromChangeset(
                mParentWindow,
                repSpec,
                csetInfo,
                null);

            mChangesetOperations.CreateBranch(
                branchCreationData,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void CreateBranchForGluon()
        {
            RepositorySpec repSpec = ChangesetsSelection.GetSelectedRepository(mChangesetsListView);
            ChangesetExtendedInfo csetInfo = ChangesetsSelection.GetSelectedChangeset(mChangesetsListView);

            BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromChangeset(
                mParentWindow,
                repSpec,
                csetInfo,
                null);

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
    }
}
