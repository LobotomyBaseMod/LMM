using UnityEditorWindow = UnityEditor.EditorWindow;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    class HostView
    {
        internal object InternalObject { get; }
        internal UnityEditorWindow m_ActualView => GetActualViewInternal(this);
        internal ContainerWindow window => GetWindow(this);

        internal HostView(object hostView)
        {
            InternalObject = hostView;
        }

        internal delegate UnityEditorWindow GetActualViewDelegate(HostView hostView);
        internal static GetActualViewDelegate GetActualViewInternal { get; set; }

        internal delegate ContainerWindow GetWindowDelegate(HostView hostView);
        internal static GetWindowDelegate GetWindow { get; set; }
    }

    internal class DockArea : HostView
    {
        internal DockArea(object hostView) : base(hostView) { }

        internal void AddTab(UnityEditorWindow pane, bool sendPaneEvents = true)
        {
            AddTabInternal(this, pane, sendPaneEvents);
        }

        internal delegate void AddTabDelegate(DockArea dockArea, UnityEditorWindow pane, bool sendPaneEvents = true);
        internal static AddTabDelegate AddTabInternal { get; set; }
    }
}
