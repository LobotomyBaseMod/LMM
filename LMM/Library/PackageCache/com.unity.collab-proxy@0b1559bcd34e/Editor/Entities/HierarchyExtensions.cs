#if HAS_ENTITIES_PACKAGE
using UnityEditor;

namespace Unity.PlasticSCM.Editor.Entities
{
    [InitializeOnLoad]
    internal static class HierarchyExtensions
    {
        static HierarchyExtensions()
        {
            GetSubScenePathFromInstance.Register();
        }
    }
}
#endif
