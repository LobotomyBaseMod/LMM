using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal class BranchExplorerWindow : EditorWindow
    {
        internal BranchExplorerView BranchExplorerView => mBranchExplorerView;

        internal void OnWorkspaceCreated(WorkspaceInfo wkInfo)
        {
            mWkInfo = wkInfo;
            mRepSpec = PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo);
            ShowBranchExplorer();
        }

        void CreateGUI()
        {
            mMainView = new VisualElement();
            mMainView.style.flexGrow = 1;

            rootVisualElement.Add(mMainView);

            if (mWkInfo == null)
            {
                ShowEmptyState();
                return;
            }

            ShowBranchExplorer();
        }

        void OnEnable()
        {
            mUVCSPlugin = UVCSPlugin.Instance;
            titleContent.image = Images.GetBranchExplorerIcon();

            mUVCSPlugin.Enable();

            TryFindWorkspace();
        }

        void OnFocus()
        {
            if (mWkInfo != null)
                return;

            if (mUVCSPlugin == null)
                return;

            if (!TryFindWorkspace())
                return;

            ShowBranchExplorer();
        }

        bool TryFindWorkspace()
        {
            if (!mUVCSPlugin.ConnectionMonitor.IsConnected)
                return false;

            mWkInfo = FindWorkspace.InfoForApplicationPath(
                ApplicationDataPath.Get(), PlasticGui.Plastic.API);

            if (mWkInfo == null)
                return false;

            mRepSpec = PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo);
            return true;
        }

        void OnDestroy()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.UnityPackage.CloseBranchExplorerView);

            if (mGameInstallation != null)
                mGameInstallation.Dispose();

            CloseWindowIfOpened.BranchExplorerOptions();

            if (mBranchExplorerView != null)
                mBranchExplorerView.Dispose();
        }

        void ShowEmptyState()
        {
            if (mMainView == null)
                return;

            if (mEmptyStateView == null)
                mEmptyStateView = new EmptyStateView(OnCreateWorkspaceButtonClicked);

            mMainView.Clear();
            mMainView.Add(mEmptyStateView);
        }

        void ShowBranchExplorer()
        {
            if (mMainView == null)
                return;

            if (mBranchExplorerView == null)
                mBranchExplorerView = new BranchExplorerView(
                    mWkInfo,
                    mRepSpec,
                    PlasticGui.Plastic.WebRestAPI,
                    mProcessExecutor,
                    mShowDownloadPlasticExeWindow,
                    this);

            mMainView.Clear();
            mMainView.Add(mBranchExplorerView);

            mGameInstallation = Game.Launcher.Install(
                mBranchExplorerView.BranchExplorerViewer, mBranchExplorerView);
        }

        void OnCreateWorkspaceButtonClicked()
        {
            ShowWindow.UVCS();
        }

        Game.Launcher mGameInstallation;

        WorkspaceInfo mWkInfo;
        RepositorySpec mRepSpec;
        VisualElement mMainView;
        BranchExplorerView mBranchExplorerView;
        EmptyStateView mEmptyStateView;

        LaunchTool.IProcessExecutor mProcessExecutor =
            new LaunchTool.ProcessExecutor();
        LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow =
            new LaunchTool.ShowDownloadPlasticExeWindow();

        UVCSPlugin mUVCSPlugin;
    }
}
