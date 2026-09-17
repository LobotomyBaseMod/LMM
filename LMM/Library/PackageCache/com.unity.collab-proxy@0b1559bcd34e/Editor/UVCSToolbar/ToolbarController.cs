using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Topbar;
using PlasticGui.WorkspaceWindow.Topbar.WorkingObjectInfo.BranchesList;
using Unity.PlasticSCM.Editor.Toolbar.PopupWindow;
using Unity.PlasticSCM.Editor.Toolbar.PopupWindow.Operations;
using Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList;

namespace Unity.PlasticSCM.Editor.Toolbar
{
    internal interface IUpdateToolbarButtonVisibility
    {
        void Show();
        void Hide();
    }

    internal class ToolbarController :
        UpdateWorkspaceInfoBar.IWorkingObjectInfoPanel,
        IRefreshableView,
        IUpdateToolbarButtonVisibility
    {
        public const string ToolbarButtonPath = "Services/Unity Version Control"; // used by Unity 6.3+

        public event Action OnToolbarButtonInvalidated = delegate { };
        public event Action OnToolbarInvalidated = delegate { };

        internal ToolbarController(UVCSPlugin uvcsPlugin)
        {
            mUVCSPlugin = uvcsPlugin;
        }

        internal bool IsControlledProject()
        {
            return mWkInfo != null;
        }

        internal void PopupClicked(Rect rect)
        {
            Vector2 buttonBottom = new Vector2(rect.x, rect.y + rect.height);

            if (IsControlledProject())
            {
                ShowControlledPopup(buttonBottom);
                return;
            }

            ShowUncontrolledPopup(buttonBottom);
        }

        internal void SetWorkspace(
            WorkspaceInfo wkInfo,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mIsGluonMode = isGluonMode;
            mModel = null;
            mWorkingBranch = null;

            if (mWkInfo != null)
            {
                // Workspace is controlled - refresh workspace info and load branches
                RefreshWorkspaceWorkingInfo();
                LoadBranches();
            }
            else
            {
                // Workspace is uncontrolled - reset to defaults and refresh the button
                ResetToolbarButtonData();
                FireOnToolbarButtonInvalidated();
            }
        }

        internal void LoadBranches()
        {
            if (mWkInfo == null)
                return;

            PlasticThreadPool.Run(delegate { SetCalculatedModel(CalculateBranchesListModel(mWkInfo)); });
        }

        internal void RefreshWorkspaceWorkingInfo()
        {
            if (mWkInfo == null)
                return;

            UpdateWorkspaceInfoBar.UpdateWorkspaceInfo(
                mWkInfo,
                null,
                this);
        }

        internal void UpdateIcon(Texture icon)
        {
            mDropDownButtonData.Icon = icon;

            FireOnToolbarButtonInvalidated();
        }

        internal void UpdatePendingChangesInfoTooltipText(string pendingChangesInfoTooltipText)
        {
            mButtonTooltipData.PendingChangesInfo = pendingChangesInfoTooltipText;

            UpdateButtonTooltip(mButtonTooltipData);
        }

        internal void UpdateIncomingChangesInfoTooltipText(string incomingChangesInfoTooltipText)
        {
            mButtonTooltipData.IncomingChangesInfo = incomingChangesInfoTooltipText;

            UpdateButtonTooltip(mButtonTooltipData);
        }

        internal UVCSToolbarButtonData GetButtonData()
        {
            if (mDropDownButtonData.Icon == null)
                mDropDownButtonData.Icon = UVCSPlugin.Instance.GetPluginStatusIcon();

            return mDropDownButtonData;
        }

        internal void Enable()
        {
            mDropDownButtonData.IsVisible = UVCSToolbarButtonIsShownPreference.IsEnabled();
            FireOnToolbarButtonInvalidated();
        }

        internal void Disable()
        {
            mDropDownButtonData.IsVisible = false;
            FireOnToolbarButtonInvalidated();
        }

        void IUpdateToolbarButtonVisibility.Show()
        {
            UVCSToolbarButtonIsShownPreference.Enable();

            mDropDownButtonData.IsVisible = true;

            FireOnToolbarButtonInvalidated();
        }

        void IUpdateToolbarButtonVisibility.Hide()
        {
#if UNITY_6000_3_OR_NEWER

            UnityEditor.Toolbars.MainToolbar.HideAll(ToolbarButtonPath);
#else
            UVCSToolbarButtonIsShownPreference.Disable();
            mDropDownButtonData.IsVisible = false;
            FireOnToolbarButtonInvalidated();
#endif
        }

        void IRefreshableView.Refresh()
        {
            LoadBranches();
        }

        void UpdateWorkspaceInfoBar.IWorkingObjectInfoPanel.UpdateInfo(
            string objectType,
            string objectName,
            string repositoryName,
            string serverName)
        {
            if (mWkInfo == null)
                return;

            mDropDownButtonData.Text = GetShorten.ObjectName(
                objectName,
                objectType);

            FireOnToolbarButtonInvalidated();

            string serverForDisplay = ResolveServer.ToDisplayString(serverName);

            mButtonTooltipData.WorkingObjectType = objectType;
            mButtonTooltipData.WorkingObjectName = objectName;
            mButtonTooltipData.RepositoryName = repositoryName;
            mButtonTooltipData.ServerName = serverForDisplay;

            UpdateButtonTooltip(mButtonTooltipData);

            UpdateWorkingBranch(
                objectType,
                objectName,
                repositoryName,
                serverName);
        }

        void UpdateWorkspaceInfoBar.IWorkingObjectInfoPanel.UpdateComment(
            string comment,
            bool bFailed)
        {
            if (mWkInfo == null)
                return;

            string commentText = string.IsNullOrEmpty(comment) ?
                PlasticLocalization.Name.NoCommentSet.GetString() :
                comment;

            mButtonTooltipData.Comment = commentText;
            UpdateButtonTooltip(mButtonTooltipData);
        }

        void UpdateButtonTooltip(ButtonTooltipData buttonTooltipData)
        {
            if (mWkInfo == null)
                return;

            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format(
                "{0}: {1}@{2}@{3}",
                buttonTooltipData.WorkingObjectType,
                buttonTooltipData.WorkingObjectName,
                buttonTooltipData.RepositoryName,
                buttonTooltipData.ServerName));

            if (!string.IsNullOrEmpty(buttonTooltipData.Comment))
            {
                sb.AppendLine();
                sb.Append(buttonTooltipData.Comment);
            }

            if (!string.IsNullOrEmpty(buttonTooltipData.IncomingChangesInfo))
            {
                sb.AppendLine();
                sb.Append(buttonTooltipData.IncomingChangesInfo);
            }

            if (!string.IsNullOrEmpty(buttonTooltipData.PendingChangesInfo))
            {
                sb.AppendLine();
                sb.Append(buttonTooltipData.PendingChangesInfo);
            }

            mDropDownButtonData.Tooltip = sb.ToString();
            FireOnToolbarButtonInvalidated();
        }

