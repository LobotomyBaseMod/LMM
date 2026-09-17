using UnityEditor;

using PlasticGui;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessProgressControls : IProgressControls
    {
        void IProgressControls.ShowProgress(string message)
        {
            if (mProgressId != -1)
                Progress.Finish(mProgressId);

            mProgressId = Progress.Start(message, null, Progress.Options.Indefinite);
        }

        void IProgressControls.HideProgress()
        {
            Progress.Finish(mProgressId);
            mProgressId = -1;
        }

        void IProgressControls.ShowError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        void IProgressControls.ShowNotification(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        void IProgressControls.ShowSuccess(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        void IProgressControls.ShowWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        int mProgressId = -1;
    }
}
