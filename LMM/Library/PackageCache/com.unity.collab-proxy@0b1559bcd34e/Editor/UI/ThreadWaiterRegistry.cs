using System.Collections.Generic;
using Codice.Client.Common.Threading;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class ThreadWaiterRegistry
    {
        internal static void Register(IThreadWaiter waiter)
        {
            lock (mLock)
            {
                mRunningWaiters.Add(waiter);
            }
        }

        internal static void Unregister(IThreadWaiter waiter)
        {
            lock (mLock)
            {
                mRunningWaiters.Remove(waiter);
            }
        }

        internal static bool HasRunningOperations()
        {
            lock (mLock)
            {
                return mRunningWaiters.Count > 0;
            }
        }

        internal static int GetRunningOperationsCount()
        {
            lock (mLock)
            {
                return mRunningWaiters.Count;
            }
        }

        static readonly object mLock = new object();
        static readonly HashSet<IThreadWaiter> mRunningWaiters = new HashSet<IThreadWaiter>();
    }
}
