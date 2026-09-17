using System;

using UnityEditor;

using Codice.Client.BaseCommands;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessGluonUpdateProgress : IUpdateProgress
    {
        void IUpdateProgress.ShowNoCancelableProgress()
        {
            if (mProgressId != -1)
                Progress.Finish(mProgressId);

            mProgressId = Progress.Start(
                PlasticLocalization.GetString(PlasticLocalization.Name.UpdatingWorkspace));
        }

        void IUpdateProgress.EndProgress()
        {
            Progress.Finish(mProgressId);
            mProgressId = -1;
        }

        void IUpdateProgress.RefreshProgress(
            UpdateProgress updateProgress,
            UpdateProgressData updateProgressData)
        {
            if (mProgressId == -1)
                return;

            float value = (float)updateProgressData.ProgressValue / 100f;

            Progress.Report(
                mProgressId,
                value,
                updateProgressData.Status);
        }

        void IUpdateProgress.ShowCancelableProgress()
        {
            throw new NotImplementedException();
        }

        int mProgressId = -1;
    }
}
