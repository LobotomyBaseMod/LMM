using System.Diagnostics;
using System.IO;

using Unity.PlasticSCM.Editor.AssetUtils;

namespace Unity.PlasticSCM.Editor
{
    internal static class UnityPlasticDllVersion
    {
        internal static string GetFileVersion()
        {
            string unityPlasticDllFullPath = Path.Combine(
                AssetsPath.GetLibEditorFolderFullPath(), "unityplastic.dll");

            return FileVersionInfo.GetVersionInfo(unityPlasticDllFullPath).FileVersion;
        }
    }
}
