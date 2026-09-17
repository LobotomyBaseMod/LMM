using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessRefreshView : IRefreshView
    {
        void IRefreshView.ForType(ViewType viewType)
        {
            UVCSWindow window = GetWindowIfOpened.UVCS();

            if (window == null)
                return;

            window.IRefreshView.ForType(viewType);
        }
    }
}
