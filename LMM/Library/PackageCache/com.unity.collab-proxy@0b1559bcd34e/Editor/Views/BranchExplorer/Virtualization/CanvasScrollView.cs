using System;
using Unity.PlasticSCM.Editor.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal class CanvasScrollView : VisualElement
    {
        internal event Action<Vector2> OnScrollChanged;

        internal VisualElement Viewport { get; }
        internal VisualElement ContentContainer { get; }

        internal Vector2 ScrollOffset
        {
            get => mScrollOffset;
            set => SetScrollOffset(value);
        }

        internal Vector2 ContentSize
        {
            get => mContentSize;
            set
            {
                mContentSize = value;

                // If viewport has valid geometry, update immediately
                // Otherwise, schedule an update for when layout is ready
                if (Viewport.layout.width > 0 && Viewport.layout.height > 0)
                {
                    UpdateScrollers();
                }
                else
                {
                    schedule.Execute(UpdateScrollers);
                }
            }
        }

        internal CanvasScrollView()
        {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            mainContainer.style.flexDirection = FlexDirection.Row;
            Add(mainContainer);

            Viewport = new VisualElement();
            Viewport.style.backgroundColor =
                UnityStyles.Colors.BranchExplorer.ControlBackgroundColor;
            Viewport.name = "viewport";
            Viewport.style.flexGrow = 1;
            Viewport.style.overflow = Overflow.Hidden;
            mainContainer.Add(Viewport);

            mVerticalScroller = new Scroller(0, 100, OnVerticalScrollChanged);
            mVerticalScroller.viewDataKey = "VerticalScroller";
            mVerticalScroller.style.width = 15;
            mainContainer.Add(mVerticalScroller);

            mBottomContainer = new VisualElement();
            mBottomContainer.style.flexDirection = FlexDirection.Row;
            mBottomContainer.style.height = 15;
            Add(mBottomContainer);

            mHorizontalScroller = new Scroller(0, 100, OnHorizontalScrollChanged, SliderDirection.Horizontal);
            mHorizontalScroller.viewDataKey = "HorizontalScroller";
            mHorizontalScroller.style.flexGrow = 1;
            mBottomContainer.Add(mHorizontalScroller);

            mCorner = new VisualElement();
            mCorner.style.width = 15;
            mCorner.style.height = 15;
            mBottomContainer.Add(mCorner);

            ContentContainer = new VisualElement();
            ContentContainer.name = "content-container";
            ContentContainer.style.position = Position.Absolute;
            Viewport.Add(ContentContainer);

            Viewport.RegisterCallback<WheelEvent>(OnWheelEvent);
            Viewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        }

        internal void Dispose()
        {
            Viewport.UnregisterCallback<WheelEvent>(OnWheelEvent);
            Viewport.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        }

        void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateScrollers();
            SetScrollOffset(mScrollOffset);
        }

        void OnWheelEvent(WheelEvent evt)
        {
            if (evt.ctrlKey)
                return;

            if (evt.pressedButtons != 0)
                return;

            Vector2 delta = evt.delta * MOUSE_WHEEL_SCROLL_SIZE;

            ScrollOffset = new Vector2(
                ScrollOffset.x + delta.x,
                ScrollOffset.y + delta.y);

            evt.StopPropagation();
        }

        void OnHorizontalScrollChanged(float value)
        {
            mScrollOffset.x = value;
            ApplyScrollPosition();
        }

        void OnVerticalScrollChanged(float value)
        {
            mScrollOffset.y = value;
            ApplyScrollPosition();
        }

        void SetScrollOffset(Vector2 newOffset)
        {
            float viewportWidth = Viewport.layout.width;
            float viewportHeight = Viewport.layout.height;

            float maxScrollX = Mathf.Max(0, mContentSize.x - viewportWidth);
            float maxScrollY = Mathf.Max(0, mContentSize.y - viewportHeight);

            mScrollOffset.x = Mathf.Clamp(newOffset.x, 0, maxScrollX);
            mScrollOffset.y = Mathf.Clamp(newOffset.y, 0, maxScrollY);

            // Update scrollers without triggering callbacks
            mHorizontalScroller.slider.SetValueWithoutNotify(mScrollOffset.x);
            mVerticalScroller.slider.SetValueWithoutNotify(mScrollOffset.y);

            ApplyScrollPosition();
        }

        void UpdateScrollers()
        {
            if (Viewport.layout.width <= 0 || Viewport.layout.height <= 0)
                return;

            float viewportWidth = Viewport.layout.width;
            float viewportHeight = Viewport.layout.height;

            float maxScrollX = Mathf.Max(0, mContentSize.x - viewportWidth);
            mHorizontalScroller.lowValue = 0;
            mHorizontalScroller.highValue = maxScrollX;

            float horizontalFactor = mContentSize.x > 0 ? viewportWidth / mContentSize.x : 1f;
            mHorizontalScroller.Adjust(horizontalFactor);

            float maxScrollY = Mathf.Max(0, mContentSize.y - viewportHeight);
            mVerticalScroller.lowValue = 0;
            mVerticalScroller.highValue = maxScrollY;

            float verticalFactor = mContentSize.y > 0 ? viewportHeight / mContentSize.y : 1f;
            mVerticalScroller.Adjust(verticalFactor);

            bool showHorizontalScroller = maxScrollX > 0;
            bool showVerticalScroller = maxScrollY > 0;

            mBottomContainer.style.display = showHorizontalScroller ? DisplayStyle.Flex : DisplayStyle.None;
            mVerticalScroller.style.display = showVerticalScroller ? DisplayStyle.Flex : DisplayStyle.None;
            mCorner.style.display = showHorizontalScroller && showVerticalScroller ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ApplyScrollPosition()
        {
            // Position the content container using transform (negative because we're moving content opposite to scroll)
            ContentContainer.style.translate = new Translate(-mScrollOffset.x, -mScrollOffset.y);

            OnScrollChanged?.Invoke(mScrollOffset);
        }

        Vector2 mScrollOffset;
        Vector2 mContentSize;

        VisualElement mBottomContainer;
        VisualElement mCorner;
        Scroller mHorizontalScroller;
        Scroller mVerticalScroller;

        // Matches Unity's default UIElementsUtility.singleLineHeight (18px)
        const float MOUSE_WHEEL_SCROLL_SIZE = 18f;
    }
}
