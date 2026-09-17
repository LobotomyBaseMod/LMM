using UnityEditor;

#if !UNITY_6000_3_OR_NEWER
using Unity.PlasticSCM.Editor.UnityInternals.UnityEditor;

using EditorWindow = UnityEditor.EditorWindow;
using DockArea = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.DockArea;
#endif

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class DockEditorWindow
    {
        internal static void To(EditorWindow dockWindow, EditorWindow window)
        {
#if !UNITY_6000_3_OR_NEWER
            DockArea dockArea = dockWindow.m_Parent() as DockArea;
#else
            DockArea dockArea = dockWindow.m_Parent as DockArea;
#endif

            if (dockArea == null)
                return;

            dockArea.AddTab(window);
        }
    }
}
