using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.Help;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using PlasticGui.WorkspaceWindow.QueryViews.Labels;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Headless;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;
using Unity.PlasticSCM.Editor.Views.Labels.Dialogs;
using Unity.PlasticSCM.Editor.Views.Merge;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu
{
    class BranchExplorerViewLabelMenuOperations : ILabelMenuOperations
    {
        internal interface ISelectionResolver
        {
            MarkerExtendedInfo ResolveSelectedLabel();

            MarkerExtendedInfo ResolveLabel(long labelId, LabelDrawInfo labelInfo);
        }

        internal BranchExplorerViewLabelMenuOperations(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            EditorWindow window,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher switcher,
            IMergeViewLauncher mergeViewLauncher,
            BranchExplorerView branchExplorerView,
            BranchExplorerSelection selectionHandler,
            ISelectionResolver selectedLabelResolver,
            IProgressControls progressControls,
            GuiHelpEvents guiHelpEvents,
            IAssetStatusCache assetStatusCache,
            IPendingChangesUpdater pendingChangesUpdater,
            IIncomingChangesUpdater incomingChangesUpdater,
            IShelvedChangesUpdater shelvedChangesUpdater,
            LaunchTool.IProcessExecutor processExecutor,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow)
        {
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mWindow = window;
            mSelectionHandler = selectionHandler;
            mSelectedLabelResolver = selectedLabelResolver;
            mGuiHelpEvents = guiHelpEvents;
            mAssetStatusCache = assetStatusCache;
            mPendingChangesUpdater = pendingChangesUpdater;
            mIncomingChangesUpdater = incomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mProcessExecutor = processExecutor;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;

            mLabelOperations = new LabelOperations(
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

        public int GetSelectedLabelsCount()
        {
            if (!mSelectionHandler.HasSelectedLabels())
                return 0;

            return mSelectionHandler.GetSelectedLabels().Count;
        }

        public void CreateLabel()
        {
            LabelCreationData labelCreationData = CreateLabelDialog.CreateLabel(
                mWindow, mWkInfo);

            mLabelOperations.CreateLabel(
                labelCreationData,
                null,
                null);
        }

        public void ApplyLabelToWorkspace()
        {
            MarkerInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mLabelOperations.ApplyLabelToWorkspace(mRepSpec, label);
                });
        }

        public void SwitchToLabel()
        {
            MarkerInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mLabelOperations.SwitchToLabel(
                        mRepSpec,
                        label,
                        RefreshAsset.BeforeLongAssetOperation,
                        items => RefreshAsset.AfterLongAssetOperation(
                            mAssetStatusCache,
                            ProjectPackages.ShouldBeResolvedFromUpdateReport(mWkInfo, items)));
                });
        }

        public void BrowseRepositoryOnLabel()
        {
            MarkerInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
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
                        label.Changeset,
                        SettingSerializationName.BRANCH_EXPLORER_VIEW_NAME,
                        mNotifyStatusBar);
                        */
                });
        }

        public void DiffWithAnotherLabel()
        {
            MarkerInfo currentLabel = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();

            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    currentLabel = ResolveSelectedLabel();
                },
                afterOperationDelegate: /*async*/ delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    MarkerInfo targetedLabel = await LabelExplorerDialog.AskForLabelAsync(
                        mWindow, mWkInfo, mRepSpec, mGuiHelpEvents);

                    DiffLabels(currentLabel, targetedLabel);
                    */
                });
        }

        public void DiffSelectedLabels()
        {
            IList<MarkerExtendedInfo> labels = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();

            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    labels = ResolveSelectedLabels();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    DiffLabels(labels[0], labels[1]);
                });
        }

        public void MergeLabel()
        {
            MarkerInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mLabelOperations.MergeLabel(mRepSpec, label);
                });
        }

        public void MergeToLabel()
        {
            MarkerInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    mLabelOperations.MergeToLabel(
                        mRepSpec,
                        label,
                        MergeToDestinationBranch.GetAsync(mRepSpec, mWindow, mGuiHelpEvents));
                    */
                });
        }

        public void CreateBranchFromLabel()
        {
            MarkerExtendedInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    BranchCreationData branchCreationData = CreateBranchDialog.CreateBranchFromLabel(
                        mWindow,
                        mRepSpec,
                        label);

                    mLabelOperations.CreateBranchFromLabel(
                        branchCreationData,
                        RefreshAsset.BeforeLongAssetOperation,
                        items => RefreshAsset.AfterLongAssetOperation(
                            mAssetStatusCache,
                            ProjectPackages.ShouldBeResolvedFromPaths(mWkInfo, items)));
                });
        }

        public void RenameLabel()
        {
            MarkerInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    LabelRenameData renameLabelData = RenameLabelDialog.GetLabelRenameData(
                        mRepSpec, label, mWindow);

                    mLabelOperations.RenameLabel(renameLabelData);
                });
        }

        public void DeleteLabel()
        {
            MarkerExtendedInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    mLabelOperations.DeleteLabel(
                        new List<RepositorySpec>() { mRepSpec },
                        new List<MarkerExtendedInfo>() { label });
                });
        }

        public void ViewPermissions()
        {
            MarkerInfo label = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    label = ResolveSelectedLabel();
                },
                afterOperationDelegate: delegate
                {
                    if (!HasOperationSucceeded(waiter))
                        return;

                    /*
                    PermissionsDialog.ShowPermissions(
                        mRepSpec,
                        label,
                        mRepSpec.Server,
                        mGuiHelpEvents,
                        mWindow,
                        new PlasticGuiMessage(mWindow));
                        */
                });
        }

        MarkerExtendedInfo ResolveSelectedLabel()
        {
            return mSelectedLabelResolver.ResolveSelectedLabel();
        }

        IList<MarkerExtendedInfo> ResolveSelectedLabels()
        {
            MarkerExtendedInfo selectedLabel = mSelectedLabelResolver.ResolveSelectedLabel();
            List<LabelDrawInfo> labels = mSelectionHandler.GetSelectedLabels();

            List<MarkerExtendedInfo> selectedLabels = labels.
                Where(li => !li.Labels.Any(x => x.Id == selectedLabel.Id)).
                Select(l => mSelectedLabelResolver.ResolveLabel(l.Labels[0].Id, l)).ToList();

            selectedLabels.Insert(0, selectedLabel);

            return selectedLabels;
        }

        bool HasOperationSucceeded(IThreadWaiter waiter)
        {
            if (waiter.Exception == null)
                return true;

            ExceptionsHandler.DisplayException(waiter.Exception);
            return false;
        }

        void DiffLabels(
            MarkerInfo currentLabel,
            MarkerInfo targetedLabel)
        {
            if (targetedLabel == null)
                return;

            LaunchDiffOperations.DiffSelectedLabels(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                mRepSpec,
                currentLabel,
                targetedLabel,
                PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
        }

        readonly LaunchTool.IProcessExecutor mProcessExecutor;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly LabelOperations mLabelOperations;
        readonly ISelectionResolver mSelectedLabelResolver;
        readonly BranchExplorerSelection mSelectionHandler;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IIncomingChangesUpdater mIncomingChangesUpdater;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
        readonly EditorWindow mWindow;
        readonly RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;
        readonly GuiHelpEvents mGuiHelpEvents;
    }
}
