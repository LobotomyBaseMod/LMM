using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    internal interface IAssetDatabase
    {
        string GetAssetPath(Object o);
        AssetImporter GetAssetImporterFromPath(string path);
        void StartAssetEdit();
        void StopAssetEdit();
    }

    internal class AssetDatabaseSystem : IAssetDatabase
    {
        public string GetAssetPath(Object o)
        {
            return AssetDatabase.GetAssetPath(o);
        }

        public AssetImporter GetAssetImporterFromPath(string path)
        {
            return AssetImporter.GetAtPath(path);
        }

        public void StartAssetEdit()
        {
            AssetDatabase.StartAssetEditing();
        }

        public void StopAssetEdit()
        {
            AssetDatabase.StopAssetEditing();
        }
    }
}
