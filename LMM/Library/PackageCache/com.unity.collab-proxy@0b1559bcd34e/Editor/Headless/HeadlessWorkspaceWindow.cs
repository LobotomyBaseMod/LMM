using System;
using System.Collections.Generic;

using Codice.Client.BaseCommands;
using Codice.Client.Commands.CheckIn;
using Codice.CM.Common;
using Codice.CM.Common.Replication;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Replication;
using Unity.PlasticSCM.Editor.Toolbar;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessWorkspaceWindow : IWorkspaceWindow
    {
        internal HeadlessWorkspaceWindow(
            Action refreshWorkspaceWorkingInfo,
            Action<WorkingObjectInfo> setWorkingObjectInfo)
        {
            mRefreshWorkspaceWorkingInfo = refreshWorkspaceWorkingInfo;
            mSetWorkingObjectInfo = setWorkingObjectInfo;
            mUpdateProgress = new HeadlessUpdateProgress();
        }

        void IWorkspaceWindow.UpdateTitle()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
            {
                mRefreshWorkspaceWorkingInfo();
                return;
            }

            window.IWorkspaceWindow.UpdateTitle();
        }

        bool IWorkspaceWindow.CheckOperationInProgress()
        {
            return mUpdateProgress.IsOperationRunning();
        }

        bool IWorkspaceWindow.IsOperationInProgress()
        {
            return mUpdateProgress.IsOperationRunning();
        }

        void IWorkspaceWindow.ShowUpdateProgress(string title, UpdateNotifier notifier)
        {
            mUpdateProgress.ShowUpdateProgress(title, notifier);
        }

        void IWorkspaceWindow.EndUpdateProgress()
        {
            mUpdateProgress.EndUpdateProgress();
        }

        void IWorkspaceWindow.RefreshWorkingObjectViews(WorkingObjectInfo workingObjectInfo)
        {
            if (workingObjectInfo != null && workingObjectInfo.BranchInfo != null)
                mSetWorkingObjectInfo(workingObjectInfo);

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IWorkspaceWindow.RefreshWorkingObjectViews(workingObjectInfo);
        }

        void IWorkspaceWindow.RefreshView(ViewType viewType)
        {
            if (viewType == ViewType.BranchesListPopup)
            {
                (UVCSToolbar.Controller as IRefreshableView)?.Refresh();
                return;
            }

            if (viewType == ViewType.BranchExplorerView)
            {
                (GetWindowIfOpened.BranchExplorer()?.BranchExplorerView as IRefreshableView)?.Refresh();
            }

            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IWorkspaceWindow.RefreshView(viewType);
        }

        void IWorkspaceWindow.EndCheckinProgress()
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.EndProgress()
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.EndReplicationProgress(ReplicationStatus replicationStatus)
        {
            throw new NotImplementedException();
        }

        List<IDisposable> IWorkspaceWindow.GetRegisteredViews()
        {
            throw new NotImplementedException();
        }

        bool IWorkspaceWindow.HasCheckinCancelled()
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.RefreshCheckinProgress(
            CheckinStatus checkinStatus,
            BuildProgressSpeedAndRemainingTime.ProgressData progressState)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.RefreshProgress(ProgressData progressData)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.RefreshReplicationProgress(
            BranchReplicationData replicationData,
            ReplicationStatus replicationStatus,
            int current,
            int total)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.RegisterView(
            ViewType type,
            IDisposable disposable,
            IRefreshableView refreshable,
            IWorkingObjectRefreshableView workingObjectRefreshableView)
        {
            throw new NotImplementedException();
        }

        EncryptionConfigurationDialogData IWorkspaceWindow.RequestEncryptionPassword(string server)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.ShowCheckinProgress()
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.ShowProgress()
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.ShowProgress(IProgressOperation progressOperation)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.ShowReplicationProgress(IReplicationOperation replicationOperation)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.UnregisterView(
            ViewType type,
            IDisposable disposable,
            IRefreshableView refreshable,
            IWorkingObjectRefreshableView workingObjectRefreshableView)
        {
            throw new NotImplementedException();
        }

        readonly Action mRefreshWorkspaceWorkingInfo;
        readonly Action<WorkingObjectInfo> mSetWorkingObjectInfo;
        readonly HeadlessUpdateProgress mUpdateProgress;
    }
}
