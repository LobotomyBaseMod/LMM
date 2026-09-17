using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using Codice.Client.BaseCommands;
using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.ExplorerTree;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.Client.Common.WebApi;
using Codice.Client.IssueTracker;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.Help;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.BranchExplorer.Search;
using PlasticGui.WorkspaceWindow.CodeReview;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Zoom;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal interface IBrExNavigate
    {
        void NavigateToShape(VirtualShape shape, bool animate = true);
    }

    internal class BranchExplorerViewer : VisualElement,
        IBrExNavigate,
        BrExShape.IBrExShapeClickListener,
        BranchExplorerSearch.IBrExNavigate
    {
        internal VirtualCanvas VirtualCanvas { get { return mVirtualCanvas; } }
        internal BrExTree ExplorerTree { get { return mExplorerTree; } }
        internal Button OptionsButton { get { return mOptionsButton; } }
        internal Button ZoomInButton { get { return mZoomInButton; } }
        internal Button ZoomOutButton { get { return mZoomOutButton; } }
        internal Button HomeButton { get { return mHomeButton; } }
        internal BrExZoom Zoom { get { return mZoom; } }
        internal BrExLayout ExplorerLayout { get { return mExplorerLayout; } }
        internal BranchExplorerSelection Selection { get {return mSelectionHandler; } }
        internal BranchExplorerSearch Search { get { return mSearchHandler; } }
        internal BranchExplorerViewer(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            IIssueTrackerExtension issueTracker,
            BranchExplorerSelection selectionHandler,
            IVirtualCanvasUpdateVisualsListener loadedListener,
            IVirtualCanvasUpdateListener updatedListener,
            Action onOptionsClicked = null)
        {
            mWkInfo = wkInfo;
            mRepSpec = repSpec;
            mIssueTracker = issueTracker;
            mSelectionHandler = selectionHandler;
            mOnOptionsClicked = onOptionsClicked;

            CreateGUI(loadedListener, updatedListener);

            mSearchHandler = new BranchExplorerSearch(this, GetVirtualShapes);
        }

        internal void InitializeBranchExplorerViewMenu(
            IPlasticWebRestApi restApi,
            WorkspaceInfo wkInfo,
            EditorWindow window,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher switcher,
            IMergeViewLauncher mergeViewLauncher,
            BranchExplorerView branchExplorerView,
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
            mBranchExplorerViewMenu = new BranchExplorerViewMenu(
                restApi,
                wkInfo,
                mRepSpec,
                window,
                workspaceWindow,
                switcher,
                mergeViewLauncher,
                branchExplorerView,
                this,
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
        }

        internal void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mBranchExplorerViewMenu?.UpdateRepositorySpec(repSpec);
            mTaskLoader?.UpdateRepositorySpec(repSpec);
            mUserNameResolver?.UpdateRepositorySpec(repSpec);
            mCommentResolver?.UpdateRepositorySpec(repSpec);
        }

        internal void SetWorkspaceUIConfiguration(
            WorkspaceUIConfiguration workspaceUIConfiguration)
        {
            mConfig = workspaceUIConfiguration;
            mZoom.SetWorkspaceUIConfiguration(workspaceUIConfiguration);
            mSearchHandler.SetWorkspaceUIConfiguration(workspaceUIConfiguration);
        }

        internal void SetLayout(BrExLayout brExLayout, BrExTree brExTree)
        {
            mExplorerLayout = brExLayout;
            mExplorerTree = brExTree;
        }

        internal void SetColorConfig(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec)
        {
            mColorProvider = new ColorProvider(
                repSpec, wkInfo, mExplorerTree);

            UpdateColorProviderRulesConfiguration(
                mColorProvider, mConfig.Rules);
        }

        internal void UpdateColorConfig()
        {
            if (mColorProvider == null || mConfig == null)
                return;

            UpdateColorProviderRulesConfiguration(
                mColorProvider,
                mConfig.Rules);
        }

        internal ChangesetDrawInfo SaveRelativePosition()
        {
            ChangesetDrawInfo changesetDrawInfo = FindReferenceChangeset.FromChangesetShapes(
                mVirtualCanvas.GetChangesetShapes());

            if (changesetDrawInfo == null)
                return null;

            mRelativePositionShape = BuildRelativePositionChangeset(
                changesetDrawInfo,
                mZoom);

            return changesetDrawInfo;
        }

        internal void RecalculateLayout(RepositorySpec repSpec)
        {
            if (mExplorerTree == null)
                return;

            mExplorerLayout = PlasticGui.Plastic.API.GetBranchExplorerLayout(
                repSpec, mExplorerTree, mConfig.DisplayOptions);
        }

        internal void NavigateToHome(bool selectShape = true, bool animate = true)
        {
            if (mExplorerLayout == null)
                return;

            ObjectDrawInfo homeDrawInfo = FindDrawInfo.
                GetHomeDrawInfo(
                    mExplorerLayout.ChangesetDraws,
                    mExplorerLayout.BranchDraws);

            if (homeDrawInfo == null || homeDrawInfo.Visual == null)
                return;

            NavigateToShape((VirtualShape)homeDrawInfo.Visual, selectShape, animate);
        }

        internal void RestoreRelativePosition()
        {
            if (mRelativePositionShape == null)
                return;

            try
            {
                ChangesetDrawInfo newDraw = GetChangesetDrawToRestorePosition(
                    mRelativePositionShape.DrawInfo,
                    mVirtualCanvas.Layout.ChangesetDraws);

                if (newDraw == null)
                    return;

                float x = (newDraw.Bounds.X - mRelativePositionShape.RelativePosition.x) * mZoom.ZoomLevel;
                float y = (newDraw.Bounds.Y - mRelativePositionShape.RelativePosition.y) * mZoom.ZoomLevel;

                mZoom.Offset = new Vector2(x, y);
            }
            finally
            {
                mRelativePositionShape = null;
            }
        }

        internal void UpdateLayout()
        {
            ResetTaskLoader();
            ResetUserNameResolver();
            ResetCommentResolver();

            if (mExplorerLayout == null)
                return;

            VirtualCanvasFiller.FillVirtualCanvas(
                mVirtualCanvas,
                mExplorerLayout,
                mConfig,
                mColorProvider,
                mTaskLoader,
                mUserNameResolver,
                mCommentResolver);

            SearchAndSelectionUpdater.Update(
                mSearchHandler,
                mSelectionHandler,
                GetVirtualShapes);
        }

        internal void Dispose()
        {
            if (mTaskLoader != null)
                mTaskLoader.TasksLoaded -= TaskLoadFinished;

            if (mUserNameResolver != null)
                mUserNameResolver.UserNamesResolved -= UserNamesResolved;

            if (mCommentResolver != null)
                mCommentResolver.ChangesetCommentsResolved -= ChangesetCommentsResolved;

            mScrollViewer.OnScrollChanged -= OnScrollChanged;
            mScrollViewer.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            mScrollViewer.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            mScrollViewer.UnregisterCallback<PointerMoveEvent>(
                OnPointerMoveResetPressedState);

            mScrollViewer.Dispose();
            mExplorerLayout = null;
            mVirtualCanvas.Dispose();
            mZoom.Dispose();
            mPan.Dispose();
        }

        internal void SelectObject(RepObjectInfo objectToSelect)
        {
            /*if (objectToSelect == null)
                return;

            ObjectDraw drawToSelect = GetDrawObject(objectToSelect);

            if (drawToSelect == null)
                return;

            SelectObject(drawToSelect);*/
        }

        internal void SelectObject(ObjectDrawInfo targetObject)
        {
            if (targetObject == null)
                return;

            VirtualShape targetVirtualShape =
                (VirtualShape)targetObject.Visual;

            if (targetVirtualShape == null)
                return;

            NavigateToShape(targetVirtualShape);
        }

        void BranchExplorerSearch.IBrExNavigate.NavigateToShape(IVirtualShape shape, bool animate)
        {
            NavigateToShape((VirtualShape)shape, animate);
        }

        void BrExShape.IBrExShapeClickListener.OnShapeClicked(
            VirtualShape shape, bool isMultiSelection)
        {
            SelectShape(shape, isMultiSelection);
        }

        void BrExShape.IBrExShapeClickListener.OnContextMenuRequested()
        {
            mBranchExplorerViewMenu.Popup();
            mPendingPointerReset = true;
        }

        void OnPointerMoveResetPressedState(PointerMoveEvent evt)
        {
            if (!mPendingPointerReset)
                return;

            mPendingPointerReset = false;

            // The IMGUI GenericMenu consumes the right-button PointerUp,
            // leaving UIElements with pressedButtons bit 2 (right button)
            // permanently set. This breaks hover detection on shapes
            // because PointerEnterEvent.pressedButtons != 0.
            // A synthetic MouseUp clears the stale state.
            schedule.Execute(() =>
            {
                if (panel == null)
                    return;

                using (var upEvt = MouseUpEvent.GetPooled(
                    new Event { type = EventType.MouseUp, button = 1 }))
                {
                    panel.visualTree.SendEvent(upEvt);
                }
            });
        }

        internal void Redraw()
        {
            mVirtualCanvas.Redraw();
        }

        void BrExShape.IBrExShapeClickListener.OnShapeDoubleClicked()
        {
            mBranchExplorerViewMenu.ExecuteDefaultAction();
        }

        void IBrExNavigate.NavigateToShape(VirtualShape shape, bool animate)
        {
            NavigateToShape(shape, animate);
        }

        void SelectShape(
            VirtualShape shape, bool isMultiSelection)
        {
            mSelectionHandler.SelectShape(shape, isMultiSelection);
        }

        void ResetTaskLoader()
        {
            if (mTaskLoader != null)
            {
                mTaskLoader.Clean();
                return;
            }

            if (mIssueTracker == null)
                return;

            mTaskLoader = new AsyncTaskLoader(
                mRepSpec, new RequestTaskDelegate(RequestTaskInfo),
                mIssueTracker.GetWorkingMode());

            mTaskLoader.TasksLoaded += TaskLoadFinished;
        }

        void ResetUserNameResolver()
        {
            if (mUserNameResolver != null)
            {
                mUserNameResolver.Clean();
                return;
            }

            mUserNameResolver = new AsyncUserNameResolver(mRepSpec);

            mUserNameResolver.UserNamesResolved += UserNamesResolved;
        }

        void ResetCommentResolver()
        {
            if (mCommentResolver != null)
            {
                mCommentResolver.Clean();
                return;
            }

            mCommentResolver = new AsyncChangesetCommentResolver(mRepSpec);

            mCommentResolver.ChangesetCommentsResolved += ChangesetCommentsResolved;
        }

        Dictionary<string, PlasticTask> RequestTaskInfo(
            List<string> visibleBranches)
        {
            if (mConfig == null
                || !mConfig.DisplayOptions.DisplayTaskInfoOnBranches
                || mIssueTracker == null)
            {
                return null;
            }

            if (mIssueTracker.GetWorkingMode() != ExtensionWorkingMode.TaskOnBranch)
                return null;

            return mIssueTracker.Extension.GetTasksForBranches(visibleBranches);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (mBranchExplorerViewMenu.ProcessKeyActionIfNeeded(evt.ToIMGUIEvent()))
            {
                evt.StopPropagation();
                return;
            }

            if (!ProcessNavigationKeyEvent(evt))
                return;

            evt.StopPropagation();
        }

        void NavigateToShape(VirtualShape shape, bool selectShape = true, bool animate = true)
        {
            if (selectShape)
                SelectShape(shape, false);

            mZoom.ScrollIntoView(shape.BoundsForNavigation, animate);
        }

        bool ProcessNavigationKeyEvent(KeyDownEvent evt)
        {
            KeyboardActions.NavigationAction navigationAction =
                KeyboardActions.GetNavigationAction(evt);

            if (navigationAction == KeyboardActions.NavigationAction.None)
                return false;

            ObjectDrawInfo selectedObject =
                mSelectionHandler.GetSingleSelectedObject();

            if (selectedObject == null)
                return true;

            ObjectDrawInfo targetObject = null;

            switch (navigationAction)
            {
                case KeyboardActions.NavigationAction.MoveUp:
                    targetObject = FindDrawInfo.GetObjectOnTop(
                        selectedObject, mExplorerLayout);
                    break;
                case KeyboardActions.NavigationAction.MoveDown:
                    targetObject = FindDrawInfo.GetObjectOnBottom(
                        selectedObject, mExplorerLayout);
                    break;
                case KeyboardActions.NavigationAction.MoveRight:
                    targetObject = FindDrawInfo.GetObjectOnRight(
                        selectedObject, mExplorerLayout,
                        KeyboardEvents.IsShiftPressed(evt));
                    break;
                case KeyboardActions.NavigationAction.MoveLeft:
                    targetObject = FindDrawInfo.GetObjectOnLeft(
                        selectedObject, mExplorerLayout,
                        KeyboardEvents.IsShiftPressed(evt),
                        KeyboardEvents.IsAltPressed(evt),
                        mConfig.DisplayOptions.DisplayOnlyRelevantChangesets);
                    break;
                case KeyboardActions.NavigationAction.Escape:
                    targetObject = FindDrawInfo.GetAlternativeObject(
                        selectedObject, mExplorerLayout);
                    break;
                case KeyboardActions.NavigationAction.Home:
                    targetObject = FindDrawInfo.GetInitialChangeset(
                        selectedObject);
                    break;
                case KeyboardActions.NavigationAction.End:
                    targetObject = FindDrawInfo.GetEndChangeset(
                        selectedObject);
                    break;
            }

            if (targetObject != null)
            {
                SelectObject(targetObject);
            }

            return true;
        }

        void TaskLoadFinished()
        {
            EditorDispatcher.Dispatch(Redraw);
        }

        void UserNamesResolved()
        {
            EditorDispatcher.Dispatch(Redraw);
        }

        void ChangesetCommentsResolved()
        {
            EditorDispatcher.Dispatch(
                () => mVirtualCanvas.RedrawChangesetCommentShapes());
        }

        static void UpdateColorProviderRulesConfiguration(
            ColorProvider colorProvider, List<Rule> rules)
        {
            colorProvider.SetRulesConfiguration(rules);
        }

        void CreateGUI(
            IVirtualCanvasUpdateVisualsListener loadedListener,
            IVirtualCanvasUpdateListener updatedListener)
        {
            style.flexGrow = 1;

            mScrollViewer = new CanvasScrollView();
            mScrollViewer.OnScrollChanged += OnScrollChanged;
            mScrollViewer.focusable = true;
            mScrollViewer.tabIndex = 0;
            mScrollViewer.RegisterCallback<MouseDownEvent>(OnMouseDown);
            mScrollViewer.RegisterCallback<KeyDownEvent>(OnKeyDown);
            mScrollViewer.RegisterCallback<PointerMoveEvent>(
                OnPointerMoveResetPressedState);

            mScrollViewer.style.flexGrow = 1;

            mVirtualCanvas = new VirtualCanvas(
                mScrollViewer,
                loadedListener,
                updatedListener);
            mScrollViewer.ContentContainer.Add(mVirtualCanvas);

            VisualElement buttonsPanel = CreateButtonsPanel();
            mScrollViewer.Viewport.Add(buttonsPanel);

            Add(mScrollViewer);

            mZoom = new BrExZoom(
                mWkInfo,
                mScrollViewer,
                mVirtualCanvas);

            mPan = new BrExPan(
                mScrollViewer.Viewport,
                mZoom);

            if (mDebugRealizedShapes)
            {
                VisualElement statusPanel = CreateStatusPanel();
                statusPanel.schedule.Execute(UpdateStatus).Every(250);
                Add(statusPanel);
            }
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            mScrollViewer.Focus();
        }

        void OnScrollChanged(Vector2 value)
        {
            if (mVirtualCanvas == null)
                return;

            mVirtualCanvas.OnScrollChanged();
        }

        void UpdateStatus()
        {
            List<BrExShape> shapes = mVirtualCanvas.GetShapes();

            mStatusLabel.text = string.Format(
                "Realized {0} visual shapes", shapes.Count);
        }

        static ChangesetDrawInfo GetChangesetDrawToRestorePosition(
            ChangesetDrawInfo relativePositionShape,
            List<ChangesetDrawInfo> changesetDraws)
        {
            ChangesetDrawInfo result = FindDrawInfo.GetChangeset(
                changesetDraws, ((BrExChangeset)relativePositionShape.Tag).Id);

            if (result != null)
                return result;

            // not found -> It could be not visible due to a filter
            // try to find the nearest changeset
            result = FindDrawInfo.GetNearestChangeset(
                relativePositionShape, changesetDraws);

            if (result != null)
                return result;

            // show last visible changeset in this case
            return FindDrawInfo.GetNewestChangeset(changesetDraws);
        }

        static RelativePositionChangeset BuildRelativePositionChangeset(
            ChangesetDrawInfo drawInfo,
            BrExZoom zoom)
        {
            float x = drawInfo.Bounds.X - (zoom.Offset.x / zoom.ZoomLevel);
            float y = drawInfo.Bounds.Y - (zoom.Offset.y / zoom.ZoomLevel);

            return new RelativePositionChangeset(
                drawInfo,
                new Vector2(x, y));
        }

        VisualElement CreateButtonsPanel()
        {
            VisualElement buttonsPanel = new VisualElement();
            buttonsPanel.style.position = Position.Absolute;
            buttonsPanel.style.right = 20;
            buttonsPanel.style.bottom = 20;

            float buttonSize = 30;

            mOptionsButton = ControlBuilder.Button.CreateImageButton(
                Images.GetSettingsIcon(),
                PlasticLocalization.Name.BrexOptionsWindowTitle.GetString(),
                () => { mOnOptionsClicked?.Invoke(); });
            mOptionsButton.style.marginRight = 0;
            mOptionsButton.style.width = buttonSize;
            mOptionsButton.style.height = buttonSize;
            buttonsPanel.Add(mOptionsButton);

            mHomeButton = ControlBuilder.Button.CreateImageButton(
                Images.GetButtonHomeIcon(),
                PlasticLocalization.Name.GoHomeTooltip.GetString(),
                () => { NavigateToHome(); });
            mHomeButton.style.marginRight = 0;
            mHomeButton.style.width = buttonSize;
            mHomeButton.style.height = buttonSize;
            buttonsPanel.Add(mHomeButton);

            mZoomInButton = ControlBuilder.ButtonGroup.CreateImageTopButton(
                Images.GetZoomInIcon(),
                PlasticLocalization.Name.ZoomIn.GetString(),
                () => { mZoom.ZoomIn(); });
            mZoomInButton.style.marginRight = 0;
            mZoomInButton.style.width = buttonSize;
            mZoomInButton.style.height = buttonSize;
            buttonsPanel.Add(mZoomInButton);

            mZoomOutButton = ControlBuilder.ButtonGroup.CreateImageBottomButton(
                Images.GetZoomOutIcon(),
                PlasticLocalization.Name.ZoomOut.GetString(),
                () => { mZoom.ZoomOut(); });
            mZoomOutButton.style.width = buttonSize;
            mZoomOutButton.style.height = buttonSize;
            mZoomOutButton.style.marginRight = 0;
            mZoomOutButton.style.marginBottom = 0;
            buttonsPanel.Add(mZoomOutButton);

            return buttonsPanel;
        }

        VisualElement CreateStatusPanel()
        {
            VisualElement statusPanel = new VisualElement();

            statusPanel.style.backgroundColor = new StyleColor(new Color(0.6f, 0.329f, 0.0f));

            mStatusLabel = new Label("Ready");
            mStatusLabel.style.color = new StyleColor(Color.white);
            mStatusLabel.style.marginBottom = mStatusLabel.style.marginTop = mStatusLabel.style.marginLeft = mStatusLabel.style.marginRight = 5;

            statusPanel.Add(mStatusLabel);

            return statusPanel;
        }

        IEnumerable GetVirtualShapes()
        {
            return mVirtualCanvas.VirtualChildren;
        }

        class RelativePositionChangeset
        {
            internal ChangesetDrawInfo DrawInfo { get; private set; }
            internal Vector2 RelativePosition { get; private set; }

            internal RelativePositionChangeset(ChangesetDrawInfo drawInfo, Vector2 relativePosition)
            {
                DrawInfo = drawInfo;
                RelativePosition = relativePosition;
            }
        }

        // set to true to enable a label showing the number
        // of currently realized shapes in the canvas
        bool mDebugRealizedShapes;

        AsyncTaskLoader mTaskLoader;

        RelativePositionChangeset mRelativePositionShape;
        AsyncUserNameResolver mUserNameResolver;
        AsyncChangesetCommentResolver mCommentResolver;

        Button mOptionsButton;
        Button mHomeButton;
        Button mZoomInButton;
        Button mZoomOutButton;
        BrExTree mExplorerTree;
        BrExLayout mExplorerLayout;
        WorkspaceUIConfiguration mConfig;
        ColorProvider mColorProvider;

        CanvasScrollView mScrollViewer;
        VirtualCanvas mVirtualCanvas;
        BrExZoom mZoom;
        BrExPan mPan;
        BranchExplorerViewMenu mBranchExplorerViewMenu;
        bool mPendingPointerReset;
        readonly BranchExplorerSearch mSearchHandler;
        readonly BranchExplorerSelection mSelectionHandler;
        readonly IIssueTrackerExtension mIssueTracker;
        readonly Action mOnOptionsClicked;
        readonly RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;

        Label mStatusLabel;
    }
}
