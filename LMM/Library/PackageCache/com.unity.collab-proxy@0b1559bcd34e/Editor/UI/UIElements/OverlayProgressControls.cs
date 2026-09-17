using System;
using System.Threading.Tasks;
using PlasticGui;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.UI.UIElements
{
    internal class OverlayProgressControls : VisualElement, IProgressControls
    {
        internal OverlayProgressControls(params VisualElement[] actionControls)
        {
            mActionControls = actionControls;

            InitializeLayoutAndStyles();

            BuildComponents();

            visible = false;
        }

        internal Task ShowProgress(string message, TimeSpan after)
        {
            lock (mLock)
            {
                mIsOperationRunning = true;
                mCurrentOperation = new object();
            }

            return Task.Delay(after).ContinueWith((task, state) =>
            {
                EditorDispatcher.Dispatch(() =>
                {
                    lock (mLock)
                    {
                        if (!mIsOperationRunning)
                            return;

                        if (state != mCurrentOperation)
                            return;
                    }

                    ((IProgressControls)this).ShowProgress(message);
                });
            }, mCurrentOperation);
        }

        void EnableActionControls(bool enable)
        {
            foreach (var control in mActionControls)
                if (control != null)
                    control.SetEnabled(enable);
        }

        void IProgressControls.ShowProgress(string message)
        {
            lock (mLock)
            {
                mIsOperationRunning = true;

                mProgressBar.title = message;

                if (visible)
                    return;

                visible = true;
                EnableActionControls(false);

                mLastUpdateTime = EditorApplication.timeSinceStartup;
                mTimer = schedule.Execute(UpdateProgress).Every(16);
            }
        }

        void IProgressControls.HideProgress()
        {
            lock (mLock)
            {
                mIsOperationRunning = false;

                if (!visible)
                    return;

                visible = false;
                EnableActionControls(true);

                mTimer.Pause();
                mTimer = null;
            }
        }

        void IProgressControls.ShowNotification(string message)
        {
            throw new System.NotImplementedException();
        }

        void IProgressControls.ShowError(string message)
        {
            throw new System.NotImplementedException();
        }

        void IProgressControls.ShowWarning(string message)
        {
            throw new System.NotImplementedException();
        }

        void IProgressControls.ShowSuccess(string message)
        {
            throw new System.NotImplementedException();
        }

        void UpdateProgress()
        {
            double now = EditorApplication.timeSinceStartup;
            double deltaTime = now - mLastUpdateTime;
            mLastUpdateTime = now;

            double deltaPercent = deltaTime * PERCENT_PER_SECONDS;
            mProgressBar.value = Mathf.Repeat(mProgressBar.value + (float)deltaPercent, 1f);
        }

        void InitializeLayoutAndStyles()
        {
            style.position = Position.Absolute;
            style.top = 0;
            style.left = 0;
            style.bottom = 0;
            style.right = 0;
            style.justifyContent = Justify.Center;
            style.alignContent = Align.Center;
            style.backgroundColor = UnityStyles.Colors.OverlayProgressBackgroundColor;
        }

        void BuildComponents()
        {
            mProgressBar = new ProgressBar();
            mProgressBar.lowValue = 0;
            mProgressBar.highValue = 1;

            mProgressBar.style.alignSelf = Align.Center;

            Label progressBarLabel = mProgressBar.Q<Label>();
            progressBarLabel.style.marginLeft = 30;
            progressBarLabel.style.marginRight = 30;

            progressBarLabel.style.textOverflow = TextOverflow.Ellipsis;
            progressBarLabel.style.overflow = Overflow.Hidden;
            progressBarLabel.style.unityTextOverflowPosition = TextOverflowPosition.End;

            Add(mProgressBar);
        }

        double mLastUpdateTime;
        const double PERCENT_PER_SECONDS = 0.6;

        object mLock = new object();
        object mCurrentOperation = new object();
        bool mIsOperationRunning;

        IVisualElementScheduledItem mTimer;
        ProgressBar mProgressBar;
        readonly VisualElement[] mActionControls;
    }
}
