using UnityEditor;
using UnityEngine;

using Codice.Client.Common.Authentication;
using PlasticGui;

namespace Unity.PlasticSCM.Editor.UI.Progress
{
    class ProgressControlsForDialogs : IProgressControls, IAuthenticationProgressControls
    {
        internal class Data
        {
            internal bool IsWaitingAsyncResult;
            internal float ProgressPercent;
            internal string ProgressMessage;

            internal MessageType StatusType;
            internal string StatusMessage;

            internal void CopyInto(Data other)
            {
                other.IsWaitingAsyncResult = IsWaitingAsyncResult;
                other.ProgressPercent = ProgressPercent;
                other.ProgressMessage = ProgressMessage;
                other.StatusType = StatusType;
                other.StatusMessage = StatusMessage;
            }
        }

        internal Data ProgressData { get { return mData; } }

        internal void UpdateProgress(EditorWindow dialog)
        {
            if (!mData.IsWaitingAsyncResult)
                return;

            if (Event.current.type == EventType.Repaint)
                dialog.Repaint();
        }

        void IProgressControls.HideProgress()
        {
            InternalHideProgress();
        }

        void IAuthenticationProgressControls.HideProgress()
        {
            InternalHideProgress();
        }

        void InternalHideProgress()
        {
            mData.IsWaitingAsyncResult = false;
            mData.ProgressMessage = string.Empty;
        }

        void IProgressControls.ShowProgress(string message)
        {
            InternalShowProgress(message);
        }

        void IAuthenticationProgressControls.ShowProgress(string message)
        {
            InternalShowProgress(message);
        }

        void InternalShowProgress(string message)
        {
            CleanStatusMessage(mData);

            mData.IsWaitingAsyncResult = true;
            mData.ProgressPercent = -1;
            mData.ProgressMessage = message;
        }

        void IProgressControls.ShowError(string message)
        {
            mData.StatusMessage = message;
            mData.StatusType = MessageType.Error;
        }

        void IProgressControls.ShowNotification(string message)
        {
            mData.StatusMessage = message;
            mData.StatusType = MessageType.Info;
        }

        void IProgressControls.ShowSuccess(string message)
        {
            mData.StatusMessage = message;
            mData.StatusType = MessageType.Info;
        }

        void IProgressControls.ShowWarning(string message)
        {
            mData.StatusMessage = message;
            mData.StatusType = MessageType.Warning;
        }

        static void CleanStatusMessage(Data data)
        {
            data.StatusMessage = string.Empty;
            data.StatusType = MessageType.None;
        }

        Data mData = new Data();
    }
}
