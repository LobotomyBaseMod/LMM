using System;

using UnityEditor;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList
{
    internal class PopupupProgressBar
    {
        internal PopupupProgressBar(
            Action repaint,
            float cycleDurationSeconds = 1.5f)
        {
            mRepaint = repaint;
            mCycleDurationSeconds = cycleDurationSeconds;
        }

        internal bool IsVisible { get; set; }

        internal float Progress
        {
            get { return mProgress; }
        }

        internal void Reset()
        {
            mProgressBarLastTime = 0;
            mProgressBarAccumulatedTime = 0;
            mProgress = 0;
        }

        internal void OnEditorApplicationUpdate()
        {
            if (!IsVisible)
                return;

            double currentTime = EditorApplication.timeSinceStartup;
            double deltaTime = currentTime - mProgressBarLastTime;
            mProgressBarLastTime = currentTime;

            mProgressBarAccumulatedTime += deltaTime;
            mProgress = (float)(mProgressBarAccumulatedTime % mCycleDurationSeconds) / mCycleDurationSeconds;

            mRepaint();
        }

        readonly Action mRepaint;
        readonly float mCycleDurationSeconds;

        double mProgressBarLastTime = 0f;
        double mProgressBarAccumulatedTime = 0f;
        float mProgress = 0f;
    }
}
