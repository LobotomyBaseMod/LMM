using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.BaseCommands.BranchExplorer;
using PlasticGui;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class ChangesetCommentShape : BrExShape
    {
        // internal for testing purposes
        internal bool IsExpanded { get { return mIsExpanded; } }

        internal ChangesetDrawInfo ChangesetDraw
        {
            get { return VirtualShape.DrawInfo as ChangesetDrawInfo; }
        }

        internal ChangesetCommentShape(
            VirtualShape virtualShape,
            AsyncChangesetCommentResolver commentResolver) : base(virtualShape)
        {
            mCommentResolver = commentResolver;

            mCommentContainer = new VisualElement();
            mCommentContainer.style.position = Position.Absolute;
            mCommentContainer.style.overflow = Overflow.Hidden;
            mCommentContainer.style.borderTopLeftRadius = CornerRadius;
            mCommentContainer.style.borderTopRightRadius = CornerRadius;
            mCommentContainer.style.borderBottomLeftRadius = CornerRadius;
            mCommentContainer.style.borderBottomRightRadius = CornerRadius;
            mCommentContainer.style.paddingLeft = 0;
            mCommentContainer.style.paddingRight = 0;
            mCommentContainer.style.paddingTop = 0;
            mCommentContainer.style.paddingBottom = 0;
            mCommentContainer.style.width = CollapsedWidth;

            mCommentLabel = new Label();
            mCommentLabel.style.fontSize = DefaultFontSize;
            mCommentLabel.style.color = UnityStyles.Colors.SecondaryLabel;
            mCommentLabel.style.marginBottom = 0;
            mCommentLabel.style.marginTop = 0;
            mCommentLabel.style.paddingBottom = 0;
            mCommentLabel.style.paddingTop = 0;
            mCommentLabel.style.whiteSpace = WhiteSpace.NoWrap;
            mCommentLabel.style.textOverflow = TextOverflow.Ellipsis;
            mCommentLabel.style.overflow = Overflow.Hidden;
            mCommentLabel.pickingMode = PickingMode.Ignore;

            mCommentLabel.style.transitionProperty =
                new List<StylePropertyName> { new StylePropertyName("color") };
            mCommentLabel.style.transitionDuration =
                new List<TimeValue> { new TimeValue(ColorTransitionMs, TimeUnit.Millisecond) };

            mScrollView = new ScrollView(ScrollViewMode.Vertical);
            mScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            mScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            mScrollView.Add(mCommentLabel);
            mCommentContainer.Add(mScrollView);
            Add(mCommentContainer);

            mHitTestOverlay = new VisualElement();
            mHitTestOverlay.style.position = Position.Absolute;
            mHitTestOverlay.pickingMode = PickingMode.Position;
            Add(mHitTestOverlay);

            mHitTestOverlay.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            mHitTestOverlay.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            mCommentContainer.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            mCommentContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            mCommentContainer.RegisterCallback<WheelEvent>(OnWheelEvent);
            mCommentContainer.RegisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        internal override void Dispose()
        {
            CancelScheduledCollapse();
            mHitTestOverlay.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            mHitTestOverlay.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            mCommentContainer.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            mCommentContainer.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            mCommentContainer.UnregisterCallback<WheelEvent>(OnWheelEvent);
            mCommentContainer.UnregisterCallback<GeometryChangedEvent>(OnContainerGeometryChanged);
            UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            base.Dispose();
        }

        internal override void Redraw()
        {
            UpdateCommentText();
            UpdateLayout();
            UpdateVisibilityForZoom();
            base.Redraw();
        }

        protected override void GenerateVisualContent(Painter2D painter)
        {
            if (!DisplayOptions.DisplayChangesetComments)
                return;

            RequestResolveComment();
        }

        internal override bool HasContextMenu() => false;

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return mCommentContainer.localBound.Contains(localPoint);
        }

        internal override void OnScrollChanged()
        {
            UpdateVisibilityForZoom();
        }

        protected internal override VirtualShape GetVirtualShapeForSelection()
        {
            VirtualCanvas canvas = GetFirstAncestorOfType<VirtualCanvas>();
            if (canvas == null)
                return null;

            foreach (BrExShape brExShape in canvas.GetShapes())
            {
                if (brExShape is ChangesetShape changesetShape &&
                    changesetShape.ChangesetDraw == ChangesetDraw)
                    return brExShape.VirtualShape;
            }

            return null;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (!DisplayOptions.DisplayChangesetComments)
                return;

            RequestResolveComment();
            UpdateCommentText();
            UpdateLayout();
            UpdateVisibilityForZoom();
        }

        void OnContainerGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateHitTestOverlaySize();
        }

        // internal for testing purposes
        internal void OnPointerEnter(PointerEnterEvent evt)
        {
            // don't expand while dragging
            if (evt.pressedButtons != 0)
                return;

            Expand();
        }

        // internal for testing purposes
        internal void OnPointerLeave(PointerLeaveEvent evt)
        {
            // Don't collapse while dragging (e.g. scrollbar thumb drag
            // that moves the pointer outside the container bounds).
            if (mIsExpanded && evt.pressedButtons != 0)
                return;

            ScheduleCollapse();
        }

        void OnWheelEvent(WheelEvent evt)
        {
            if (!mIsExpanded)
                return;

            float contentHeight = mScrollView.contentContainer.layout.height;
            float viewportHeight = mScrollView.contentViewport.layout.height;

            if (contentHeight <= viewportHeight)
                return;

            evt.StopPropagation();
        }

        internal void Expand()
        {
            CancelScheduledCollapse();

            if (mIsExpanded)
                return;

            if (string.IsNullOrEmpty(mCommentLabel.text))
                return;

            if (!IsVisibleForZoomLevel())
                return;

            mIsExpanded = true;

            mCommentLabel.text = mOriginalComment;
            mCommentLabel.style.color = UnityStyles.Colors.Label;
            mCommentLabel.style.whiteSpace = WhiteSpace.Normal;
            mCommentLabel.style.textOverflow = TextOverflow.Clip;
            mCommentLabel.style.overflow = Overflow.Visible;

            mCommentContainer.style.backgroundColor =
                UnityStyles.Colors.BranchExplorer.ControlBackgroundColor;

            Color borderColor = BrExColors.Label.GetCaptionBorderColor();

            mCommentContainer.style.borderTopColor = borderColor;
            mCommentContainer.style.borderBottomColor = borderColor;
            mCommentContainer.style.borderLeftColor = borderColor;
            mCommentContainer.style.borderRightColor = borderColor;

            mCommentContainer.style.borderTopWidth = ExpandedBorderWidth;
            mCommentContainer.style.borderBottomWidth = ExpandedBorderWidth;
            mCommentContainer.style.borderLeftWidth = ExpandedBorderWidth;
            mCommentContainer.style.borderRightWidth = ExpandedBorderWidth;

            mCommentContainer.style.paddingLeft = ExpandedPadding;
            mCommentContainer.style.paddingRight = ExpandedPadding;
            mCommentContainer.style.paddingTop = ExpandedPadding;
            mCommentContainer.style.paddingBottom = ExpandedPadding;
            mCommentContainer.style.width = StyleKeyword.Auto;
            mCommentContainer.style.maxWidth = MaxExpandedWidth + 2 * ExpandedPadding;
            mCommentContainer.style.maxHeight = MaxExpandedHeight;

            mScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            mScrollView.style.maxHeight =
                MaxExpandedHeight - 2 * (ExpandedPadding + ExpandedBorderWidth);

            // Let pointer events pass through the overlay so the
            // ScrollView and its scrollbar receive clicks/drags directly.
            // The container's own enter/leave callbacks handle collapse.
            mHitTestOverlay.pickingMode = PickingMode.Ignore;

            UpdateLayout();
            BringToFront();
        }

        internal void ScheduleCollapse()
        {
            CancelScheduledCollapse();
            mScheduledCollapse = schedule.Execute(Collapse).StartingIn(CollapseDelayMs);
        }

        void Collapse()
        {
            if (!mIsExpanded)
                return;

            mIsExpanded = false;

            mCommentLabel.text = CommentFormatter.GetFormattedComment(mOriginalComment);
            mCommentLabel.style.color = UnityStyles.Colors.SecondaryLabel;
            mCommentLabel.style.whiteSpace = WhiteSpace.NoWrap;
            mCommentLabel.style.textOverflow = TextOverflow.Ellipsis;
            mCommentLabel.style.overflow = Overflow.Hidden;

            mCommentContainer.style.paddingLeft = 0;
            mCommentContainer.style.paddingRight = 0;
            mCommentContainer.style.paddingTop = 0;
            mCommentContainer.style.paddingBottom = 0;
            mCommentContainer.style.borderTopWidth = 0;
            mCommentContainer.style.borderBottomWidth = 0;
            mCommentContainer.style.borderLeftWidth = 0;
            mCommentContainer.style.borderRightWidth = 0;

            mCommentContainer.style.backgroundColor = Color.clear;

            mCommentContainer.style.width = CollapsedWidth;
            mCommentContainer.style.maxWidth = new StyleLength(StyleKeyword.None);
            mCommentContainer.style.maxHeight = CollapsedHeight;
            mCommentContainer.style.overflow = Overflow.Hidden;

            mScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            mScrollView.style.maxHeight = new StyleLength(StyleKeyword.None);
            mScrollView.scrollOffset = Vector2.zero;

            // Restore the overlay so it captures enter/leave for
            // the collapsed state hit testing.
            mHitTestOverlay.pickingMode = PickingMode.Position;

            UpdateLayout();

            VirtualCanvas canvas = parent as VirtualCanvas;
            canvas?.RestoreZOrder(this);
        }

        void CancelScheduledCollapse()
        {
            if (mScheduledCollapse == null)
                return;

            mScheduledCollapse.Pause();
            mScheduledCollapse = null;
        }

        void UpdateCommentText()
        {
            if (!DisplayOptions.DisplayChangesetComments)
            {
                mCommentLabel.text = string.Empty;
                mOriginalComment = string.Empty;
                return;
            }

            BrExChangeset changeset = ChangesetDraw.Tag as BrExChangeset;
            if (changeset == null)
            {
                mCommentLabel.text = string.Empty;
                mOriginalComment = string.Empty;
                return;
            }

            string comment = mCommentResolver.GetResolvedComment(changeset.Id);
            mOriginalComment = comment ?? string.Empty;

            mCommentLabel.text = mIsExpanded
                ? mOriginalComment
                : CommentFormatter.GetFormattedComment(mOriginalComment);
        }

        void UpdateLayout()
        {
            float shapeWidth = VirtualShape.Bounds.width;

            // always use the collapsed width for the text origin so the
            // text stays at the exact same position in both states.
            float textOriginX = (shapeWidth - CollapsedWidth) / 2f;
            float textOriginY = 0;

            float containerX = textOriginX;
            float containerY = textOriginY;

            // when expanded, offset by the padding + border so the
            // background/border grow outward and text doesn't shift.
            if (mIsExpanded)
            {
                containerX -= ExpandedPadding + ExpandedBorderWidth;
                containerY -= ExpandedPadding + ExpandedBorderWidth;
            }

            mCommentContainer.style.left = containerX;
            mCommentContainer.style.top = containerY;

            mHitTestOverlay.style.left = containerX;
            mHitTestOverlay.style.top = containerY;

            UpdateHitTestOverlaySize();
        }

        void UpdateHitTestOverlaySize()
        {
            if (!mIsExpanded)
            {
                mHitTestOverlay.style.width = CollapsedWidth;
                mHitTestOverlay.style.height = CollapsedHeight;
                return;
            }

            float containerWidth = mCommentContainer.resolvedStyle.width;
            float containerHeight = mCommentContainer.resolvedStyle.height;

            float hitWidth = !float.IsNaN(containerWidth) && containerWidth > 0
                ? containerWidth
                : CollapsedWidth;

            float hitHeight = !float.IsNaN(containerHeight) && containerHeight > 0
                ? containerHeight
                : CollapsedHeight;

            float offset = ExpandedPadding + ExpandedBorderWidth;
            float minExpandedHeight = CollapsedHeight + offset + HitTestMargin;
            float minExpandedWidth = CollapsedWidth + offset + HitTestMargin;

            if (hitHeight < minExpandedHeight)
                hitHeight = minExpandedHeight;
            if (hitWidth < minExpandedWidth)
                hitWidth = minExpandedWidth;

            mHitTestOverlay.style.width = hitWidth;
            mHitTestOverlay.style.height = hitHeight;
        }

        void UpdateVisibilityForZoom()
        {
            bool isVisible = IsVisibleForZoomLevel();

            if (!visible && mIsExpanded)
                Collapse();

            visible = isVisible;
        }

        void RequestResolveComment()
        {
            BrExChangeset changeset = ChangesetDraw.Tag as BrExChangeset;
            if (changeset == null)
                return;

            mCommentResolver.RequestComment(changeset.Id);
        }

        internal static ChangesetCommentShape FindForChangeset(
            VirtualCanvas canvas, ChangesetDrawInfo changesetDraw)
        {
            if (canvas == null)
                return null;

            foreach (BrExShape shape in canvas.GetShapes())
            {
                if (shape is ChangesetCommentShape commentShape &&
                    commentShape.ChangesetDraw == changesetDraw)
                    return commentShape;
            }

            return null;
        }

        bool IsVisibleForZoomLevel()
        {
            VirtualCanvas canvas = GetFirstAncestorOfType<VirtualCanvas>();
            return canvas != null && canvas.ZoomLevel >= MinZoomToShowComments;
        }

        IVisualElementScheduledItem mScheduledCollapse;
        bool mIsExpanded;
        string mOriginalComment = string.Empty;

        readonly AsyncChangesetCommentResolver mCommentResolver;
        readonly VisualElement mCommentContainer;
        readonly ScrollView mScrollView;
        readonly Label mCommentLabel;
        readonly VisualElement mHitTestOverlay;

        const float CollapsedWidth = BrExDrawProperties.ChangesetWidth;
        const float CollapsedHeight = 20;
        const float MaxExpandedWidth = 250;
        const float MaxExpandedHeight = 120;
        const float ExpandedPadding = 4;
        const float CornerRadius = 4;
        const float ExpandedBorderWidth = 1;
        const float HitTestMargin = 4;
        const int DefaultFontSize = 10;
        const long CollapseDelayMs = 100;
        const int ColorTransitionMs = 250;
        const float MinZoomToShowComments = 0.8f;
    }
}
