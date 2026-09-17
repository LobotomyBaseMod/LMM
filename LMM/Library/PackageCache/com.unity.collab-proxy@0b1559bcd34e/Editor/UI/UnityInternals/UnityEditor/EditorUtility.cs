namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal static class EditorUtility
    {
        internal delegate void Internal_UpdateAllMenusDelegate();

        internal static Internal_UpdateAllMenusDelegate Internal_UpdateAllMenus { get; set; }
    }
}
