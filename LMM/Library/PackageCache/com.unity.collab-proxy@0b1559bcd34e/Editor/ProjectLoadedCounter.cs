using UnityEditor;

using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal class ProjectLoadedCounter
    {
        internal static int Get()
        {
            return IntSetting.Load(UnityConstants.PROJECT_LOADED_COUNTER_KEY_NAME, 0);
        }

        internal static void Set(int value)
        {
            IntSetting.Save(value, UnityConstants.PROJECT_LOADED_COUNTER_KEY_NAME);
        }

        internal static void IncrementOnceOnEnable()
        {
            if (SessionState.GetBool(IS_PROJECT_LOADED_COUNTER_ALREADY_EXECUTED_KEY, false))
                return;

            Increment();

            SessionState.SetBool(IS_PROJECT_LOADED_COUNTER_ALREADY_EXECUTED_KEY, true);
        }

        static void Increment()
        {
            Set(Get() + 1);
        }

        internal const string IS_PROJECT_LOADED_COUNTER_ALREADY_EXECUTED_KEY =
            "PlasticSCM.ProjectLoadedCounter.IsAlreadyExecuted";
    }
}