        void FireOnToolbarInvalidated()
        {
            if (OnToolbarInvalidated != null)
                OnToolbarInvalidated();
        }

        void FireOnToolbarButtonInvalidated()
        {
            if (OnToolbarButtonInvalidated != null)
                OnToolbarButtonInvalidated();
        }

        void SetWorkingBranch(WorkingObjectInfo workingObjectInfo)
        {
            mWorkingBranch = workingObjectInfo.BranchInfo;
        }

        void UpdateWorkingBranch(
            string objectType,
            string objectName,
            string repository,
            string server)
        {
            RepositorySpec repSpec = RepositorySpec.BuildFromNameAndResolvedServer(repository, server);

            if (objectType != PlasticLocalization.Name.Branch.GetString())
            {
                mWorkingBranch = null;
                return;
            }

            PlasticThreadPool.Run(delegate
            {
                try
                {
                    mWorkingBranch = PlasticGui.Plastic.API.GetBranchInfo(repSpec, objectName);
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat("Error loading the working branch: {0}", ex.Message);
                    mLog.DebugFormat("Stack trace: {0}", ex.StackTrace);
                }
            });
        }

        void ShowUncontrolledPopup(Vector2 popupPosition)
        {
            int popupHeight = PopupWindowDrawing.MENU_ITEM_HEIGHT * 4 +
                              PopupWindowDrawing.DELIMITER_HEIGHT * 2;

            UncontrolledPopupWindow window = new UncontrolledPopupWindow(
                new UncontrolledPopupOperations(mUVCSPlugin, this),
                FireOnToolbarInvalidated,
                new Vector2(285, popupHeight));

            UnityEditor.PopupWindow.Show(
                new Rect(popupPosition, Vector2.zero), window);
        }

