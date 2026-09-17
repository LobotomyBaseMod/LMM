using System;
using System.Diagnostics;

using Codice.Client.Common.Threading;
using Codice.LogWrapper;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class UnityThreadWaiterBuilder : IThreadWaiterBuilder
    {
        IThreadWaiter IThreadWaiterBuilder.GetWaiter()
        {
            return new UnityThreadWaiter(mPlasticTimerBuilder, false);
        }

        IThreadWaiter IThreadWaiterBuilder.GetWaiter(int timerIntervalMilliseconds)
        {
            return new UnityThreadWaiter(mPlasticTimerBuilder, false, timerIntervalMilliseconds);
        }

        IThreadWaiter IThreadWaiterBuilder.GetModalWaiter()
        {
            return new UnityThreadWaiter(mPlasticTimerBuilder, true);
        }

        IThreadWaiter IThreadWaiterBuilder.GetModalWaiter(int timerIntervalMilliseconds)
        {
            return new UnityThreadWaiter(mPlasticTimerBuilder, true, timerIntervalMilliseconds);
        }

        IPlasticTimer IThreadWaiterBuilder.GetTimer(
            int timerIntervalMilliseconds, ThreadWaiter.TimerTick timerTickDelegate)
        {
            return mPlasticTimerBuilder.Get(false, timerIntervalMilliseconds, timerTickDelegate);
        }

        static IPlasticTimerBuilder mPlasticTimerBuilder = new UnityPlasticTimerBuilder();
    }

    internal class UnityThreadWaiter : IThreadWaiter
    {
        Exception IThreadWaiter.Exception { get { return mThreadOperation.Exception; } }

        internal UnityThreadWaiter(
            IPlasticTimerBuilder timerBuilder, bool bModalMode)
        {
            mPlasticTimer = timerBuilder.Get(bModalMode, OnTimerTick);
        }

        internal UnityThreadWaiter(
            IPlasticTimerBuilder timerBuilder,
            bool bModalMode,
            int timerIntervalMilliseconds)
        {
            mPlasticTimer = timerBuilder.Get(bModalMode, timerIntervalMilliseconds, OnTimerTick);
        }

        internal static void Shutdown()
        {
            mbIsShutdown = true;
        }

        void IThreadWaiter.Execute(
            PlasticThread.Operation threadOperationDelegate,
            PlasticThread.Operation afterOperationDelegate)
        {
            ((IThreadWaiter)(this)).Execute(threadOperationDelegate, afterOperationDelegate, null);
        }

        void IThreadWaiter.Execute(
            PlasticThread.Operation threadOperationDelegate,
            PlasticThread.Operation afterOperationDelegate,
            PlasticThread.Operation timerTickDelegate)
        {
            mThreadOperation = new PlasticThread(threadOperationDelegate);
            mAfterOperationDelegate = afterOperationDelegate;
            mTimerTickDelegate = timerTickDelegate;

            if (mbIsShutdown)
            {
                mLog.WarnFormat(
                    "Shutdown in progress, skipping execution. Callstack:\n{0}",
                    new StackTrace().ToString());
                return;
            }

            ThreadWaiterRegistry.Register(this);

            mPlasticTimer.Start();

            mThreadOperation.Execute();
        }

        void IThreadWaiter.Cancel()
        {
            mbCancelled = true;
        }

        void OnTimerTick()
        {
            if (mThreadOperation.IsRunning)
            {
                if (mTimerTickDelegate != null && !mbCancelled)
                    EditorDispatcher.Dispatch(() => mTimerTickDelegate());

                return;
            }

            mPlasticTimer.Stop();

            ThreadWaiterRegistry.Unregister(this);

            if (mbCancelled)
                return;

            EditorDispatcher.Dispatch(() => mAfterOperationDelegate());
        }

        bool mbCancelled = false;

        IPlasticTimer mPlasticTimer;
        PlasticThread mThreadOperation;
        PlasticThread.Operation mTimerTickDelegate;
        PlasticThread.Operation mAfterOperationDelegate;

        static volatile bool mbIsShutdown = false;
        static readonly ILog mLog = LogManager.GetLogger("ThreadWaiter");
    }
}
