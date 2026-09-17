using System;

using UnityEditor;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Update;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessUpdateProgress
    {
        internal void ShowUpdateProgress(string title, UpdateNotifier notifier)
        {
            mProgressData = new BuildProgressSpeedAndRemainingTime.ProgressData(DateTime.Now);

            if (mProgressId == -1)
                Progress.Finish(mProgressId);

            mProgressId = Progress.Start(title);
            mUpdateProgressNotifier = notifier;

            EditorApplication.update += UpdateProgress;
        }

        internal void EndUpdateProgress()
        {
            EditorApplication.update -= UpdateProgress;
            Progress.Finish(mProgressId);
            mProgressId = -1;
        }

        internal bool IsOperationRunning()
        {
            return mProgressId != -1;
        }

        void UpdateProgress()
        {
            if (mProgressId == -1)
                return;

            UpdateOperationStatus status = mUpdateProgressNotifier.GetUpdateStatus();

            float progress = GetProgressBarPercent.ForTransfer(
                status.UpdatedSize, status.TotalSize) / 100f;

            Progress.Report(
                mProgressId,
                progress,
                UpdateProgressRender.GetProgressString(status, mProgressData));
        }

        int mProgressId = -1;
        UpdateNotifier mUpdateProgressNotifier;
        BuildProgressSpeedAndRemainingTime.ProgressData mProgressData;
    }
}
