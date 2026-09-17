using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessViewSwitcher : IViewSwitcher
    {
        void IViewSwitcher.CloseMergeView()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IViewSwitcher.CloseMergeView();
        }

        void IViewSwitcher.DisableMergeView()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IViewSwitcher.DisableMergeView();
        }

        IMergeView IViewSwitcher.GetMergeView()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return null;

            return window.IViewSwitcher.GetMergeView();
        }

        bool IViewSwitcher.IsIncomingChangesView()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return false;

            return window.IViewSwitcher.IsIncomingChangesView();
        }

        void IViewSwitcher.ShowBranchExplorerView()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IViewSwitcher.ShowBranchExplorerView();
        }

        void IViewSwitcher.ShowPendingChanges()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IViewSwitcher.ShowPendingChanges();
        }

        void IViewSwitcher.ShowShelvesView()
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IViewSwitcher.ShowShelvesView();
        }

        void IViewSwitcher.ShowSyncView(string syncViewToSelect)
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IViewSwitcher.ShowSyncView(syncViewToSelect);
        }

        void IViewSwitcher.ShowView(ViewType viewType)
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IViewSwitcher.ShowView(viewType);
        }
    }
}
