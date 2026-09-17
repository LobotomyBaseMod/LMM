using System.Collections.Generic;

using UnityEditor;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using GluonGui;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui;
using PlasticGui.Gluon;
using PlasticGui.Help;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.CodeReview;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using PlasticGui.WorkspaceWindow.QueryViews.Changesets;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Headless;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using Unity.PlasticSCM.Editor.Views.Changesets.Dialogs;
using Unity.PlasticSCM.Editor.Views.Merge;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu
{
    class BranchExplorerViewChangesetMenuOperations :
        IChangesetMenuOperations,
        IBranchExplorerChangesetMenuOperations,
        ICheckoutChangesetMenuOperations,
        ILaunchCodeReviewWindow
    {
        internal BranchExplorerViewChangesetMenuOperations(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            EditorWindow window,
            IWorkspaceWindow workspaceView,
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
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            ViewHost viewHost,
            SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog enableSwitchAndShelveFeatureDialog,
            IShelvePendingChangesQuestionerBuilder shelvePendingChangesQuestionerBuilder,
            IUpdateProgress updateProgressNotifier,
            IUpdateReport updateReport,
            IWorkspaceStatusChangeListener workspaceStatusChangeListener)
        {
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mWindow = window;
            mBrExNavigate = brExNavigate;
            mSelectionHandler = selection;
            mSelectedObjectResolver = selectedObjectResolver;
            mWorkspaceWindow = workspaceView;
            mSwitcher = switcher;
            mGuiHelpEvents = guiHelpEvents;
            mOpenedCodeReviewWindows = openedCodeReviewWindows;
            mAssetStatusCache = assetStatusCache;
            mPendingChangesUpdater = pendingChangesUpdater;
            mIncomingChangesUpdater = incomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mProcessExecutor = processExecutor;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProgressControls = progressControls;
            mViewHost = viewHost;
            mEnableSwitchAndShelveFeatureDialog = enableSwitchAndShelveFeatureDialog;
            mShelvePendingChangesQuestionerBuilder = shelvePendingChangesQuestionerBuilder;
            mUpdateProgressNotifier = updateProgressNotifier;
            mUpdateReport = updateReport;
            mWorkspaceStatusChangeListener = workspaceStatusChangeListener;

            mChangesetOperations = new ChangesetOperations(
                wkInfo,
                workspaceView,
                switcher,
                mergeViewLauncher,
                progressControls,
                new HeadlessUpdateReport(),
                new ApplyShelveReport(window),
                new ContinueWithPendingChangesQuestionerBuilder(switcher, window),
                new ShelvePendingChangesQuestionerBuilder(window),
                new ApplyShelveWithConflictsQuestionerBuilder(),
                pendingChangesUpdater,
                incomingChangesUpdater,
                shelvedChangesUpdater,
                workspaceModeNotificationUpdater,
                openedCodeReviewWindows,
                new EnableSwitchAndShelveFeature(repSpec, window));
        }

        public void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mRepSpec = repSpec;
        }

        public int GetSelectedChangesetsCount()
        {
            return mSelectedObjectResolver.GetSelectedObjectsCount();
        }

        public void DiffChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    LaunchDiffOperations.DiffChangeset(
                        mShowDownloadPlasticExeWindow,
                        mProcessExecutor,
                        mRepSpec,
                        changeset,
                        PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
                });
        }

        public void DiffWithAnotherChangeset()
        {
            ChangesetInfo currentCset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    currentCset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    ChangesetInfo targetedCset = await ChangesetExplorerDialog.AskForChangesetAsync(
                        mWindow, mWkInfo, mRepSpec, mGuiHelpEvents);

                    if (targetedCset == null)
                        return;

                    DiffWindowParameters diffParams = DiffWindowParametersBuilder.Build(
                        mWkInfo,
                        mRepSpec,
                        mWindow.WorkspacePanel.WorkspaceView,
                        mWindow.WorkspacePanel.WorkspaceView.PendingChangesUpdater,
                        mWindow.WorkspacePanel.WorkspaceView.IncomingChangesUpdater,
                        mWindow.WorkspacePanel.WorkspaceView.ShelvedChangesUpdater,
                        mWindow.WorkspacePanel.WorkspaceView,
                        currentCset,
                        targetedCset);
                    DiffWindow.Diff(
                        diffParams, mWindow.WorkspacePanel.WorkspaceView);
                        */
                });
        }

        public void DiffSelectedChangesets()
        {
            IList<ChangesetInfo> changesets = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changesets = ResolveSelectedChangesets();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    if (changesets.Count < 2)
                        return;

                    LaunchDiffOperations.DiffSelectedChangesets(
                        mShowDownloadPlasticExeWindow,
                        mProcessExecutor,
                        mRepSpec,
                        (ChangesetExtendedInfo)changesets[0],
                        (ChangesetExtendedInfo)changesets[1],
                        PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
                });
        }

        public void BrowseRepositoryOnChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    DynamicViewHandler.ShowBrowseRepositoryView(
                        mWkInfo,
                        mWindow,
                        mPendingChangesUpdater,
                        mIncomingChangesUpdater,
                        mShelvedChangesUpdater,
                        mRepSpec,
                        mOverlappedView,
                        changeset.ChangesetId,
                        SettingSerializationName.CHANGESETS_VIEW_NAME,
                        mNotifyStatusBar);
                        */
                });
        }

        public void CreateCodeReview()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    NewCodeReviewBehavior choice = SelectNewCodeReviewBehavior.For(mRepSpec.Server);

                    switch (choice)
                    {
                        case NewCodeReviewBehavior.CreateAndOpenInDesktop:
                            mChangesetOperations.CreateCodeReview(mRepSpec, changeset, this);
                            break;
                        case NewCodeReviewBehavior.RequestFromUnityCloud:
                            OpenRequestReviewPage.ForChangeset(mRepSpec, changeset.ChangesetId);
                            break;
                        case NewCodeReviewBehavior.Ask:
                        default:
                            break;
                    }
                });
        }

        public void CreateBranch()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    EditorApplication.delayCall += () =>
                    {
                        BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromChangeset(
                            mWindow,
                            mRepSpec,
                            changeset,
                            null);

                        mChangesetOperations.CreateBranch(
                            branchCreationData,
                            RefreshAsset.BeforeLongAssetOperation,
                            items => RefreshAsset.AfterLongAssetOperation(
                                mAssetStatusCache,
                                ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
                    };
                });
        }

        public void LabelChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    EditorApplication.delayCall += () =>
                    {
                        ChangesetLabelData changesetLabelData = LabelChangesetDialog.Label(
                            mWindow,
                            mRepSpec,
                            changeset);

                        mChangesetOperations.LabelChangeset(changesetLabelData);
                    };
                });
        }

        public void SwitchToChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mChangesetOperations.SwitchToChangeset(
                        mRepSpec,
                        changeset,
                        RefreshAsset.BeforeLongAssetOperation,
                        items => RefreshAsset.AfterLongAssetOperation(
                            mAssetStatusCache,
                            ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
                });
        }

        public void MergeChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mChangesetOperations.MergeChangeset(mRepSpec, changeset);
                });
        }

        public void CherryPickChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mChangesetOperations.CherryPickChangeset(mRepSpec, changeset);
                });
        }

        public void SubtractiveChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mChangesetOperations.SubtractiveChangeset(mRepSpec, changeset);
                });
        }

        public void SubtractiveChangesetInterval()
        {
            IList<ChangesetInfo> changesets = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changesets = ResolveSelectedChangesets();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mChangesetOperations.SubtractiveChangesetInterval(
                        mRepSpec, changesets[0], changesets[1]);
                });
        }

        public void CherryPickChangesetInterval()
        {
            IList<ChangesetInfo> changesets = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changesets = ResolveSelectedChangesets();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mChangesetOperations.CherryPickChangesetInterval(
                        mRepSpec, changesets[0], changesets[1]);
                });
        }

        public void MergeToChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    mChangesetOperations.MergeToChangeset(
                        mRepSpec,
                        changeset,
                        await MergeToDestinationBranch.GetAsync(mRepSpec, mWindow, mGuiHelpEvents));
                        */
                });
        }

        public void MoveChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();

            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    MoveChangesetsToBranchResult selectionResult = await
                        MoveChangesetsToBranchDialog.MoveChangesetsToBranchAsync(
                            mWindow, mWkInfo, mRepSpec, changeset, mGuiHelpEvents);

                    if (!selectionResult.Result)
                        return;

                    mChangesetOperations.MoveChangeset(
                        mRepSpec,
                        changeset,
                        selectionResult.TargetBranch,
                        null,
                        null);
                        */
                });
        }

        public void DeleteChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mChangesetOperations.DeleteChangeset(
                        mRepSpec,
                        changeset,
                        null,
                        null);
                });
        }

        void IChangesetMenuOperations.RevertToChangeset()
        {
            ChangesetInfo changeset = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();

            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    changeset = ResolveSelectedChangeset();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    RevertToChangesetOperation.RevertTo(
                        mWkInfo,
                        mSwitcher,
                        mWorkspaceWindow,
                        mProgressControls,
                        mGetStatusForWorkspace,
                        mUndoCheckoutOperation,
                        mRevertToChangesetMergeController,
                        GuiMessage.Get(),
                        changeset,
                        mPendingChangesUpdater,
                        RefreshAsset.BeforeLongAssetOperation,
                        () => RefreshAsset.AfterLongAssetOperation(mAssetStatusCache),
                        () => { });
                });
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

        public void NavigateToParent()
        {
            ChangesetDrawInfo parentChangeset =
                ResolveSelectedChangesetParent();

            if (parentChangeset == null)
                return;

            mBrExNavigate.NavigateToShape(
                (VirtualShape)parentChangeset.Visual);
        }

        public void ShowPendingChangesView()
        {
            ShowWindow.UVCS();

            mSwitcher.ShowPendingChanges();
        }

        IList<ChangesetInfo> ResolveSelectedChangesets()
        {
            return mSelectedObjectResolver.GetSelectedChangesets();
        }

        ChangesetInfo ResolveSelectedChangeset()
        {
            return ResolveSelectedChangesets()[0];
        }

        ChangesetDrawInfo ResolveSelectedChangesetParent()
        {
            if (mSelectionHandler.GetSelectedChangesetsCount() != 1)
                return null;

            ChangesetDrawInfo selectedChangeset =
                mSelectionHandler.GetSelectedChangesets()[0];

            return selectedChangeset.Parent;
        }

        bool HasOperationSucceeded(IThreadWaiter waiter)
        {
            if (waiter.Exception == null)
                return true;

            ExceptionsHandler.DisplayException(waiter.Exception);
            return false;
        }

        readonly RevertToChangesetOperation.IGetStatusForWorkspace mGetStatusForWorkspace =
            new RevertToChangesetOperation.GetStatusFromWorkspace();
        readonly RevertToChangesetOperation.IUndoCheckoutOperation mUndoCheckoutOperation =
            new RevertToChangesetOperation.UndoCheckout();
        readonly RevertToChangesetOperation.IRevertToChangesetMergeController mRevertToChangesetMergeController =
            new RevertToChangesetOperation.RevertToChangesetMergeController();

        readonly ViewHost mViewHost;
        readonly IProgressControls mProgressControls;
        readonly SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog mEnableSwitchAndShelveFeatureDialog;
        readonly IShelvePendingChangesQuestionerBuilder mShelvePendingChangesQuestionerBuilder;
        readonly IUpdateProgress mUpdateProgressNotifier;
        readonly IUpdateReport mUpdateReport;
        readonly IWorkspaceStatusChangeListener mWorkspaceStatusChangeListener;

        readonly LaunchTool.IProcessExecutor mProcessExecutor;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly ChangesetOperations mChangesetOperations;
        readonly IBrExNavigate mBrExNavigate;
        readonly BranchExplorerSelection mSelectionHandler;
        readonly BranchExplorerSelectedObjectResolver mSelectedObjectResolver;
        readonly EditorWindow mWindow;
        RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly IViewSwitcher mSwitcher;
        readonly GuiHelpEvents mGuiHelpEvents;
        readonly OpenedCodeReviewWindows mOpenedCodeReviewWindows;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IIncomingChangesUpdater mIncomingChangesUpdater;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
    }
}
