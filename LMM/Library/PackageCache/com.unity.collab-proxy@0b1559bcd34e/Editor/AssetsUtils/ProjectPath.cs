using System.IO;

namespace Unity.PlasticSCM.Editor.AssetUtils
{
    internal static class ProjectPath
    {
        internal static string Get()
        {
            return Path.GetDirectoryName(AssetsPath.GetFullPath.ForPath(
                ApplicationDataPath.Get()));
        }
    }
}
