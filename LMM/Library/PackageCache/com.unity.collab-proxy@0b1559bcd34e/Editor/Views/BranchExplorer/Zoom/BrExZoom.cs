using System;
using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Zoom
{
    internal class BrExZoom
    {
        internal float ZoomLevel { get { return mZoomLevel; } }

        internal Vector2 Offset
        {
            get { return mScrollView.ScrollOffset; }
            set
            {
                StopAnimations();
                Translate(value.x, value.y);
            }
        }

        internal BrExZoom(
            WorkspaceInfo wkInfo,
            CanvasScrollView scrollView,
            VirtualCanvas canvas)
        {
            mWkInfo = wkInfo;
            mScrollView = scrollView;
            mCanvas = canvas;

            mScrollView.Viewport.RegisterCallback<PointerMoveEvent>(OnPointerMoved);
            mScrollView.Viewport.RegisterCallback<WheelEvent>(OnPointerWheelChanged);
        }

        internal void SetWorkspaceUIConfiguration(
            WorkspaceUIConfiguration workspaceUIConfiguration)
        {
            mConfig = workspaceUIConfiguration;
        }

        internal void Dispose()
        {
            mScrollView.Viewport.UnregisterCallback<PointerMoveEvent>(OnPointerMoved);
            mScrollView.Viewport.UnregisterCallback<WheelEvent>(OnPointerWheelChanged);
        }

        internal void InitializeZoomLevel()
        {
            mZoomLevel = mConfig.ZoomLevel;
            mNewZoomLevel = mZoomLevel;
            ApplyZoom();
        }

        internal void ZoomIn()
        {
            SetLastPoint(new Vector2(
                mScrollView.Viewport.resolvedStyle.width / 2,
                mScrollView.Viewport.resolvedStyle.height / 2));

            HandleZoom(1);
        }

        internal void ZoomOut()
        {
            SetLastPoint(new Vector2(
                mScrollView.Viewport.resolvedStyle.width / 2,
                mScrollView.Viewport.resolvedStyle.height / 2));

            HandleZoom(-1);
        }

        void OnPointerMoved(PointerMoveEvent evt)
        {
            Vector2 viewportPos = mScrollView.Viewport.WorldToLocal(evt.originalMousePosition);
            SetLastPoint(viewportPos);
        }

        void OnPointerWheelChanged(WheelEvent evt)
        {
            if (!evt.ctrlKey)
                return;

            Vector2 viewportPos = mScrollView.Viewport.WorldToLocal(evt.originalMousePosition);
            SetLastPoint(viewportPos);

            HandleZoom(-evt.delta.y);

            evt.StopPropagation();
        }

        internal void ScrollIntoView(Rect rect, bool animate = true)
        {
            StopAnimations();

            Rect visibleFrame = mCanvas.GetVisibleFrame(50);

            if (rect.Overlaps(visibleFrame)) // already visible
                return;

            // Scroll to center the rectangle on the visible area
            Vector2 targetPoint = new Vector2(
                (rect.x + rect.width / 2) * ZoomLevel,
                (rect.y + rect.height / 2) * ZoomLevel);

            float offsetToCenterX = mScrollView.Viewport.resolvedStyle.width / 2;
            float offsetToCenterY = mScrollView.Viewport.resolvedStyle.height / 2;

            Vector2 targetOffset = new Vector2(
                targetPoint.x - offsetToCenterX,
                targetPoint.y - offsetToCenterY);

            if (animate)
            {
                float dx = targetOffset.x - Offset.x;
                float dy = targetOffset.y - Offset.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance > MAX_ANIMATABLE_DISTANCE)
                {
                    // Jump most of the distance instantly
                    float ratio = (distance - MAX_ANIMATABLE_DISTANCE) / distance;
                    Vector2 preAnimOffset = new Vector2(
                        Offset.x + dx * ratio,
                        Offset.y + dy * ratio);

                    // Immediately set to near-target
                    Translate(preAnimOffset.x, preAnimOffset.y);

                    // Now animate the last portion
                    AnimateScroll(preAnimOffset, targetOffset,
                        TimeSpan.FromMilliseconds(MAX_SCROLL_TIME));
                    return;
                }

                // Small enough distance — animate normally
                AnimateScroll(Offset, targetOffset,
                    TimeSpan.FromMilliseconds(MAX_SCROLL_TIME));
                return;
            }

            Translate(targetOffset.x, targetOffset.y);
        }

        internal void AnimateScroll(Vector2 from, Vector2 to, TimeSpan duration)
        {
            StopAnimations(false);

            mActiveScrollAnimation = UIElementsAnimator.Animate(
                mScrollView,
                from,
                to,
                duration,
                Easing.EaseOutCubic,
                value => Translate(value.x, value.y));
        }

        internal void StopAnimations(bool resetValues = true)
        {
            StopZoomAnimation(resetValues);
            StopScrollAnimation();
        }

        void OnZoomAnimationChanged(float newZoomLevel)
        {
            if (Mathf.Approximately(mZoomLevel, newZoomLevel))
                return;

            mZoomLevel = newZoomLevel;
            mConfig.ZoomLevel = mZoomLevel;
            mConfig.Save(mWkInfo);

            ApplyZoom();
            KeepPositionStable();
        }

        void ApplyZoom()
        {
#if UNITY_2022_1_OR_NEWER
            mCanvas.style.scale = new StyleScale(new Vector2(mZoomLevel, mZoomLevel));
#endif
            mCanvas.OnZoomChanged();
        }

        void SetLastPoint(Vector2 viewportPoint)
        {
            mLastMousePoint = mScrollView.Viewport.LocalToWorld(viewportPoint);

            Vector2 canvasPoint = mCanvas.WorldToLocal(mLastMousePoint);

            Vector2 extentSize = mCanvas.ExtentSize;

            mLastEdgePoint = EdgePoint.GetEdgePoint(
                canvasPoint,
                extentSize.x,
                extentSize.y,
                mZoomLevel);

            mHasLastEdgePoint = true;
        }

        void HandleZoom(float clicks)
        {
            float amount = clicks;

            if (amount > 1) amount = 1;
            if (amount < -1) amount = -1;

            bool isSameZoomDirection = (Mathf.Sign(amount) == Mathf.Sign(mLastIncrement));

            if (!isSameZoomDirection || mStartTime == 0)
                StopAnimations();

            // Accumulate the desired zoom amount as this method is called while the
            // user is repeatedly spinning the mouse.
            float sensitivity = SENSITIVITY;
            float extra = Mathf.Abs(clicks);
            if (extra > 1)
                sensitivity = Mathf.Min(0.5f, sensitivity * extra);

            float delta = 1 - (Mathf.Abs(amount) * sensitivity);
            if (amount < 0) // negative = zoom out
                mNewZoomLevel *= delta;
            else // positive = zoom in
                mNewZoomLevel /= delta;

            if (mNewZoomLevel > MAX_ZOOM_LEVEL)
                mNewZoomLevel = MAX_ZOOM_LEVEL;
            if (mNewZoomLevel < MIN_ZOOM_LEVEL)
                mNewZoomLevel = MIN_ZOOM_LEVEL;

            // Calculate how long we want to keep zooming and increase this time
            // if the user keeps spinning the mouse so we get a nice momentum effect.
            long tick = Environment.TickCount;

            if (isSameZoomDirection && mStartTime != 0 && mStartTime + mZoomTime > tick)
            {
                // Then make the time cumulative so you get nice smooth animation
                mZoomTime += (mStartTime + mZoomTime - tick);
                if (mZoomTime > MAX_ZOOM_TIME) mZoomTime = MAX_ZOOM_TIME;
            }
            else
            {
                mStartTime = tick;
                mZoomTime = DEFAULT_ZOOM_TIME;
            }

            mLastIncrement = amount;

            AnimateZoom(mZoomLevel, mNewZoomLevel,
                TimeSpan.FromMilliseconds(mZoomTime));
        }

        void AnimateZoom(float from, float to, TimeSpan duration)
        {
            StopZoomAnimation(resetValues: false);

            mActiveZoomAnimation = UIElementsAnimator.Animate(
                mCanvas,
                from,
                to,
                duration,
                Easing.EaseOutCubic,
                OnZoomAnimationChanged);
        }

        void StopZoomAnimation(bool resetValues)
        {
            mActiveZoomAnimation?.Pause();
            mActiveZoomAnimation = null;

            if (!resetValues)
                return;

            mNewZoomLevel = mZoomLevel;
            mStartTime = 0;
        }

        void StopScrollAnimation()
        {
            mActiveScrollAnimation?.Pause();
            mActiveScrollAnimation = null;
        }

        void Translate(float x, float y)
        {
            Vector2 containerSize = mCanvas.ViewPortSize;
            Vector2 extentSize = mCanvas.ExtentSize;

            // Use extentSize consistently (not resolvedStyle which may differ due to layout timing)
            if (extentSize.x * mZoomLevel <= containerSize.x &&
                extentSize.y * mZoomLevel <= containerSize.y)
            {
                x = y = 0;
            }

            float maxTranslateX = (extentSize.x * mZoomLevel) - containerSize.x;
            float maxTranslateY = (extentSize.y * mZoomLevel) - containerSize.y;

            if (x > maxTranslateX) x = maxTranslateX;
            if (y > maxTranslateY) y = maxTranslateY;

            if (x < 0) x = 0;
            if (y < 0) y = 0;

            mScrollView.ScrollOffset = new Vector2(x, y);
        }

        void KeepPositionStable()
        {
            if (!mHasLastEdgePoint)
                return;

            Vector2 moved = mCanvas.WorldToLocal(mLastMousePoint);
            Vector2 delta = new Vector2(
                moved.x - mLastEdgePoint.x,
                moved.y - mLastEdgePoint.y);

            float x = mScrollView.ScrollOffset.x - (delta.x * mZoomLevel);
            float y = mScrollView.ScrollOffset.y - (delta.y * mZoomLevel);

            Vector2 containerSize = mCanvas.ViewPortSize;
            Vector2 extentSize = mCanvas.ExtentSize;

            // Make sure content doesn't scroll past right/bottom edges
            float right = (extentSize.x * mZoomLevel) - x;
            if (right < containerSize.x && x > 0)
                x -= (containerSize.x - right);

            float bottom = (extentSize.y * mZoomLevel) - y;
            if (bottom < containerSize.y && y > 0)
                y -= (containerSize.y - bottom);

            Translate(x, y);
        }

        IVisualElementScheduledItem mActiveZoomAnimation;
        IVisualElementScheduledItem mActiveScrollAnimation;

        long mStartTime;
        long mZoomTime = DEFAULT_ZOOM_TIME;

        Vector2 mLastMousePoint;
        Vector2 mLastEdgePoint;
        bool mHasLastEdgePoint;

        float mLastIncrement;
        float mZoomLevel = 1;
        float mNewZoomLevel = 1;

        WorkspaceUIConfiguration mConfig;

        readonly VirtualCanvas mCanvas;
        readonly WorkspaceInfo mWkInfo;
        readonly CanvasScrollView mScrollView;

        const float SENSITIVITY = 0.10f;
        const long DEFAULT_ZOOM_TIME = 200;
        const long MAX_ZOOM_TIME = 300;
        const float MAX_SCROLL_TIME = 300;
        const float MAX_ZOOM_LEVEL = 10;
        const float MIN_ZOOM_LEVEL = 0.2f;
        const float MAX_ANIMATABLE_DISTANCE = 1500; // px
    }
}
