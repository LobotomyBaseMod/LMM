using UnityEditor;

namespace Unity.PlasticSCM.Editor.CloudDrive
{
    internal static class RefreshAsset
    {
        internal static void BeforeLongAssetOperation()
        {
            AssetDatabase.DisallowAutoRefresh();
        }

        internal static void AfterLongAssetOperation()
        {
            AssetDatabase.AllowAutoRefresh();

            AssetDatabase.Refresh(ImportAssetOptions.Default);
        }
    }
}
