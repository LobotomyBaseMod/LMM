using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Zoom
{
    internal class BrExPan
    {
        internal BrExPan(VisualElement target, BrExZoom zoom)
        {
            mTarget = target;
            mZoom = zoom;

            mTarget.RegisterCallback<PointerDownEvent>(OnPointerDown);
            mTarget.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            mTarget.RegisterCallback<PointerUpEvent>(OnPointerUp);
            mTarget.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        internal void Dispose()
        {
            mTarget.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            mTarget.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            mTarget.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            mTarget.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.clickCount > 1)
                return;

            if (evt.button != 0 && evt.button != 2) // left or middle button
                return;

            mIsDragginng = true;

            mDragStartTimeStamp = System.Environment.TickCount;

            mMouseDownPoint = evt.position;

            mStartTranslatePoint = mZoom.Offset;

            mTarget.CapturePointer(evt.pointerId);

            mZoom.StopAnimations();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!mIsDragginng)
                return;

            if (evt.pressedButtons == 0)
            {
                mIsDragginng = false;
                mTarget.ReleasePointer(evt.pointerId);
                return;
            }

            //mTarget.style.cursor = new StyleCursor(Cursor.MoveArrow);

            if (Mathf.Approximately(mMouseDownPoint.x, evt.position.x) &&
                Mathf.Approximately(mMouseDownPoint.y, evt.position.y))
                return;

            MoveBy(mMouseDownPoint - new Vector2(evt.position.x, evt.position.y));
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!mIsDragginng)
                return;

            //mTarget.style.cursor = new StyleCursor(Cursor.MoveArrow);

            mIsDragginng = false;

            mTarget.ReleasePointer(evt.pointerId);

            KinectScroll(evt.position);
        }

        void KinectScroll(Vector2 mouseUpPoint)
        {
            Vector2 distance = mMouseDownPoint - mouseUpPoint;

            if (distance.magnitude < MIN_DISTANCE_TO_ANIMATE_SCROLL)
                return;

            int dragTime = Mathf.Max(Environment.TickCount - mDragStartTimeStamp, 1);

            float speedX = LimitSpeedValue(distance.x / dragTime);
            float speedY = LimitSpeedValue(distance.y / dragTime);

            if (Mathf.Abs(speedX) < 1 && Mathf.Abs(speedY) < 1)
                return;

            float speedXAbs = Mathf.Abs(speedX);
            float speedYAbs = Mathf.Abs(speedY);

            // preserve the sign (exp always return positive values)
            float xFactor = (speedX == 0) ? 0 : Mathf.Exp(speedXAbs - 1) * speedXAbs / speedX;
            float yFactor = (speedY == 0) ? 0 : Mathf.Exp(speedYAbs - 1) * speedYAbs / speedY;

            Vector2 startPoint = new Vector2(
                mStartTranslatePoint.x + distance.x,
                mStartTranslatePoint.y + distance.y);

            Vector2 targetPoint = new Vector2(
                startPoint.x + xFactor * INERTIAL_FACTOR,
                startPoint.y + yFactor * INERTIAL_FACTOR);

            mZoom.AnimateScroll(startPoint, targetPoint,
                TimeSpan.FromMilliseconds(SCROLL_DECELERATION_TIME));
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            //mTarget.style.cursor = new StyleCursor(Cursor.Arrow);

            mIsDragginng = false;
        }

        void MoveBy(Vector2 distance)
        {
            mZoom.Offset = new Vector2(
                mStartTranslatePoint.x + distance.x,
                mStartTranslatePoint.y + distance.y);
        }

        float LimitSpeedValue(float speed)
        {
            if (speed > 0 && speed > MAX_SPEED)
                return MAX_SPEED;

            if (speed < 0 && speed < -MAX_SPEED)
                return -MAX_SPEED;

            return speed;
        }

        int mDragStartTimeStamp;
        bool mIsDragginng;

        Vector2 mMouseDownPoint;
        Vector2 mStartTranslatePoint;

        readonly VisualElement mTarget;
        readonly BrExZoom mZoom;

        const float MAX_SPEED = 8;
        const int MIN_DISTANCE_TO_ANIMATE_SCROLL = 100;
        const int SCROLL_DECELERATION_TIME = 900;
        const int INERTIAL_FACTOR = 40; // the bigger, more sensitive
    }
}
