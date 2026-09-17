using System;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.Common.Threading;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal class BranchExplorerFocusListener : BranchExplorerSelection.IBranchExplorerFocusListener
    {
        internal BranchExplorerFocusListener()
        {
            mRunner = new DelayedActionBySecondsRunner(
                NotifyDelayedFocusChanged,
                UnityConstants.SELECTION_DELAYED_INPUT_ACTION_INTERVAL);
        }

        void BranchExplorerSelection.IBranchExplorerFocusListener.OnFocusChanged(ObjectDrawInfo objecDrawInfo)
        {
            mFocusedObjectDrawInfo = objecDrawInfo;

            mRunner.Run();
        }

        internal void AddFocusedObjectObserver(IFocusedObjectObserver observer)
        {
            mFocusObserver = observer;
        }

        internal void RemoveFocusedObjectObserver()
        {
            mFocusObserver = null;
        }

        void NotifyDelayedFocusChanged()
        {
            if (mFocusObserver == null)
                return;

            try
            {
                mFocusObserver.DelayedFocusedObjectChanged(mFocusedObjectDrawInfo);
            }
            catch (Exception ex)
            {
                ExceptionsHandler.HandleException("BranchExplorerFocusListener", ex);
            }
        }

        ObjectDrawInfo mFocusedObjectDrawInfo;

        IFocusedObjectObserver mFocusObserver;
        DelayedActionBySecondsRunner mRunner;
    }
}