        BranchesListModel GetCalculatedModel()
        {
            lock (mModelLock)
            {
                return mModel;
            }
        }

        void SetCalculatedModel(BranchesListModel branches)
        {
            lock (mModelLock)
            {
                mModel = branches;
                mMainBranch = (mModel.Branches == null) ?
                    null : mModel.Branches.MainBranch;
            }
        }

        void ShowControlledPopup(Vector2 popupPosition)
        {
            ControlledPopupOperations operations = new ControlledPopupOperations(
                mWkInfo,
                mUVCSPlugin,
                mIsGluonMode,
                RefreshWorkspaceWorkingInfo,
                this,
                SetWorkingBranch,
                () => mModel == null ? null : mModel.RepSpec,
                () => GetMainBranch(mWkInfo),
                () => mWorkingBranch);

            ControlledPopupWindow window = new ControlledPopupWindow(
                operations,
                RefreshBranches,
                () => mWorkingBranch,
                FireOnToolbarInvalidated,
                new Vector2(375, 425));

            BranchesListModel model = GetCalculatedModel();

            if (model != null)
                model.ResetFilter();

            window.SetModel(model);

            RefreshBranches(window);

            UnityEditor.PopupWindow.Show(
                new Rect(popupPosition, Vector2.zero), window);
        }

        void RefreshBranches(ControlledPopupWindow window)
        {
            bool isOperationFinished = false;

            Task.Delay(TimeSpan.FromMilliseconds(1500)).ContinueWith((task, state) =>
            {
                if (window != null && !isOperationFinished)
                {
                    window.ShowProgressBar();
                }
            }, null);

            BranchesListModel model = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(50);
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    model = CalculateBranchesListModel(mWkInfo);
                    SetCalculatedModel(model);
                },
                afterOperationDelegate: delegate
                {
                    isOperationFinished = true;

                    if (window == null)
                        return;

                    window.HideProgressBar();
                    window.SetModel(model);
                });
        }

        void ResetToolbarButtonData()
        {
            mButtonTooltipData = new ButtonTooltipData();
            mDropDownButtonData = UVCSToolbarButtonData.BuildDefault();
        }

        static BranchesListModel CalculateBranchesListModel(WorkspaceInfo wkInfo)
        {
            try
            {
                RepositorySpec repSpec = PlasticGui.Plastic.API.GetRepositorySpec(wkInfo);
                QueryResult queryResult = PlasticGui.Plastic.API.FindQuery(wkInfo, "find branches");

                if (repSpec == null || queryResult == null)
                    return BranchesListModel.BuildEmpty();

                ClassifiedBranchesList result = new ClassifiedBranchesList(
                    queryResult.Result[0].Cast<BranchInfo>().ToList(),
                    RecentBranchesSettings.GetRecentBranches(PlasticGuiConfig.GetConfigFile(), wkInfo.Id),
                    WellKnownGuids.MainBranch);

                return BranchesListModel.FromBranches(result, repSpec);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat("Error loading the branches: {0}", ex.Message);
                mLog.DebugFormat("Stack trace: {0}", ex.StackTrace);

                return BranchesListModel.FromException(ex);
            }
        }

        BranchInfo GetMainBranch(WorkspaceInfo wkInfo)
        {
            if (mMainBranch != null)
                return mMainBranch;

            return PlasticGui.Plastic.API.GetMainBranch(wkInfo);
        }

        WorkspaceInfo mWkInfo;
        BranchInfo mMainBranch;
        BranchInfo mWorkingBranch;
        bool mIsGluonMode;
        BranchesListModel mModel = BranchesListModel.BuildEmpty();
        ButtonTooltipData mButtonTooltipData = new ButtonTooltipData();
        UVCSToolbarButtonData mDropDownButtonData = UVCSToolbarButtonData.BuildDefault();

        readonly object mModelLock = new object();
        readonly UVCSPlugin mUVCSPlugin;

        static readonly ILog mLog = PlasticApp.GetLogger("PlasticWindow");

        class ButtonTooltipData
        {
            public string WorkingObjectType { get; set; }
            public string WorkingObjectName { get; set; }
            public string RepositoryName { get; set; }
            public string ServerName { get; set; }
            public string Comment { get; set; }
            public string PendingChangesInfo { get; set; }
            public string IncomingChangesInfo { get; set; }
        }
    }
}
