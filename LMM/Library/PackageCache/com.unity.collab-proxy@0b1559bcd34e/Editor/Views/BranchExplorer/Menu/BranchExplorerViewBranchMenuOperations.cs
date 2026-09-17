using System.Collections.Generic;

using UnityEditor;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Threading;
using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.Help;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.CodeReview;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Headless;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using Unity.PlasticSCM.Editor.Views.Merge;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu
{
    class BranchExplorerViewBranchMenuOperations :
        IBranchMenuOperations,
        IBranchExplorerBranchMenuOperations,
        ILaunchCodeReviewWindow
    {
        internal BranchExplorerViewBranchMenuOperations(
            IPlasticWebRestApi restApi,
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            EditorWindow window,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher switcher,
            IMergeViewLauncher mergeViewLauncher,
            BranchExplorerView branchExplorerView,
            IBrExNavigate brExNavigate,
            BranchExplorerSelection selection,
            BranchExplorerSelectedObjectResolver selectedObjectResolver,
            IProgressControls progressControls,
            GuiHelpEvents guiHelpEvents,
            OpenedCodeReviewWindows openedCodeReviewWindows,
            IAssetStatusCache assetStatusCache,
            IPendingChangesUpdater pendingChangesUpdater,
            IIncomingChangesUpdater incomingChangesUpdater,
            IShelvedChangesUpdater shelvedChangesUpdater,
            IWorkspaceModeNotificationUpdater workspaceModeNotificationUpdater,
            LaunchTool.IProcessExecutor processExecutor,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow)
        {
            mRestApi = restApi;
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mWindow = window;
            mSwitcher = switcher;
            mMergeViewLauncher = mergeViewLauncher;
            mBranchExplorerView = branchExplorerView;
            mBrExNavigate = brExNavigate;
            mSelectionHandler = selection;
            mSelectedObjectResolver = selectedObjectResolver;
            mProgressControl = progressControls;
            mGuiHelpEvents = guiHelpEvents;
            mOpenedCodeReviewWindows = openedCodeReviewWindows;
            mAssetStatusCache = assetStatusCache;
            mPendingChangesUpdater = pendingChangesUpdater;
            mIncomingChangesUpdater = incomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mWorkspaceModeNotificationUpdater = workspaceModeNotificationUpdater;
            mProcessExecutor = processExecutor;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;

            mBranchOperations = new BranchOperations(
                wkInfo,
                workspaceWindow,
                mergeViewLauncher,
                branchExplorerView,
                ViewType.BranchExplorerView,
                progressControls,
                new HeadlessUpdateReport(),
                new ApplyShelveReport(window),
                new ContinueWithPendingChangesQuestionerBuilder(switcher, window),
                new ShelvePendingChangesQuestionerBuilder(window),
                new ApplyShelveWithConflictsQuestionerBuilder(),
                pendingChangesUpdater,
                incomingChangesUpdater,
                shelvedChangesUpdater,
                new EnableSwitchAndShelveFeature(repSpec, window));
        }

        public void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mRepSpec = repSpec;
        }

        public BranchInfo GetSelectedBranch()
        {
            List<BranchInfo> selectedBranches = mSelectedObjectResolver.GetSelectedBranches();

            if (selectedBranches.Count == 0)
                return null;

            return selectedBranches[0];
        }

        public int GetSelectedBranchesCount()
        {
            return mSelectedObjectResolver.GetSelectedObjectsCount();
        }

        public bool AreHiddenBranchesShown()
        {
            return false;
        }

        public void CreateBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    BranchCreationData branchCreationData =
                        CreateBranchDialog.CreateBranchFromLastParentBranchChangeset(
                            mWindow, mRepSpec, branch, string.Empty);

                    mBranchOperations.CreateBranch(
                        branchCreationData,
                        RefreshAsset.BeforeLongAssetOperation,
                        items => RefreshAsset.AfterLongAssetOperation(
                            mAssetStatusCache,
                            ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
                });
        }

        public void CreateTopLevelBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    BranchCreationData branchCreationData =
                        await CreateBranchDialog.CreateTopLevelBranch(
                            GlobalConfig.Instance, mRepSpec, branch, mWindow, !mWkInfo.IsTemporary);

                    mBranchOperations.CreateBranch(branchCreationData, null, null);
                            */
                });
        }

        public void SwitchToBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mBranchOperations.SwitchToBranch(
                        mRepSpec,
                        branch,
                        RefreshAsset.BeforeLongAssetOperation,
                        items => RefreshAsset.AfterLongAssetOperation(
                            mAssetStatusCache,
                            ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
                });
        }

        public void MergeBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mBranchOperations.MergeBranch(mRepSpec, branch);
                });
        }

        public void CherrypickBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mBranchOperations.CherrypickBranch(mRepSpec, branch);
                });
        }

        public void MergeToBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    mBranchOperations.MergeToBranch(
                        mRepSpec,
                       branch,
                       await MergeToDestinationBranch.GetAsync(
                           mRepSpec, mWindow, mGuiHelpEvents));
                           */
                });
        }

        public void PullBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    ReplicationOperations.PullBranch(
                        mRestApi,
                        mWindow,
                        mWindow.WorkspacePanel.WorkspaceView,
                        mRepSpec,
                        branch,
                        mGuiHelpEvents);
                        */
                });
        }

        public void PullRemoteBranch()
        {
            /*
            ReplicationOperations.PullRemoteBranch(
                mRestApi,
                mWindow,
                mWindow.WorkspacePanel.WorkspaceView,
                mRepSpec,
                mGuiHelpEvents);
                */
        }

        public void SyncWithGit()
        {
            /*
            ReplicationOperations.SyncWithGit(
                mWindow,
                mIncomingChangesUpdater,
                mShelvedChangesUpdater,
                mRepSpec);
                */
        }

        public void PushBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    ReplicationOperations.PushBranch(
                        mRestApi,
                        mWindow,
                        mWindow.WorkspacePanel.WorkspaceView,
                        mRepSpec,
                        branch,
                        mGuiHelpEvents);
                        */
                });
        }

        public void DiffBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    LaunchDiffOperations.DiffBranch(
                        mShowDownloadPlasticExeWindow,
                        mProcessExecutor,
                        mRepSpec,
                        branch,
                        PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
                });
        }

        public void DiffWithAnotherBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    BranchInfo targetBranch = await BranchesExplorerDialog.AskForBranchAsync(
                        mWindow, string.Format("find branch on repository '{0}'", mRepSpec),
                        mGuiHelpEvents);

                    if (targetBranch == null)
                        return;

                    DiffWindow.DiffBranches(
                        mWkInfo,
                        mRepSpec,
                        mWindow.WorkspacePanel.WorkspaceView,
                        mWindow.WorkspacePanel.WorkspaceView,
                        mWindow.WorkspacePanel.WorkspaceView.PendingChangesUpdater,
                        mWindow.WorkspacePanel.WorkspaceView.IncomingChangesUpdater,
                        mWindow.WorkspacePanel.WorkspaceView.ShelvedChangesUpdater,
                        mWindow.WorkspacePanel.WorkspaceView,
                        mProgressControl,
                        branch,
                        targetBranch);
                        */
                });
        }

        public void ViewChangesets()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    DynamicViewHandler.ShowChangesetsDynamicView(
                        mWkInfo,
                        mWindow,
                        mSwitcher,
                        mMergeViewLauncher,
                        mOverlappedView,
                        SettingSerializationName.BRANCHES_VIEW_NAME,
                        branch,
                        mRepSpec,
                        mGuiHelpEvents,
                        mOpenedCodeReviewWindows,
                        mPendingChangesUpdater,
                        mIncomingChangesUpdater,
                        mShelvedChangesUpdater,
                        mWorkspaceModeNotificationUpdater,
                        mNotifyStatusBar);
                        */
                });
        }

        public void RenameBranch()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    BranchRenameData data = RenameBranchDialog.GetBranchRenameData(
                        mRepSpec, branch, mWindow);

                    mBranchOperations.RenameBranch(data);
                });
        }

        public void HideUnhideBranch()
        {
            IList<BranchInfo> branches = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branches = ResolveSelectedBranches();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mBranchOperations.HideBranch(
                        GetRepositoryList(branches.Count),
                        branches);
                });
        }

        public void DeleteBranch()
        {
            IList<BranchInfo> branches = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branches = ResolveSelectedBranches();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    if (!DeleteBranchDialog.ConfirmDelete(mWindow, branches))
                        return;

                    mBranchOperations.DeleteBranch(
                        GetRepositoryList(branches.Count),
                        branches,
                        DeleteBranchOptions.IncludeChangesets);
                });
        }

        public void CreateCodeReview()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;
                    NewCodeReviewBehavior choice = SelectNewCodeReviewBehavior.For(mRepSpec.Server);

                    switch (choice)
                    {
                        case NewCodeReviewBehavior.CreateAndOpenInDesktop:
                            mBranchOperations.CreateCodeReview(mRepSpec, branch, this);
                            break;
                        case NewCodeReviewBehavior.RequestFromUnityCloud:
                            OpenRequestReviewPage.ForBranch(mRepSpec, branch.BranchId);
                            break;
                        case NewCodeReviewBehavior.Ask:
                        default:
                            break;
                    }
                });
        }

        public void ViewPermissions()
        {
            BranchInfo branch = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    branch = ResolveSelectedBranch();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    PermissionsDialog.ShowPermissions(
                        mRepSpec,
                        branch,
                        mRepSpec.Server,
                        mGuiHelpEvents,
                        mWindow,
                        new PlasticGuiMessage(mWindow));
                        */
                });
        }

        public void NavigateToBase()
        {
            ChangesetDrawInfo branchBaseChangeset =
                ResolveSelectedBranchBaseChangeset();

            if (branchBaseChangeset == null)
                return;

            mBrExNavigate.NavigateToShape(
                (VirtualShape)branchBaseChangeset.Visual);
        }

        public void FilterSelectedBranches()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.DesktopGUI.Filters.FilterSelectedBranches);

            /*
            mBranchExplorerView.FilterBySelectedBranches(
                ResolveSelectedBranches(),
                showRelatedBranches: false,
                displayOnlyPendingToMergeBranches: false);
                */
        }

        public void FilterSelectedAndRelatedBranches()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.DesktopGUI.Filters.FilterSelectedAndRelatedBranches);

            /*
            mBranchExplorerView.FilterBySelectedBranches(
                ResolveSelectedBranches(),
                showRelatedBranches: true,
                displayOnlyPendingToMergeBranches: false);
                */
        }

        public void FilterSelectedBranchesPendingMerges()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.DesktopGUI.Filters.FilterSelectedBranchesPendingMerges);

            /*
            mBranchExplorerView.FilterBySelectedBranches(
                ResolveSelectedBranches(),
                showRelatedBranches: true,
                displayOnlyPendingToMergeBranches: true);
                */
        }

        void IBranchExplorerBranchMenuOperations.MoveBranchUp()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.BranchExplorer.Relayout.MoveBranchUp);

            List<BranchDrawInfo> selectedBranches = mSelectionHandler.GetSelectedBranches();

            foreach (BranchDrawInfo selectedBranch in selectedBranches)
            {
                if (selectedBranch.Row <= 0)
                    continue;

                PersistentBranchLayout.Get().SetPreallocatedIndex(
                    selectedBranch.BranchId, selectedBranch.Row - 1);
            }

            mBranchExplorerView.RecalculateLayout();
        }

        void IBranchExplorerBranchMenuOperations.MoveBranchDown()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.BranchExplorer.Relayout.MoveBranchDown);

            List<BranchDrawInfo> selectedBranches = mSelectionHandler.GetSelectedBranches();

            foreach (BranchDrawInfo selectedBranch in selectedBranches)
            {
                if (selectedBranch.Row >=
                    mBranchExplorerView.BranchExplorerViewer.ExplorerLayout.SlotCount - 1)
                    continue;

                PersistentBranchLayout.Get().SetPreallocatedIndex(
                    selectedBranch.BranchId, selectedBranch.Row + 1);
            }

            mBranchExplorerView.RecalculateLayout();
        }

        void IBranchExplorerBranchMenuOperations.MoveBranchTop()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.BranchExplorer.Relayout.MoveBranchTop);

            List<BranchDrawInfo> selectedBranches = mSelectionHandler.GetSelectedBranches();

            foreach (BranchDrawInfo selectedBranch in selectedBranches)
            {
                PersistentBranchLayout.Get().SetPreallocatedIndex(selectedBranch.BranchId, 0);
            }

            mBranchExplorerView.RecalculateLayout();
        }

        void IBranchExplorerBranchMenuOperations.MoveBranchBottom()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.BranchExplorer.Relayout.MoveBranchBottom);

            List<BranchDrawInfo> selectedBranches = mSelectionHandler.GetSelectedBranches();

            foreach (BranchDrawInfo selectedBranch in selectedBranches)
            {
                PersistentBranchLayout.Get().SetPreallocatedIndex(
                    selectedBranch.BranchId,
                    mBranchExplorerView.BranchExplorerViewer.ExplorerLayout.SlotCount);
            }

            mBranchExplorerView.RecalculateLayout();
        }

        void IBranchExplorerBranchMenuOperations.ClearBranchRelayout()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.BranchExplorer.Relayout.ClearBranchRelayout);

            List<BranchDrawInfo> selectedBranches = mSelectionHandler.GetSelectedBranches();

            foreach (BranchDrawInfo selectedBranch in selectedBranches)
            {
                PersistentBranchLayout.Get().Clear(selectedBranch.BranchId);
            }

            mBranchExplorerView.RecalculateLayout();
        }

        void IBranchExplorerBranchMenuOperations.ResetRelayoutData()
        {
            if (!GuiMessage.ShowYesNoQuestion(
                    PlasticLocalization.Name.ResetRelayoutData.GetString(),
                    PlasticLocalization.Name.ClearAllBranchRelayoutsDialogExplanation.GetString()))
            {
                return;
            }

            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.BranchExplorer.Relayout.ResetRelayoutData);

            PersistentBranchLayout.Get().ClearAll();

            mBranchExplorerView.RecalculateLayout();
        }

        void ILaunchCodeReviewWindow.Show(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ReviewInfo reviewInfo,
            RepObjectInfo repObjectInfo,
            bool bShowReviewChangesTab)
        {
            LaunchTool.OpenCodeReview(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                repSpec,
                reviewInfo.Id,
                PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
        }

        IList<BranchInfo> ResolveSelectedBranches()
        {
            return mSelectedObjectResolver.GetSelectedBranches();
        }

        BranchInfo ResolveSelectedBranch()
        {
            return ResolveSelectedBranches()[0];
        }

        ChangesetDrawInfo ResolveSelectedBranchBaseChangeset()
        {
            if (mSelectionHandler.GetSelectedBranchesCount() != 1)
                return null;

            BranchDrawInfo selectedBranch = mSelectionHandler.GetSelectedBranches()[0];

            if (selectedBranch.EndChangeset == null)
                return selectedBranch.HeadChangeset;

            return selectedBranch.InitChangeset.Parent;
        }

        IList<RepositorySpec> GetRepositoryList(int count)
        {
            RepositorySpec[] repositories = new RepositorySpec[count];

            for (int i = 0; i < repositories.Length; i++)
                repositories[i] = mRepSpec;

            return repositories;
        }

        bool HasOperationSucceeded(IThreadWaiter waiter)
        {
            if (waiter.Exception == null)
                return true;

            ExceptionsHandler.DisplayException(waiter.Exception);
            return false;
        }

        readonly IProgressControls mProgressControl;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly LaunchTool.IProcessExecutor mProcessExecutor;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly BranchOperations mBranchOperations;
        readonly BranchExplorerView mBranchExplorerView;
        readonly IBrExNavigate mBrExNavigate;
        readonly BranchExplorerSelection mSelectionHandler;
        readonly BranchExplorerSelectedObjectResolver mSelectedObjectResolver;
        readonly IViewSwitcher mSwitcher;
        readonly IMergeViewLauncher mMergeViewLauncher;
        readonly EditorWindow mWindow;
        RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;
        readonly GuiHelpEvents mGuiHelpEvents;
        readonly OpenedCodeReviewWindows mOpenedCodeReviewWindows;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IIncomingChangesUpdater mIncomingChangesUpdater;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
        readonly IWorkspaceModeNotificationUpdater mWorkspaceModeNotificationUpdater;
        readonly IPlasticWebRestApi mRestApi;
    }
}
