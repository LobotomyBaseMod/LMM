using System;

using UnityEditor;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class DelayedActionBySecondsRunner
    {
        internal static bool IsUnitTesting { get; set; }
        internal bool IsRunning { get { return mIsOnDelay; } }

        internal DelayedActionBySecondsRunner(Action action, double delaySeconds)
        {
            mAction = action;
            mDelaySeconds = delaySeconds;
        }

        internal void Run()
        {
            if (IsUnitTesting)
            {
                mAction();
                return;
            }

            if (mIsOnDelay)
            {
                RefreshDelay();
                return;
            }

            StartDelay();
        }

        internal void Pause()
        {
            mIsPaused = true;
        }

        internal void Resume()
        {
            if (!mIsPaused)
                return;

            mIsPaused = false;
            mLastUpdateTime = EditorApplication.timeSinceStartup;
        }

        void RefreshDelay()
        {
            mIsOnDelay = true;

            mSecondsOnDelay = mDelaySeconds;
        }

        void StartDelay()
        {
            mLastUpdateTime = EditorApplication.timeSinceStartup;

            EditorApplication.update += OnUpdate;

            RefreshDelay();
        }

        void EndDelay()
        {
            EditorApplication.update -= OnUpdate;

            mIsOnDelay = false;

            mAction();
        }

        void OnUpdate()
        {
            if (mIsPaused)
                return;

            double updateTime = EditorApplication.timeSinceStartup;
            double deltaSeconds = updateTime - mLastUpdateTime;

            mSecondsOnDelay -= deltaSeconds;

            if (mSecondsOnDelay < 0)
                EndDelay();

            mLastUpdateTime = updateTime;
        }

        bool mIsOnDelay;
        bool mIsPaused;
        double mLastUpdateTime;
        double mSecondsOnDelay;

        readonly double mDelaySeconds;
        readonly Action mAction;
    }
}
