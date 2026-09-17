namespace Unity.PlasticSCM.Editor.Views.Properties
{
    internal static class PropertiesRefreshNotifier
    {
        internal static long Version => mVersion;

        internal static void Notify()
        {
            mVersion++;
        }

        static long mVersion;
    }
}
