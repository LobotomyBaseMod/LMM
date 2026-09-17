namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class InternalEditorGUIUtility
    {
        internal delegate bool HasCurrentWindowKeyFocusDelegate();

        internal static HasCurrentWindowKeyFocusDelegate HasCurrentWindowKeyFocus { get; set; }
    }
}
