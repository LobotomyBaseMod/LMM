using GluonGui;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessGluonViewHost
    {
        internal ViewHost ViewHost { get { return mViewHost; } }

        internal HeadlessGluonViewHost(IRefreshableView branchesListPopupPanel)
        {
            mViewHost = new ViewHost();

            mViewHost.AddRefreshableView(
                ViewType.BranchesListPopup,
                branchesListPopupPanel);
            mViewHost.AddRefreshableView(
                ViewType.PendingChangesView,
                new RefreshableView(ViewType.PendingChangesView));
            mViewHost.AddRefreshableView(
                ViewType.IncomingChangesView,
                new RefreshableView(ViewType.IncomingChangesView));
            mViewHost.AddRefreshableView(
                ViewType.BranchesView,
                new RefreshableView(ViewType.BranchesView));
            mViewHost.AddRefreshableView(
                ViewType.ChangesetsView,
                new RefreshableView(ViewType.ChangesetsView));
            mViewHost.AddRefreshableView(
                ViewType.HistoryView,
                new RefreshableView(ViewType.HistoryView));
        }

        ViewHost mViewHost;

        class RefreshableView : IRefreshableView
        {
            internal RefreshableView(ViewType viewType)
            {
                mViewType = viewType;
            }

            void IRefreshableView.Refresh()
            {
                UVCSWindow window = GetWindowIfOpened.UVCS();

                if (window == null)
                    return;

                window.ViewHost.RefreshView(mViewType);
            }

            readonly ViewType mViewType;
        }
    }
}
