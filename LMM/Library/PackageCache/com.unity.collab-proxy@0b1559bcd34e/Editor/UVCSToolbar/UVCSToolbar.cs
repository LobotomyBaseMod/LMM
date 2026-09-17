namespace Unity.PlasticSCM.Editor.Toolbar
{
    internal static class UVCSToolbar
    {
        internal static ToolbarController Controller
        {
            get { return mController; }
        }

        static UVCSToolbar()
        {
            mController = new ToolbarController(UVCSPlugin.Instance);
        }

        static readonly ToolbarController mController;
    }
}
