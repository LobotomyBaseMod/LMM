using Codice.CM.Common;

using PlasticGui;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessHistoryViewLauncher : IHistoryViewLauncher
    {
        internal HeadlessHistoryViewLauncher(UVCSPlugin uvcsPlugin)
        {
            mUVCSPlugin = uvcsPlugin;
        }

        void IHistoryViewLauncher.ShowHistoryView(
            RepositorySpec repSpec, long itemId, string path, bool isDirectory)
        {
            UVCSWindow window = SwitchUVCSPlugin.OnIfNeeded(mUVCSPlugin);

            window.IHistoryViewLauncher.ShowHistoryView(repSpec, itemId, path, isDirectory);
        }

        readonly UVCSPlugin mUVCSPlugin;
    }
}
