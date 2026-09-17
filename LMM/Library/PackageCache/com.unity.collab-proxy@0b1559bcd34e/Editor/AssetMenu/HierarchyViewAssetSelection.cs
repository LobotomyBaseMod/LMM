using UnityEditor.VersionControl;

namespace Unity.PlasticSCM.Editor.AssetMenu
{
    internal class HierarchyViewAssetSelection : AssetUVCSOperations.IAssetSelection
    {
        AssetList AssetUVCSOperations.IAssetSelection.GetSelectedAssets()
        {
            return mSelectedAsset != null ? new AssetList() { mSelectedAsset } : new AssetList();
        }

        internal void SetActiveAsset(Asset asset)
        {
            mSelectedAsset = asset;
        }

        Asset mSelectedAsset;
    }
}
