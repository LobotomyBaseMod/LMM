using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.Utils;
using Unity.PlasticSCM.Editor.UI.UIElements;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class BrExShape : VisualElement
    {
        internal interface IBrExShapeClickListener
        {
            void OnShapeClicked(VirtualShape shape, bool isMultiSelection);
            void OnShapeDoubleClicked();
            void OnContextMenuRequested();
        }

        internal VirtualShape VirtualShape { get { return mVirtualShape; } }

        internal BrExShape(VirtualShape virtualShape)
        {
            SetVirtualShape(virtualShape);

            generateVisualContent += OnGenerateVisualContent;

            RegisterCallback<PointerDownEvent>(OnMouseDown);
            RegisterCallback<ContextClickEvent>(OnContextClick);

            if (!HasTooltip(this))
                return;

            //mBrExShapeTooltipPopup = new BrExShapeTooltipPopup(this);
            //LogicalChildren.Add(mBrExShapeTooltipPopup);
        }

        internal bool IsLinkNavigationTarget
        {
            get { return mIsLinkNavigationTarget; }
            set
            {
                if (mIsLinkNavigationTarget == value)
                    return;

                mIsLinkNavigationTarget = value;

                MarkDirtyRepaint();
            }
        }

        internal bool IsSearchResult
        {
            get { return mIsSearchResult; }
            set
            {
                if (mIsSearchResult == value)
                    return;

                mIsSearchResult = value;
                MarkDirtyRepaint();
            }
        }

        internal bool IsCurrentSearchResult
        {
            get { return mIsCurrentSearchResult; }
            set
            {
                if (mIsCurrentSearchResult == value)
                    return;

                mIsCurrentSearchResult = value;
                MarkDirtyRepaint();
            }
        }

        internal bool IsSelected
        {
            get { return mIsSelected; }
            set
            {
                if (mIsSelected == value)
                    return;

                mIsSelected = value;
                MarkDirtyRepaint();
            }
        }

        internal DisplayOptions DisplayOptions
        {
            get { return mVirtualShape.Config.DisplayOptions; }
        }

        internal virtual void Dispose()
        {
            //mBrExShapeTooltipPopup?.Dispose();
            generateVisualContent -= OnGenerateVisualContent;

            UnregisterCallback<PointerDownEvent>(OnMouseDown);
            UnregisterCallback<ContextClickEvent>(OnContextClick);
        }

        internal virtual void Redraw()
        {
            MarkDirtyRepaint();
        }

        internal virtual void OnScrollChanged() { }

        internal virtual bool HasContextMenu() => false;

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (!MustNotifyClick(this))
                return false;

            return base.ContainsPoint(localPoint);
        }

        // internal for testing purposes
        internal void OnMouseDown(PointerDownEvent e)
        {
            if (!MustNotifyClick(this))
                return;

            if (IsDoubleClick(e))
            {
                OnMouseDoubleClick(e);
                return;
            }

            OnMouseClick(
                e.button,
                e.commandKey,
                e.ctrlKey);
        }

        void OnMouseDoubleClick(PointerDownEvent e)
        {
            if (MouseEvents.IsRightButtonPressed(e))
                return;

            if (!MustNotifyDoubleClick(this))
                return;

            IBrExShapeClickListener clickListener =
                GetFirstAncestorOfType<IBrExShapeClickListener>();

            if (clickListener == null)
                return;

            clickListener.OnShapeDoubleClicked();
        }

        void OnMouseClick(
            int clickedMouseButton,
            bool isCommandKeyPressed,
            bool isCtrlKeyPressed)
        {
            if (!IsSelectionEvent(clickedMouseButton, IsSelected))
                return;

            IBrExShapeClickListener clickListener =
                GetFirstAncestorOfType<IBrExShapeClickListener>();

            if (clickListener == null)
                return;

            clickListener.OnShapeClicked(
                GetVirtualShapeForSelection(),
                IsMultipleSelection(isCommandKeyPressed, isCtrlKeyPressed));
        }

        // internal for testing purposes
        internal void OnContextClick(ContextClickEvent e)
        {
            OnMouseClick(
                e.button,
                e.commandKey,
                e.ctrlKey);

            IBrExShapeClickListener clickListener =
                GetFirstAncestorOfType<IBrExShapeClickListener>();

            if (clickListener == null)
                return;

            if (!HasContextMenu())
                return;

            clickListener.OnContextMenuRequested();

            e.StopPropagation();
        }

        static bool MustNotifyClick(BrExShape shape)
        {
            return shape is BranchShape ||
                   shape is BranchCaptionShape ||
                   shape is ChangesetShape ||
                   shape is LabelShape ||
                   shape is MergeLinkShape;
        }

        static bool MustNotifyDoubleClick(BrExShape shape)
        {
            return shape is BranchShape ||
                   shape is BranchCaptionShape ||
                   shape is ChangesetShape ||
                   shape is LabelShape;
        }

        static bool HasTooltip(BrExShape shape)
        {
            return shape is BranchShape ||
                   shape is BranchCaptionShape ||
                   shape is ChangesetShape ||
                   shape is LabelShape;
        }

        static bool IsDoubleClick(PointerDownEvent e)
        {
            return e.clickCount == 2;
        }

        static bool IsSelectionEvent(int mouseButton, bool isSelected)
        {
            if (MouseEvents.IsRightButtonPressed(mouseButton) && isSelected)
                return false;

            return true;
        }

        static bool IsMultipleSelection(
            bool isCommandKeyPressed,
            bool isCtrlKeyPressed)
        {
            if (PlatformIdentifier.IsMac())
                return isCommandKeyPressed;

            return isCtrlKeyPressed;
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
#if UNITY_2022_1_OR_NEWER
            GenerateVisualContent(ctx.painter2D);

            if (!mIsDrawDebugBoundsEnabled)
                return;

            DrawDebugBounds(ctx.painter2D, this);
#endif
        }

        protected virtual void GenerateVisualContent(Painter2D painter) { }

        protected virtual void SetVirtualShape(VirtualShape virtualShape)
        {
            mVirtualShape = virtualShape;

            IsSelected = mVirtualShape.IsSelected;
            IsSearchResult = mVirtualShape.IsSearchResult;
            IsCurrentSearchResult = mVirtualShape.IsCurrentSearchResult;
            IsLinkNavigationTarget = mVirtualShape.IsLinkNavigationTarget;

            style.width = mVirtualShape.Bounds.width;
            style.height = mVirtualShape.Bounds.height;
        }

        protected internal virtual VirtualShape GetVirtualShapeForSelection()
        {
            return mVirtualShape;
        }

        protected void DrawDebugHitTestingBounds(Painter2D painter, List<Vector2> polygon)
        {
            painter.lineWidth = 1;
            painter.strokeColor = new Color(1f, 0f, 1f, 1f);

            painter.DrawPoligon(polygon);

            painter.Stroke();
        }

        static void DrawDebugBounds(Painter2D painter, BrExShape shape)
        {
            painter.lineWidth = 1;

            if (shape is ColumnHeaderShape)
            {
                // brown
                painter.strokeColor = new Color(0.6f, 0.4f, 0.2f, 1f);
                painter.DrawRect(new Rect(0, 0, shape.VirtualShape.Bounds.width, shape.VirtualShape.Bounds.height));
                painter.Stroke();
            }
            else if (shape is BranchShape)
            {
                // red
                painter.strokeColor = new Color(1f, 0f, 0f, 1f);
                painter.DrawRect(new Rect(0, 0, shape.VirtualShape.Bounds.width, shape.VirtualShape.Bounds.height));
                painter.Stroke();
            }
            else if (shape is BranchCaptionShape)
            {
                // aquamarine
                painter.strokeColor = new Color(0.5f, 1f, 0.83f, 1f);
                painter.DrawRect(new Rect(0, 0, shape.VirtualShape.Bounds.width, shape.VirtualShape.Bounds.height));
                painter.Stroke();
            }
            else if (shape is ChangesetShape)
            {
                // blue
                painter.strokeColor = new Color(0f, 0f, 1f, 1f);
                painter.DrawRect(new Rect(0, 0, shape.VirtualShape.Bounds.width, shape.VirtualShape.Bounds.height));
                painter.Stroke();
            }
            else if (shape is LabelShape)
            {
                // green
                painter.strokeColor = new Color(0f, 1f, 0f, 1f);
                painter.DrawRect(new Rect(0, 0, shape.VirtualShape.Bounds.width, shape.VirtualShape.Bounds.height));
                painter.Stroke();
            }
            else if (shape is ParentLinkShape)
            {
                // organge
                painter.strokeColor = new Color(1f, 0.5f, 0f, 1f);
                painter.DrawRect(new Rect(0, 0, shape.VirtualShape.Bounds.width, shape.VirtualShape.Bounds.height));
                painter.Stroke();
            }
            else if (shape is MergeLinkShape)
            {
                // magenta
                painter.strokeColor = new Color(1f, 0f, 1f, 1f);
                painter.DrawRect(new Rect(0, 0, shape.VirtualShape.Bounds.width, shape.VirtualShape.Bounds.height));
            }

            painter.Stroke();
        }

#if UNITY_2022_1_OR_NEWER
        // enable to draw the shape geometry bounds
        static readonly bool mIsDrawDebugBoundsEnabled = false;
#endif

        bool mIsLinkNavigationTarget;
        bool mIsSearchResult;
        bool mIsCurrentSearchResult;
        bool mIsSelected;

        protected VirtualShape mVirtualShape;
    }
}
