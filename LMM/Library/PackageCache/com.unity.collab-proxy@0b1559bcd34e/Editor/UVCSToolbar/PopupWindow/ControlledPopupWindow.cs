using System;

using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList;
using Unity.PlasticSCM.Editor.Toolbar.PopupWindow.Operations;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow
{
    internal class ControlledPopupWindow : PopupWindowContent, BranchesTreeView.IClickListener
    {
        internal ControlledPopupWindow(
            ControlledPopupOperations operations,
            Action<ControlledPopupWindow> refreshBranches,
            Func<BranchInfo> fetchWorkingBranch,
            Action repaintToolbar,
            Vector2 size)
        {
            mOperations = operations;
            mRefreshBranches = refreshBranches;
            mRepaintToolbar = repaintToolbar;
            mSize = size;

            mProgressBar = new PopupupProgressBar(Repaint);
            mTreeView = new BranchesTreeView(this);
            mTreeView.SetWorkingBranch(fetchWorkingBranch);

            mEmptyState = new NoBranchesEmptyState(CreateBranch, Repaint);
            mLoadingEmptyState = new LoadingEmptyState(Repaint);
            mErrorEmptyState = new ErrorEmptyState(RefreshBranches, Repaint);

            mDelayedFilterAction = new DelayedActionBySecondsRunner(
                DelayedSearchChanged,
                UnityConstants.SEARCH_DELAYED_INPUT_ACTION_INTERVAL);
        }

        public override Vector2 GetWindowSize()
        {
            return mSize;
        }

        internal void ShowProgressBar()
        {
            mProgressBar.Reset();
            mProgressBar.IsVisible = true;
            EditorApplication.update += RepaintOnce;
        }

        internal void HideProgressBar()
        {
            mProgressBar.IsVisible = false;
            EditorApplication.update += RepaintOnce;
        }

        internal void SetModel(BranchesListModel model)
        {
            mModel = model;

            if (model != null)
            {
                mTreeView.SetBranches(model.Branches);
                mErrorEmptyState.SetError(model.Exception);
            }

            Repaint();
        }

        void Repaint()
        {
            if (editorWindow == null)
                return;

            editorWindow.Repaint();
        }

        void RepaintOnce()
        {
            Repaint();
            EditorApplication.update -= RepaintOnce;
        }

        public override void OnOpen()
        {
            mTreeView.Reload();

            EditorApplication.update += mProgressBar.OnEditorApplicationUpdate;
        }

        public override void OnClose()
        {
            EditorApplication.update -= mProgressBar.OnEditorApplicationUpdate;
            RepaintToolbar();
        }

        void BranchesTreeView.IClickListener.OnItemClicked(BranchTreeViewItem item)
        {
            mOperations.SwitchToBranch(item.BranchInfo, mModel.RepSpec);
            editorWindow.Close();
        }

        void RepaintToolbar()
        {
            if (mRepaintToolbar == null)
                return;

            mRepaintToolbar();
        }

        void CreateBranch()
        {
            ExecuteAndClosePopup(() => mOperations.CreateBranch(mSearchTerm));
        }

        void RefreshBranches()
        {
            mRefreshBranches(this);
        }

        void DelayedSearchChanged()
        {
            mTreeView.SetSearchString(mSearchTerm);
            mModel.ApplyFilter(new Filter(mSearchTerm));
            mTreeView.SetBranches(mModel.Branches);
            mTreeView.Reload();
        }

        public override void OnGUI(Rect rect)
        {
            if (Keyboard.IsKeyPressed(Event.current, KeyCode.Escape))
            {
                editorWindow.Close();
                return;
            }

#if UNITY_EDITOR_WIN && UNITY_6000_0_OR_NEWER && !UNITY_6000_3_OR_NEWER
            // Workaround for Unity 6.0-6.2 on Windows: close popup if mouse goes above toolbar
            if (ToolbarMouseBoundary.IsAboveToolbar(rect, Event.current.mousePosition))
            {
                editorWindow.Close();
                return;
            }
#endif

            EditorGUILayout.BeginVertical();

            GUILayout.Space(1);

            Rect checkinRect;

            if (PopupWindowDrawing.DrawMenuItem(
                    PlasticLocalization.Name.CheckinPendingChanges.GetString(),
                    Images.GetPendingChangesIcon(),
                    ToolbarOperationsShortcut.GetPendingChangesShortcutString(),
                    out checkinRect))
            {
                ExecuteAndClosePopup(mOperations.ShowPendingChangesView);
                return;
            }

            Rect incomingChangesRect;

            if (PopupWindowDrawing.DrawMenuItem(
                    PlasticLocalization.Name.ViewIncomingChanges.GetString(),
                    Images.GetOutOfSyncIcon(),
                    ToolbarOperationsShortcut.GetIncomingChangesShortcutString(),
                    out incomingChangesRect))
            {
                ExecuteAndClosePopup(mOperations.ShowIncomingChangesView);
                return;
            }

            Rect delimiterRect = new Rect(
                incomingChangesRect.x,
                incomingChangesRect.y + incomingChangesRect.height,
                incomingChangesRect.width,
                PopupWindowDrawing.DELIMITER_HEIGHT);

            PopupWindowDrawing.DrawDelimiterRect(
                delimiterRect,
                UnityStyles.Colors.SplitLineColor);

            mLastHoveredMenuIndex = PopupWindowDrawing.RepaintWhenHoveredMenuItemChanged(
                editorWindow.Repaint,
                mLastHoveredMenuIndex,
                checkinRect,
                incomingChangesRect);

            if (mProgressBar.IsVisible)
            {
                float progressWidth = delimiterRect.width * mProgressBar.Progress;
                Rect progressRect = new Rect(
                    delimiterRect.x,
                    delimiterRect.y,
                    progressWidth,
                    PopupWindowDrawing.PROGRESS_BAR_HEIGHT);

                GUIStyle progressBarStyle = GUI.skin.FindStyle("ProgressBarBar");
                if (progressBarStyle != null)
                {
                    GUI.DrawTexture(progressRect, progressBarStyle.normal.background);
                }
            }

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            GUI.SetNextControlName("SearchField");

            Rect searchRect = GUILayoutUtility.GetRect(
                new GUIContent(string.Empty),
                UnityStyles.EditorToolbar.Popup.SearchField,
                GUILayout.ExpandWidth(true));

            string previousSearchTerm = mSearchTerm;
            string newSearchTerm = GUI.TextField(
                searchRect,
                mSearchTerm);

            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Text);
            if (GUILayout.Button(
                PlasticLocalization.Name.NewBranchButton.GetString(),
                GUILayout.ExpandWidth(false),
                GUILayout.MinWidth(110)))
            {
                CreateBranch();
                return;
            }

            if (newSearchTerm != previousSearchTerm)
            {
                mSearchTerm = newSearchTerm;
                mDelayedFilterAction.Run();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            DrawBranchesModel(mModel);

            EditorGUILayout.EndVertical();

            EditorGUI.FocusTextInControl("SearchField");

#if UNITY_EDITOR_WIN && UNITY_6000_0_OR_NEWER && !UNITY_6000_3_OR_NEWER
            // Request continuous repaint to track mouse position even outside popup rect
            if (editorWindow != null)
                editorWindow.Repaint();
#endif
        }

        void DrawBranchesModel(BranchesListModel model)
        {
            Rect treeViewRect = GUILayoutUtility.GetRect(
                0,
                0,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (model == null || model.IsLoading)
            {
                mLoadingEmptyState.OnGUI(treeViewRect);
                return;
            }

            if (model.HasErrors)
            {
                mErrorEmptyState.OnGUI(treeViewRect);
                return;
            }

            if (model.IsEmpty)
            {
                mEmptyState.OnGUI(treeViewRect);
                return;
            }

            mTreeView.OnGUI(treeViewRect);
        }

        void ExecuteAndClosePopup(Action action)
        {
            editorWindow.Close();
            EditorDispatcher.Dispatch(action);
        }

        Action mRepaintToolbar;

        readonly PopupupProgressBar mProgressBar;
        readonly BranchesTreeView mTreeView;
        readonly ControlledPopupOperations mOperations;
        readonly NoBranchesEmptyState mEmptyState;
        readonly LoadingEmptyState mLoadingEmptyState;
        readonly ErrorEmptyState mErrorEmptyState;
        readonly DelayedActionBySecondsRunner mDelayedFilterAction;
        readonly Action<ControlledPopupWindow> mRefreshBranches;
        readonly Vector2 mSize;

        BranchesListModel mModel = BranchesListModel.BuildEmpty();
        string mSearchTerm = string.Empty;
        int mLastHoveredMenuIndex = -1;
    }
}
