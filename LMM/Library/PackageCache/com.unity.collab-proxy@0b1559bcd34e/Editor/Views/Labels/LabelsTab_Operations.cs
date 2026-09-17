using Codice.CM.Common;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;

namespace Unity.PlasticSCM.Editor.Views.Labels
{
    internal partial class LabelsTab
    {
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
            RepositorySpec repSpec = LabelsSelection.GetSelectedRepository(mLabelsListView);
            MarkerExtendedInfo label = LabelsSelection.GetSelectedLabel(mLabelsListView);

            BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromLabel(
                mParentWindow,
                repSpec,
                label);

            mLabelOperations.CreateBranchFromLabel(
                branchCreationData,
                RefreshAsset.BeforeLongAssetOperation,
                items => RefreshAsset.AfterLongAssetOperation(
                    mAssetStatusCache,
                    ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
        }

        void CreateBranchForGluon()
        {
            RepositorySpec repSpec = LabelsSelection.GetSelectedRepository(mLabelsListView);
            MarkerExtendedInfo label = LabelsSelection.GetSelectedLabel(mLabelsListView);

            BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromLabel(
                mParentWindow,
                repSpec,
                label);

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
