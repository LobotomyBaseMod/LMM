using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal interface IVirtualChild
    {
        Rect Bounds { get; }
        VisualElement Visual { get; }
        VisualElement CreateVisual();
        void DisposeVisual();
    }

    internal interface IVirtualCanvasUpdateVisualsListener
    {
        void OnVisualsUpdated();
    }

    internal interface IVirtualCanvasUpdateListener
    {
        void OnDelayedCanvasUpdate();
    }

    internal class VirtualCanvas : VisualElement
    {
        internal Vector2 ViewPortSize { get { return mViewPortSize; } }

        internal Vector2 ExtentSize { get { return mExtent; } }

        internal Vector2 ScrollOffset
        {
            get { return mScrollView.ScrollOffset; }
        }

        internal float ZoomLevel
        {
            get { return style.scale.value.value.x; }
        }

        internal BrExLayout Layout { get; set; }

        internal List<IVirtualChild> VirtualChildren { get { return mChildren; } }

        internal VirtualCanvas(
            CanvasScrollView scrollView,
            IVirtualCanvasUpdateVisualsListener loadedListener,
            IVirtualCanvasUpdateListener updatedListener)
        {
            mScrollView = scrollView;
            mLoadedListener = loadedListener;
            mUpdatedListener = updatedListener;

            mExtraSizeAroundEdges = new Vector2(10, 10);
            mChildren = new List<IVirtualChild>();

            style.transformOrigin = new TransformOrigin(0, 0, 0);

            mBackdrop = new VisualElement();

            Add(mBackdrop);

            mSelfThrottlingWorker = new SelfThrottlingWorker();

            mScrollView.Viewport.RegisterCallback<GeometryChangedEvent>(OnViewPortGeometryChanged);
        }

        internal void Dispose()
        {
            mScrollView.Viewport.UnregisterCallback<GeometryChangedEvent>(OnViewPortGeometryChanged);

            if (mChildren != null)
            {
                foreach (IVirtualChild child in mChildren)
                    child.DisposeVisual();

                mChildren.Clear();
            }

            mVisualPositions?.Clear();
            mVisibleRegions?.Clear();
            mDirtyRegions?.Clear();
            mIndex = null;
        }

        internal List<BrExShape> GetShapes()
        {
            List<BrExShape> result = new List<BrExShape>();

            foreach (VisualElement control in Children())
            {
                BrExShape shape = control as BrExShape;

                if (shape == null)
                    continue;

                result.Add(shape);
            }

            return result;
        }

        internal List<ChangesetShape> GetChangesetShapes()
        {
            List<ChangesetShape> result = new List<ChangesetShape>();

            foreach (var element in Children())
            {
                ChangesetShape commitShape = element as ChangesetShape;

                if (commitShape != null)
                    result.Add(commitShape);
            }

            return result;
        }

        internal void SetLayout(BrExLayout layout)
        {
            Layout = layout;

            if (layout == null)
                return;

            mExtent = new Vector2(
                Layout.Size.Width,
                Layout.Size.Height);
        }

        internal void ClearVirtualChildren()
        {
            mChildren.Clear();
        }

        internal void AddVirtualChild(IVirtualChild child)
        {
            mChildren.Add(child);
        }

        internal void Redraw()
        {
            foreach (IVirtualChild child in mChildren)
            {
                if (child.Visual == null)
                    continue;

                BrExShape brExShape = (BrExShape)child.Visual;
                brExShape.Redraw();
            }
        }

        internal void RedrawChangesetShapes()
        {
            foreach (IVirtualChild child in mChildren)
            {
                if (child.Visual == null)
                    continue;

                VirtualShape virtualShape = (VirtualShape)child;

                if (virtualShape.ShapeType != ShapeType.Changeset)
                    continue;

                BrExShape brExShape = (BrExShape)child.Visual;
                brExShape.Redraw();
            }
        }

        internal void RedrawChangesetCommentShapes()
        {
            foreach (IVirtualChild child in mChildren)
            {
                if (child.Visual == null)
                    continue;

                VirtualShape virtualShape = (VirtualShape)child;

                if (virtualShape.ShapeType != ShapeType.ChangesetComment)
                    continue;

                BrExShape brExShape = (BrExShape)child.Visual;
                brExShape.Redraw();
            }
        }

        internal void RebuildVisuals()
        {
            InvalidateVisuals();
            StartLazyUpdate();
        }

        internal void InvalidateVisuals()
        {
            mIndex = null;
            mVisualPositions = null;
            mVisible = new Rect();
            mVisualsUpdated = false;
            mDone = false;

            foreach (VisualElement e in Children())
            {
                IVirtualChild n = e.userData as IVirtualChild;
                if (n != null)
                {
                    e.userData = null;
                    n.DisposeVisual();
                }
            }

            Clear();
            Add(mBackdrop);
            InvalidateLayout();
        }

        internal void LazyUpdateVisuals()
        {
            // Do a quantized unit of work for creating newly visible visuals,
            //and cleaning up visuals that are no longer needed.

            if (mIndex == null)
            {
                this.CalculateNeededSizeToDisplayAllTheVirtualChildren();
            }

            mLastCanvasUpdateTicks = Environment.TickCount;
            mDone = true;
            mAdded = 0;
            mRemoved = 0;

            mSelfThrottlingWorker.CreateQuanta(
                new SelfThrottlingWorker.QuantizedWorkHandler(LazyCreateNodes));
            mSelfThrottlingWorker.RemoveQuanta(
                new SelfThrottlingWorker.QuantizedWorkHandler(LazyRemoveNodes));
            mSelfThrottlingWorker.GcQuanta(
                new SelfThrottlingWorker.QuantizedWorkHandler(LazyGarbageCollectNodes));

            if (mAdded > 0)
            {
                InvalidateLayout();
            }
            if (!mDone)
            {
                StartLazyUpdate();
            }

            MarkDirtyRepaint();
        }

        internal void UpdateVisualBounds(VisualElement visual, Rect bounds)
        {
            visual.style.left = bounds.xMin;
            visual.style.top = bounds.yMin;
        }

        internal void BeginUpdateScroll()
        {
            mIsUpdatingScroll = true;
        }

        internal void EndUpdateScroll()
        {
            mIsUpdatingScroll = false;
        }

        internal void OnZoomChanged()
        {
            mScrollView.ContentSize = new Vector2(mExtent.x * ZoomLevel, mExtent.y * ZoomLevel);

            OnScrollChanged();
        }

        // The visible region has changed, so we need to queue up work for dirty regions and new visible regions
        // then start asynchronously building new visuals via StartLazyUpdate.
        internal void OnScrollChanged()
        {
            if (mIsUpdatingScroll)
                return;

            Rect dirty = mVisible;
            AddTheCurrentVisibleRectToTheListOfRegionsToProcess();
            mNodeCollectCycle = 0;
            mDone = false;

            RegionsUpdater.UpdateDirtyRegions(mDirtyRegions, dirty, mVisible);

            StartLazyUpdate();

            foreach (var child in mChildren)
                ((BrExShape)child.Visual)?.OnScrollChanged();
        }

        internal Rect GetVisibleRectStrict()
        {
            // The rect visible by the user, without any kind of security margins
            float xstart = mScrollView.ScrollOffset.x / ZoomLevel;
            float ystart = mScrollView.ScrollOffset.y / ZoomLevel;

            float xend = (mScrollView.ScrollOffset.x + mViewPortSize.x) / ZoomLevel;
            float yend = (mScrollView.ScrollOffset.y + mViewPortSize.y) / ZoomLevel;

            return new Rect(xstart, ystart, xend - xstart, yend - ystart);
        }

        internal void RestoreZOrder(VisualElement element)
        {
            IVirtualChild child = element.userData as IVirtualChild;

            if (child == null)
                return;

            int position = GetVisualPosition(child);
            int insertIndex = FindInsertionIndex(position);

            // move the element to the correct position
            hierarchy.Insert(insertIndex, element);
        }

        internal Rect GetVisibleFrame(int offset)
        {
            Rect visibleRect = GetVisibleRect();
            return visibleRect.Inflate(-offset / ZoomLevel, -offset / ZoomLevel);
        }

        void InvalidateLayout()
        {
            schedule.Execute(PerformLayout);
        }

        void OnViewPortGeometryChanged(GeometryChangedEvent evt)
        {
            // Equivalent to checking if availableSize changed in MeasureOverride
            if (evt.oldRect.size == evt.newRect.size)
                return;

            Vector2 newSize = evt.newRect.size;

            if (newSize == mViewPortSize)
                return;

            SetViewportSize(newSize);
            PerformLayout();
        }

        void PerformLayout()
        {
            CalculateNeededSizeToDisplayAllTheVirtualChildren();

            if (mIndex == null)
            {
                StartLazyUpdate();
            }

            NotifyVisualsUpdated();
        }

        /// <summary>
        /// Calculate the size needed to display all the virtual children.
        /// </summary>
        void CalculateNeededSizeToDisplayAllTheVirtualChildren()
        {
            bool rebuild = false;
            if (mIndex == null || mExtent.x == 0 || mExtent.y == 0 ||
                float.IsNaN(mExtent.x) || float.IsNaN(mExtent.y))
            {
                rebuild = true;

                int width = Layout == null ? 0 : Layout.Size.Width;
                int height = Layout == null ? 0 : Layout.Size.Height;

                mExtent = new Vector2(width, height);
                // Ok, now we know the size we can create the index.
                mIndex = new QuadTree(new Rect(0, 0, mExtent.x, mExtent.y));
                mVisualPositions = new Dictionary<IVirtualChild, int>();
                int index = 0;
                foreach (IVirtualChild n in mChildren)
                {
                    // Position is calculated as: (zIndex * BASE_MULTIPLIER) + originalIndex
                    // This ensures proper z-ordering while maintaining relative order within each z-index level.
                    // Shapes with lower z-index are placed earlier in the hierarchy (drawn at the back),
                    // shapes with higher z-index are placed later (drawn at the front).
                    int zIndex = ZIndex.GetZIndex((VirtualShape)n);
                    mVisualPositions[n] = (zIndex * ZIndex.BASE_MULTIPLIER) + index++;
                    if (n.Bounds.width > 0 && n.Bounds.height > 0)
                    {
                        mIndex.InsertNode(n);
                    }
                }
            }

            float w = mExtent.x;
            float h = mExtent.y;

            style.width = w;
            style.height = h;

            mScrollView.ContentSize = new Vector2(w * ZoomLevel, h * ZoomLevel);

            if (!float.IsInfinity(mViewPortSize.x) &&
                !float.IsInfinity(mViewPortSize.y))
            {
                w = Math.Max(w, mViewPortSize.x / ZoomLevel);
                h = Math.Max(h, mViewPortSize.y / ZoomLevel);
                mBackdrop.style.width = w;
                mBackdrop.style.height = h;
            }

            if (rebuild)
            {
                AddTheCurrentVisibleRectToTheListOfRegionsToProcess();
            }
        }

        void NotifyVisualsUpdated()
        {
            if (mVisualsUpdated)
                return;

            if (Layout == null)
                return;

            mVisualsUpdated = true;
            mLoadedListener.OnVisualsUpdated();
        }

        Rect GetVisibleRect()
        {
            // Add a bit of extra around the edges so we are sure to create
            // nodes that have a tiny bit showing.
            float xstart = (mScrollView.ScrollOffset.x - mExtraSizeAroundEdges.x) / ZoomLevel;

            float ystart = (mScrollView.ScrollOffset.y - mExtraSizeAroundEdges.y) / ZoomLevel;

            float xend = (mScrollView.ScrollOffset.x + (mViewPortSize.x +
                                                        (2 * mExtraSizeAroundEdges.x))) / ZoomLevel;

            float yend = (mScrollView.ScrollOffset.y + (mViewPortSize.y +
                                                        (2 * mExtraSizeAroundEdges.y))) / ZoomLevel;

            return new Rect(xstart, ystart, xend - xstart, yend - ystart);
        }

        void SetViewportSize(Vector2 s)
        {
            if (s == mViewPortSize)
                return;

            mViewPortSize = s;
            OnScrollChanged();
        }

        void AddTheCurrentVisibleRectToTheListOfRegionsToProcess()
        {
            mVisible = GetVisibleRect();
            mVisibleRegions.Clear();
            mVisibleRegions.Add(mVisible);
        }

        void StartLazyUpdate()
        {
            if (mTimer == null)
            {
                mTimer = schedule.Execute(OnStartLazyUpdate).Every(5);
            }

            if (mDelayedUpdateTimer == null)
            {
                mDelayedUpdateTimer = schedule.Execute(OnDelayedCanvasUpdate).Every(50);
            }

            mTimer.Resume();
            mDelayedUpdateTimer.Resume();
        }

        void OnStartLazyUpdate()
        {
            mTimer.Pause();
            this.LazyUpdateVisuals();
        }

        void OnDelayedCanvasUpdate()
        {
            long ellapsedTimeSinceLastUpdate =
                Environment.TickCount - mLastCanvasUpdateTicks;

            // It hasn't passed enough time since the last canvas update
            // to warn the DelayedCanvasUpdateListener about it.
            if (ellapsedTimeSinceLastUpdate < UPDATE_LISTENER_DELAY)
                return;

            mDelayedUpdateTimer.Pause();
            mUpdatedListener.OnDelayedCanvasUpdate();
        }

        int LazyCreateNodes(int quantum)
        {
            if (mVisible.width <= 0 || mVisible.height <= 0)
            {
                mVisible = GetVisibleRect();
                mVisibleRegions.Add(mVisible);
                mDone = false;
            }

            int count = 0;
            int regionCount = 0;
            while (mVisibleRegions.Count > 0 && count < quantum)
            {
                Rect visibleRegion = mVisibleRegions[0];
                mVisibleRegions.RemoveAt(0);
                regionCount++;

                // Iterate over the visible range of nodes and make sure they have visuals.
                foreach (IVirtualChild n in mIndex.GetNodesInside(visibleRegion))
                {
                    if (n.Visual == null ||
                        n.Visual.userData == null)
                    {
                        EnsureVisual(n);
                        mAdded++;
                    }

                    count++;

                    if (count >= quantum)
                    {
                        RegionsUpdater.UpdateRegions(
                            mVisibleRegions, regionCount, visibleRegion, mExtraSizeAroundEdges);

                        mDone = false;
                        break;
                    }
                }
            }

            return count;
        }

        // Remove visuals for nodes that are no longer visible.
        int LazyRemoveNodes(int quantum)
        {
            Rect visible = GetVisibleRect();
            int count = 0;

            // Also remove nodes that are no longer visible.
            int regionCount = 0;
            while (mDirtyRegions.Count > 0 && count < quantum)
            {
                int last = mDirtyRegions.Count - 1;
                Rect dirtyRegion = mDirtyRegions[last];
                mDirtyRegions.RemoveAt(last);
                regionCount++;

                // Iterate over the visible range of nodes and make sure they have visuals.
                foreach (IVirtualChild n in mIndex.GetNodesInside(dirtyRegion))
                {
                    VisualElement e = n.Visual;
                    if (e != null)
                    {
                        Rect nrect = n.Bounds;
                        if (!nrect.Intersects(visible))
                        {
                            e.userData = null;
                            Remove(e);
                            n.DisposeVisual();
                            mRemoved++;
                        }
                    }

                    count++;
                    if (count >= quantum)
                    {
                        RegionsUpdater.UpdateRegions(
                            mDirtyRegions, regionCount, dirtyRegion, mExtraSizeAroundEdges);

                        mDone = false;
                        break;
                    }
                }
            }
            return count;
        }

        // Check all child nodes to see if any leaked from LazyRemoveNodes and remove their visuals.
        int LazyGarbageCollectNodes(int quantum)
        {
            int count = 0;
            // Now after every update also do a full incremental scan over all the children
            // to make sure we didn't leak any nodes that need to be removed.
            while (count < quantum && mNodeCollectCycle < childCount)
            {
                VisualElement e = hierarchy[mNodeCollectCycle];
                IVirtualChild n = e.userData as IVirtualChild;
                if (n != null)
                {
                    Rect nrect = n.Bounds;
                    if (!nrect.Intersects(mVisible))
                    {
                        e.userData = null;
                        Remove(e);
                        n.DisposeVisual();
                        mRemoved++;
                        count++;
                        continue;
                    }
                }
                mNodeCollectCycle++;
            }

            if (mNodeCollectCycle < childCount)
            {
                mDone = false;
            }

            return count;
        }

        // Insert the visual for the child in the correct z-order position.
        //
        // GetNodesInside returns nodes in spatial order (from QuadTree), not z-order,
        // so we use binary search with mVisualPositions to find the correct insertion point.
        // This gives O(log M) insertion where M is the number of visible children.
        void EnsureVisual(IVirtualChild child)
        {
            VisualElement e = child.CreateVisual();
            e.style.position = Position.Absolute;
            e.userData = child;

            if (!(e is ParentLinkShape) && !(e is MergeLinkShape))
                UpdateVisualBounds(e, child.Bounds);

            int position = GetVisualPosition(child);
            int insertIndex = FindInsertionIndex(position);
            hierarchy.Insert(insertIndex, e);
        }

        int GetVisualPosition(IVirtualChild child)
        {
            if (mVisualPositions != null && mVisualPositions.TryGetValue(child, out int position))
                return position;

            // Fallback: calculate position from z-index if not in dictionary
            // Use a high index within the z-level to place at end of that level
            VirtualShape virtualShape = child as VirtualShape;
            if (virtualShape != null)
            {
                int zIndex = ZIndex.GetZIndex(virtualShape);
                return (zIndex * ZIndex.BASE_MULTIPLIER) + (ZIndex.BASE_MULTIPLIER - 1);
            }

            // Last resort: place at the end
            return int.MaxValue;
        }

        // Binary search to find the insertion index for an element with the given position.
        // Returns the first index where all preceding elements have smaller positions.
        // Non-virtual elements (like backdrop) are treated as having position -infinity.
        int FindInsertionIndex(int targetPosition)
        {
            int low = 0;
            int high = hierarchy.childCount;

            while (low < high)
            {
                int mid = (low + high) / 2;
                VisualElement v = hierarchy[mid];
                IVirtualChild n = v.userData as IVirtualChild;

                if (n == null)
                {
                    // Non-virtual elements (backdrop) are at the beginning
                    low = mid + 1;
                }
                else
                {
                    int midPosition = GetVisualPosition(n);
                    if (midPosition < targetPosition)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid;
                    }
                }
            }

            return low;
        }

        static class RegionsUpdater
        {
            internal static void UpdateRegions(
                IList<Rect> regions, int regionCount, Rect region, Vector2 extraSizeAroundEdges)
            {
                // This region is too big, so subdivide it into smaller slices.
                if (regionCount == 1)
                {
                    // We didn't even complete 1 region, so we better split it.
                    SplitRegion(region, regions, extraSizeAroundEdges);
                    return;

                }

                regions.Add(region); // put it back since we're not done!
            }

            internal static void UpdateDirtyRegions(
                IList<Rect> dirtyRegions, Rect dirtyRegion, Rect visibleRegion)
            {
                Rect intersection = dirtyRegion.Intersect(visibleRegion);
                if (intersection.width <= 0 || intersection.height <= 0)
                {
                    dirtyRegions.Add(dirtyRegion); // the whole thing is dirty
                    return;
                }

                // Add left stripe
                if (dirtyRegion.xMin < intersection.xMin)
                {
                    dirtyRegions.Add(new Rect(
                        dirtyRegion.xMin, dirtyRegion.yMin,
                        intersection.xMin - dirtyRegion.xMin, dirtyRegion.height));
                }
                // Add right stripe
                if (dirtyRegion.xMax > intersection.xMax)
                {
                    dirtyRegions.Add(new Rect(
                        intersection.xMax, dirtyRegion.yMin,
                        dirtyRegion.xMax - intersection.xMax, dirtyRegion.height));
                }
                // Add top stripe
                if (dirtyRegion.yMin < intersection.yMin)
                {
                    dirtyRegions.Add(new Rect(
                        dirtyRegion.xMin, dirtyRegion.yMin,
                        dirtyRegion.width, intersection.yMin - dirtyRegion.yMin));
                }
                // Add right stripe
                if (dirtyRegion.yMax > intersection.yMax)
                {
                    dirtyRegions.Add(new Rect(
                        dirtyRegion.xMin, intersection.yMax,
                        dirtyRegion.width, dirtyRegion.yMax - intersection.yMax));
                }
            }

            static void SplitRegion(
                Rect region, IList<Rect> regions, Vector2 extraSizeAroundEdges)
            {
                float minWidth = extraSizeAroundEdges.x * 2;
                float minHeight = extraSizeAroundEdges.y * 2;

                if (region.width > region.height && region.height > minHeight)
                {
                    // horizontal slices
                    float h = region.height / 2;
                    regions.Add(new Rect(region.xMin, region.yMin, region.width, h + 10));
                    regions.Add(new Rect(region.xMin, region.yMin + h, region.width, h + 10));
                    return;
                }

                if (region.width < region.height && region.width > minWidth)
                {
                    // vertical slices
                    float w = region.width / 2;
                    regions.Add(new Rect(region.xMin, region.yMin, w + 10, region.height));
                    regions.Add(new Rect(region.xMin + w, region.yMin, w + 10, region.height));
                    return;
                }

                regions.Add(region); // put it back since we're not done!
            }
        }

        bool mIsUpdatingScroll = false;
        bool mVisualsUpdated = false;
        bool mDone = true;
        int mAdded;
        int mRemoved;
        int mNodeCollectCycle;
        long mLastCanvasUpdateTicks;

        IVisualElementScheduledItem mTimer;
        IVisualElementScheduledItem mDelayedUpdateTimer;
        SelfThrottlingWorker mSelfThrottlingWorker;

        VisualElement mBackdrop;
        Vector2 mViewPortSize;
        Vector2 mExtent;

        Rect mVisible;
        IList<Rect> mVisibleRegions = new List<Rect>();
        IList<Rect> mDirtyRegions = new List<Rect>();

        QuadTree mIndex;
        List<IVirtualChild> mChildren;
        IDictionary<IVirtualChild, int> mVisualPositions;

        readonly CanvasScrollView mScrollView;
        readonly Vector2 mExtraSizeAroundEdges;
        readonly IVirtualCanvasUpdateVisualsListener mLoadedListener;
        readonly IVirtualCanvasUpdateListener mUpdatedListener;

        static readonly int UPDATE_LISTENER_DELAY = 200;
    }
}
