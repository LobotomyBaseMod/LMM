using UnityEditorWindow = UnityEditor.EditorWindow;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class UnityEditorExtensions
    {
        internal static HostView m_Parent(this UnityEditorWindow editorWindow)
        {
            return m_ParentInternal(editorWindow);
        }

        internal static void ShowWithMode(this UnityEditorWindow editorWindow, int mode)
        {
            ShowWithModeInternal(editorWindow, mode);
        }

        internal delegate HostView m_ParentDelegate(UnityEditorWindow editorWindow);
        internal static m_ParentDelegate m_ParentInternal { get; set; }

        internal delegate void ShowWithModeDelegate(UnityEditorWindow editorWindow, int mode);
        internal static ShowWithModeDelegate ShowWithModeInternal { get; set; }
    }
}
