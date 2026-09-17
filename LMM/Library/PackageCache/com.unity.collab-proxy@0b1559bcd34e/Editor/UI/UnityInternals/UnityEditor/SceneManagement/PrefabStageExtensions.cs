using System.Reflection;

using UnityEditor.SceneManagement;

namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.SceneManagement
{
    internal static class PrefabStageExtensions
    {
        internal static bool Save(this PrefabStage prefabStage)
        {
            return SaveInternal(prefabStage);
        }

        internal static void AutoSave(this PrefabStage prefabStage)
        {
            MethodInfo autoSavePrefabMethod = typeof(PrefabStage).GetMethod(
                "AutoSave", BindingFlags.NonPublic | BindingFlags.Instance);

            if (autoSavePrefabMethod == null)
                return;

            autoSavePrefabMethod.Invoke(prefabStage, null);
        }

        internal delegate bool SaveDelegate(PrefabStage prefabStage);

        internal static SaveDelegate SaveInternal { get; set; }
    }
}
