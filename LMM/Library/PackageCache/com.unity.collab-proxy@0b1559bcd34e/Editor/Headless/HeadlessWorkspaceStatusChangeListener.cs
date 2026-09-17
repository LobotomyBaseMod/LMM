using System;

using PlasticGui.Gluon;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessWorkspaceStatusChangeListener : IWorkspaceStatusChangeListener
    {
        internal HeadlessWorkspaceStatusChangeListener(Action refreshWorkspaceWorkingInfo)
        {
            mRefreshWorkspaceWorkingInfo = refreshWorkspaceWorkingInfo;
        }

        void IWorkspaceStatusChangeListener.OnWorkspaceStatusChanged()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
            {
                mRefreshWorkspaceWorkingInfo();
                return;
            }

            window.IWorkspaceStatusChangeListener.OnWorkspaceStatusChanged();
        }

        readonly Action mRefreshWorkspaceWorkingInfo;
    }
}
