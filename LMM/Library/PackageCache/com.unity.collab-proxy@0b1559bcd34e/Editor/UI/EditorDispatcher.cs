using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using Codice.LogWrapper;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class EditorDispatcher
    {
        internal static bool IsOnMainThread
        {
            get { return Thread.CurrentThread.ManagedThreadId == mMainThreadId; }
        }

        internal static void InitializeMainThreadIdAndContext(
            int mainThreadId,
            SynchronizationContext mainUnitySyncContext)
        {
            mMainThreadId = mainThreadId;
            mMainUnitySyncContext = mainUnitySyncContext;
        }

        internal static void VerifyMainThreadAccess()
        {
            if (!IsOnMainThread)
            {
                [DoesNotReturn]
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void ThrowVerifyAccess()
                    => throw new InvalidOperationException("Call from invalid thread");
                ThrowVerifyAccess();
            }
        }

        internal static void Shutdown()
        {
            lock (mLock)
            {
                mMainUnitySyncContext = null;
                mDispatchQueue.Clear();
            }
        }

        internal static void Dispatch(Action task)
        {
            bool shouldPost = false;
            SynchronizationContext syncContext = null;

            lock (mLock)
            {
                syncContext = mMainUnitySyncContext;
                if (syncContext == null)
                    return;

                mDispatchQueue.Enqueue(task);

                if (mDispatchQueue.Count == 1)
                    shouldPost = true;
            }

            if (shouldPost)
                syncContext.Post(_ => Update(), null);
        }

        internal static void Update()
        {
            if (!IsOnMainThread)
            {
                throw new InvalidOperationException(
                    "EditorDispatcher.Update() must be called on the main thread");
            }

            Action[] actions;

            lock (mLock)
            {
                if (mDispatchQueue.Count == 0)
                    return;

                actions = mDispatchQueue.ToArray();
                mDispatchQueue.Clear();
            }

            foreach (Action action in actions)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    mLog.ErrorFormat("Error dispatching action: {0}", ex.Message);
                    mLog.DebugFormat("Stack trace: {0}", ex.StackTrace);
                }
            }
        }

        static readonly ILog mLog = PlasticApp.GetLogger("EditorDispatcher");
        static readonly object mLock = new object();
        static SynchronizationContext mMainUnitySyncContext;
        static readonly Queue<Action> mDispatchQueue = new Queue<Action>();
        static int mMainThreadId;
    }
}
