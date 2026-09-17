using UnityEditor;
using UnityEngine;

using Codice.Client.Commands;
using Codice.Client.Common;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.BrowseRepository;
using PlasticGui.WorkspaceWindow.QueryViews;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Views.BrowseRepository
{
    internal class BrowseRepositoryPanel : FillBrowseRepositoryView.IUpdateView
    {
        internal BrowseRepositoryPanel(
            WorkspaceInfo wkInfo,
            IGetRepositorySpec getRepositorySpec,
            EditorWindow parentWindow)
        {
            mWkInfo = wkInfo;
            mGetRepositorySpec = getRepositorySpec;

            mParentWindow = parentWindow;
            mGuiMessage = new UnityPlasticGuiMessage();
            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);

            BuildComponents();

            mProgressControls = new ProgressControlsForViews();
        }

        internal void ClearInfo()
        {
            ClearBrowseRepositoryView(mBrowseRepositoryTreeView);

            mParentWindow.Repaint();
        }

        internal void UpdateInfo(MarkerExtendedInfo selectedLabelInfo)
        {
            mSelectedLabelInfo = selectedLabelInfo;

            mFillBrowseRepositoryView.FillView(
                mWkInfo,
                mGetRepositorySpec.Get(),
                selectedLabelInfo.Changeset,
                this,
                mProgressControls);

            mParentWindow.Repaint();
        }

        internal void OnEnable() { }

        internal void OnDisable()
        {
            TreeHeaderSettings.Save(
                mBrowseRepositoryTreeView.multiColumnHeader.state,
                UnityConstants.BROWSE_REPOSITORY_TABLE_SETTINGS_NAME);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI(float maxWidth = 10000, float maxHeight = 10000)
        {
            EditorGUILayout.BeginVertical();

            Rect viewRect = OverlayProgress.CaptureViewRectangle();

            DoActionsToolbar(
                mBrowseRepositoryTreeView,
                mProgressControls);

            DoBrowseRepositoryViewArea(
                mBrowseRepositoryTreeView,
                mEmptyStatePanel,
                mProgressControls.IsOperationRunning(),
                maxWidth,
                maxHeight);

            if (mProgressControls.HasNotification())
            {
                DrawProgressForViews.ForNotificationArea(
                    mProgressControls.ProgressData);
            }

            EditorGUILayout.EndVertical();

            if (mProgressControls.IsOperationRunning())
            {
                OverlayProgress.DoOverlayProgress(
                    viewRect,
                    mProgressControls.ProgressData.ProgressPercent,
                    mProgressControls.ProgressData.ProgressMessage);
            }
        }

        void FillBrowseRepositoryView.IUpdateView.UpdateTree(
            TreeContent treeContent, BrowseRepositoryTree browseRepositoryTree)
        {
            if (mSelectedLabelInfo.Changeset != treeContent.ChangesetId)
                return;

            ClearBrowseRepositoryView(mBrowseRepositoryTreeView);

            UpdateBrowseRepositoryView(browseRepositoryTree, mBrowseRepositoryTreeView);
        }

        void FillBrowseRepositoryView.IUpdateView.UpdateBranches()
        {
            mBrowseRepositoryTreeView.Repaint();
        }

        static void ClearBrowseRepositoryView(
            BrowseRepositoryTreeView browseRepositoryTreeView)
        {
            browseRepositoryTreeView.ClearModel();
        }

        static void UpdateBrowseRepositoryView(
            BrowseRepositoryTree browseRepositoryTree,
            BrowseRepositoryTreeView browseRepositoryTreeView)
        {
            browseRepositoryTreeView.BuildModel(browseRepositoryTree);

            browseRepositoryTreeView.Reload();

            browseRepositoryTreeView.SetExpanded(1, true);
        }

        void DoActionsToolbar(
            BrowseRepositoryTreeView browseRepositoryTreeView,
            ProgressControlsForViews progressControls)
        {
            //EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            //GUILayout.FlexibleSpace();

            // TODO: search bar

            //EditorGUILayout.EndHorizontal();
        }

        static void DoBrowseRepositoryViewArea(
            BrowseRepositoryTreeView browseRepositoryTreeView,
            EmptyStatePanel emptyStatePanel,
            bool isOperationRunning,
            float maxWidth,
            float maxHeight)
        {
            GUI.enabled = !isOperationRunning;

            Rect rect = GUILayoutUtility.GetRect(0, maxWidth, 0, maxHeight);

            if (Event.current.type == EventType.Layout)
                emptyStatePanel.UpdateContent(GetEmptyStateMessage(browseRepositoryTreeView));

            if (emptyStatePanel.IsEmpty())
            {
                browseRepositoryTreeView.OnGUI(rect);
            }
            else
            {
                emptyStatePanel.OnGUI(rect);
            }

            GUI.enabled = true;
        }

        static string GetEmptyStateMessage(BrowseRepositoryTreeView browseRepositoryTreeView)
        {
            if (browseRepositoryTreeView.GetRows().Count > 0)
                return string.Empty;

            return PlasticLocalization.Name.NoContentToBrowseExplanation.GetString();
        }

        void BuildComponents()
        {
            BrowseRepositoryHeaderState headerState =
                BrowseRepositoryHeaderState.GetDefault();

            TreeHeaderSettings.Load(
                headerState,
                UnityConstants.BROWSE_REPOSITORY_TABLE_SETTINGS_NAME,
                (int)BrowseRepositoryColumn.Item,
                true);

            mBrowseRepositoryTreeView = new BrowseRepositoryTreeView(
                () => mFillBrowseRepositoryView.LoadQueuedBranches(this),
                headerState);

            mBrowseRepositoryTreeView.Reload();
        }

        MarkerExtendedInfo mSelectedLabelInfo;
        BrowseRepositoryTreeView mBrowseRepositoryTreeView;

        readonly FillBrowseRepositoryView mFillBrowseRepositoryView = new FillBrowseRepositoryView();
        readonly EmptyStatePanel mEmptyStatePanel;
        readonly ProgressControlsForViews mProgressControls;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly EditorWindow mParentWindow;
        readonly WorkspaceInfo mWkInfo;
        readonly IGetRepositorySpec mGetRepositorySpec;
    }
}
