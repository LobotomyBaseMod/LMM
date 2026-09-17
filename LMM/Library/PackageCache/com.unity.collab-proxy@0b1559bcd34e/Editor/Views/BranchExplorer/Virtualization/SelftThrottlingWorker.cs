using System;
using System.Diagnostics;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal class SelfThrottlingWorker
    {
        internal delegate int QuantizedWorkHandler(int quantum);

        internal void CreateQuanta(QuantizedWorkHandler quantizedWorkHandler)
        {
            mCreateQuanta = SelfThrottlingWork(
                mCreateQuanta, mIdealDuration, quantizedWorkHandler);
        }

        internal void RemoveQuanta(QuantizedWorkHandler quantizedWorkHandler)
        {
            mRemoveQuanta = SelfThrottlingWork(
                mRemoveQuanta, mIdealDuration, quantizedWorkHandler);
        }

        internal void GcQuanta(QuantizedWorkHandler quantizedWorkHandler)
        {
            mGcQuanta = SelfThrottlingWork(
                mGcQuanta, mIdealDuration, quantizedWorkHandler);
        }

        //Self-tuning how much time is allocated to the given handler.
        /// <param name="quantum">The current quantum allocation</param>
        /// <param name="idealDuration">The time in milliseconds we want to take</param>
        /// <param name="handler">The handler to call that does the work being throttled</param>
        /// <returns>Returns the new quantum to use next time that will more likely hit the ideal time</returns>
        static int SelfThrottlingWork(
            int quantum, int idealDuration, QuantizedWorkHandler handler)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            int count = handler(quantum);

            timer.Stop();
            long durationInMilliseconds = timer.ElapsedMilliseconds;

            if (durationInMilliseconds > 0 && count > 0)
            {
                long estimatedFullDuration = durationInMilliseconds * (quantum / count);
                long newQuanta = (quantum * idealDuration) / estimatedFullDuration;
                quantum = Math.Max(100, (int)Math.Min(newQuanta, int.MaxValue));
            }

            return quantum;
        }

        int mCreateQuanta = 1000;
        int mRemoveQuanta = 2000;
        int mGcQuanta = 5000;
        int mIdealDuration = 50; // 50 milliseconds.
    }
}
