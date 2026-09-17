using UnityEditorWindow = UnityEditor.EditorWindow;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal class EditorWindow
    {
        internal delegate UnityEditorWindow GetInspectorWindowDelegate();
        internal static GetInspectorWindowDelegate GetInspectorWindow { get; set; }

        internal delegate void Internal_MakeModalDelegate(ContainerWindow window);
        internal static Internal_MakeModalDelegate Internal_MakeModal { get; set; }
    }
}
