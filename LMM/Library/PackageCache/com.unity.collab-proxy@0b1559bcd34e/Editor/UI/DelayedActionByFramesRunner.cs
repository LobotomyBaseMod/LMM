using System;

using UnityEditor;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class DelayedActionByFramesRunner
    {
        internal static bool IsUnitTesting { get; set; }
        internal bool IsRunning { get { return mIsOnDelay; } }

        internal DelayedActionByFramesRunner(Action action, int delayFrames)
        {
            mAction = action;
            mDelayFrames = delayFrames;
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

        void RefreshDelay()
        {
            mFramesOnDelay = mDelayFrames;
        }

        void StartDelay()
        {
            mIsOnDelay = true;

            EditorApplication.update += OnUpdate;

            RefreshDelay();
        }

        void EndDelay()
        {
            mIsOnDelay = false;

            EditorApplication.update -= OnUpdate;

            mAction();
        }

        void OnUpdate()
        {
            mFramesOnDelay--;

            if (mFramesOnDelay <= 0)
                EndDelay();
        }

        bool mIsOnDelay;
        int mFramesOnDelay;

        readonly int mDelayFrames;
        readonly Action mAction;
    }
}
