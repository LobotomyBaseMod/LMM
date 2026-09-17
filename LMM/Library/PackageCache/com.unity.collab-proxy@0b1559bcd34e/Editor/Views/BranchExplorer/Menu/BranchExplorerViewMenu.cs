using UnityEditor;
using UnityEngine;

using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.Help;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.CodeReview;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.Topbar;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Headless;
using Unity.PlasticSCM.Editor.Toolbar;
using Unity.PlasticSCM.Editor.Views.Branches;
using Unity.PlasticSCM.Editor.Views.Changesets;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu
{
    internal class BranchExplorerViewMenu : ChangesetsViewMenu.IMenuOperations, IGetWorkingObject
    {
        internal BranchExplorerViewMenu(
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
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mWindow = window;
            mWorkspaceWindow = workspaceWindow;
            mSwitcher = switcher;
            mMergeViewLauncher = mergeViewLauncher;
            mBranchExplorerView = branchExplorerView;
            mSelectionHandler = selection;
            mSelectedObjectResolver = selectedObjectResolver;
            mProgressControls = progressControls;
            mGuiHelpEvents = guiHelpEvents;
            mAssetStatusCache = assetStatusCache;
            mPendingChangesUpdater = pendingChangesUpdater;
            mIncomingChangesUpdater = incomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mProcessExecutor = processExecutor;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;

            mBranchOperations = new BranchExplorerViewBranchMenuOperations(
                restApi,
                wkInfo,
                repSpec,
                window,
                workspaceWindow,
                switcher,
                mergeViewLauncher,
                branchExplorerView,
                brExNavigate,
                mSelectionHandler,
                selectedObjectResolver,
                progressControls,
                guiHelpEvents,
                openedCodeReviewWindows,
                assetStatusCache,
                pendingChangesUpdater,
                incomingChangesUpdater,
                shelvedChangesUpdater,
                workspaceModeNotificationUpdater,
                processExecutor,
                showDownloadPlasticExeWindow);

            mChangesetOperations = new BranchExplorerViewChangesetMenuOperations(
                wkInfo,
                repSpec,
                window,
                workspaceWindow,
                switcher,
                mergeViewLauncher,
                branchExplorerView,
                brExNavigate,
                mSelectionHandler,
                selectedObjectResolver,
                progressControls,
                guiHelpEvents,
                openedCodeReviewWindows,
                assetStatusCache,
                pendingChangesUpdater,
                incomingChangesUpdater,
                shelvedChangesUpdater,
                workspaceModeNotificationUpdater,
                processExecutor,
                showDownloadPlasticExeWindow,
                new ViewHost(),
                new EnableSwitchAndShelveFeature(repSpec, window),
                new ShelvePendingChangesQuestionerBuilder(window),
                new HeadlessGluonUpdateProgress(),
                new HeadlessGluonUpdateReport(),
                new HeadlessWorkspaceStatusChangeListener(RefreshWorkspaceWorkingInfo));

            mLinkOperations = new BranchExplorerViewLinkMenuOperations(brExNavigate, selection);
        }

        internal void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mRepSpec = repSpec;

            mBranchOperations.UpdateRepositorySpec(repSpec);
            mChangesetOperations.UpdateRepositorySpec(repSpec);
            //mExternalToolsOperations.UpdateRepositorySpec(repSpec);

            if (mLabelsMenu != null)
                mLabelsMenu.UpdateRepositorySpec(repSpec);
        }

        void RefreshWorkspaceWorkingInfo()
        {
            if (mWkInfo == null)
                return;

            UpdateWorkspaceInfoBar.UpdateWorkspaceInfo(
                mWkInfo,
                null,
                UVCSToolbar.Controller);
        }

        internal void Popup()
        {
            if (mSelectionHandler.HasSelectedBranches())
            {
                GetBranchesViewMenu().Popup();
                return;
            }

            if (mSelectionHandler.IsCheckoutChangesetSelected())
            {
                GetCheckoutChangesetViewMenu().Popup();
                return;
            }

            if (mSelectionHandler.HasSelectedChangesets())
            {
                GetChangesetsViewMenu().Popup();
                return;
            }

            if (mSelectionHandler.HasSelectedLabels())
            {
                GetLabelsViewMenu().Popup();
                return;
            }

            if (mSelectionHandler.HasSelectedLinks())
            {
                GetLinkMenu().Popup();
                return;
            }
        }

        internal bool ProcessKeyActionIfNeeded(Event e)
        {
            if (mSelectionHandler.HasSelectedBranches())
            {
                return GetBranchesViewMenu().ProcessKeyActionIfNeeded(e);
            }

            if (mSelectionHandler.IsCheckoutChangesetSelected())
            {
                // the checkout changeset menu hasn't shortcuts
                return false;
            }

            if (mSelectionHandler.HasSelectedChangesets())
            {
                return GetChangesetsViewMenu().ProcessKeyActionIfNeeded(e);
            }

            if (mSelectionHandler.HasSelectedLabels())
            {
                return GetLabelsViewMenu().ProcessKeyActionIfNeeded(e);
            }

            if (mSelectionHandler.HasSelectedLinks())
            {
                // the merge link menu hasn't shortcuts
                return false;
            }

            return false;
        }

        internal void ExecuteDefaultAction()
        {
            if (mSelectionHandler.HasSelectedBranches())
            {
                mBranchOperations.DiffBranch();
                return;
            }

            if (mSelectionHandler.HasSelectedChangesets())
            {
                mChangesetOperations.DiffChangeset();
                return;
            }

            if (mSelectionHandler.HasSelectedLabels())
            {
                GetLabelsViewMenu().BrowseRepositoryOnLabel();
                return;
            }
        }

        void ChangesetsViewMenu.IMenuOperations.DiffBranch()
        {
        }

        ChangesetInfo ChangesetsViewMenu.IMenuOperations.GetSelectedChangeset()
        {
            RepObjectInfo selectedObject = mSelectedObjectResolver.GetFirstSelectedObject();

            if (selectedObject is not ChangesetInfo)
                return null;

            return (ChangesetInfo)selectedObject;
        }

        object IGetWorkingObject.Get()
        {
            return mSelectedObjectResolver.GetFirstSelectedObject();
        }

        BranchesViewMenu GetBranchesViewMenu()
        {
            if (mBranchesViewMenu == null)
            {
                mBranchesViewMenu = new BranchesViewMenu(
                    mBranchOperations,
                    PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
            }

            return mBranchesViewMenu;
        }

        CheckoutChangesetViewMenu GetCheckoutChangesetViewMenu()
        {
            if (mCheckoutChangesetViewMenu == null)
                mCheckoutChangesetViewMenu = new CheckoutChangesetViewMenu(mChangesetOperations);

            return mCheckoutChangesetViewMenu;
        }

        ChangesetsViewMenu GetChangesetsViewMenu()
        {
            if (mChangesetsViewMenu == null)
            {
                mChangesetsViewMenu = new ChangesetsViewMenu(
                    mChangesetOperations,
                    this,
                    this,
                    PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
            }

            return mChangesetsViewMenu;
        }

        BranchExplorerLabelMenu GetLabelsViewMenu()
        {
            if (mLabelsMenu == null)
            {
                mLabelsMenu = new BranchExplorerLabelMenu(
                    mWkInfo,
                    mRepSpec,
                    mWindow,
                    mWorkspaceWindow,
                    mSwitcher,
                    mMergeViewLauncher,
                    mBranchExplorerView,
                    mSelectionHandler,
                    mProgressControls,
                    mGuiHelpEvents,
                    mAssetStatusCache,
                    mPendingChangesUpdater,
                    mIncomingChangesUpdater,
                    mShelvedChangesUpdater,
                    mProcessExecutor,
                    mShowDownloadPlasticExeWindow);
            }

            return mLabelsMenu;
        }

        LinkMenu GetLinkMenu()
        {
            if (mLinkMenu == null)
                mLinkMenu = new LinkMenu(mLinkOperations);

            return mLinkMenu;
        }

        BranchesViewMenu mBranchesViewMenu;
        CheckoutChangesetViewMenu mCheckoutChangesetViewMenu;
        ChangesetsViewMenu mChangesetsViewMenu;
        BranchExplorerLabelMenu mLabelsMenu;
        LinkMenu mLinkMenu;

        RepositorySpec mRepSpec;

        readonly LaunchTool.IProcessExecutor mProcessExecutor;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly WorkspaceInfo mWkInfo;
        readonly EditorWindow mWindow;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly IViewSwitcher mSwitcher;
        readonly IMergeViewLauncher mMergeViewLauncher;
        readonly BranchExplorerView mBranchExplorerView;
        readonly BranchExplorerSelection mSelectionHandler;
        readonly BranchExplorerSelectedObjectResolver mSelectedObjectResolver;
        readonly IProgressControls mProgressControls;
        readonly GuiHelpEvents mGuiHelpEvents;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IPendingChangesUpdater mPendingChangesUpdater;
        readonly IIncomingChangesUpdater mIncomingChangesUpdater;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;

        readonly BranchExplorerViewBranchMenuOperations mBranchOperations;
        readonly BranchExplorerViewChangesetMenuOperations mChangesetOperations;
        readonly BranchExplorerViewLinkMenuOperations mLinkOperations;
    }
}
