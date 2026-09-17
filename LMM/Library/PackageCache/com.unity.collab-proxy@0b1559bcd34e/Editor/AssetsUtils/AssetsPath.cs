using System.IO;
using System.Reflection;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Utils;
using PlasticGui;

namespace Unity.PlasticSCM.Editor.AssetUtils
{
    internal static class AssetsPath
    {
        internal static class GetFullPath
        {
            internal static string ForPath(string path)
            {
#if UNITY_6000_5_OR_NEWER
                string absolutePath = UnityEditor.FileUtil.PathToAbsolutePath(path);

                // While the Editor is moving away from MonoPathRemapper usage, our package is still not ready
                // to support unified path separators. In order to avoid errors, we need to rectify the path
                // returned by FileUtil.PathToAbsolutePath on Windows.
                return PlatformIdentifier.IsWindows()
                    ? absolutePath.Replace("/", "\\")
                    : absolutePath;
#else
                return Path.GetFullPath(path);
#endif
            }

            internal static string ForObject(Object obj)
            {
                string relativePath = AssetDatabase.GetAssetPath(obj);

                if (string.IsNullOrEmpty(relativePath))
                    return null;

                return ForPath(relativePath);
            }

            internal static string ForGuid(string guid)
            {
                string relativePath = GetAssetPath(guid);

                if (string.IsNullOrEmpty(relativePath))
                    return null;

                return ForPath(relativePath);
            }
        }

        internal static class GetFullPathUnderWorkspace
        {
            internal static string ForAsset(
                string wkPath,
                string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath))
                    return null;

                string fullPath = GetFullPath.ForPath(assetPath);

                if (!PathHelper.IsContainedOn(fullPath, wkPath))
                    return null;

                if (!fullPath.StartsWith("/"))
                    fullPath = fullPath.Substring(0, 1).ToLowerInvariant() + fullPath.Substring(1);
                return fullPath.TrimEnd('/', '\\');
            }

            internal static string ForGuid(
                string wkPath,
                string guid)
            {
                return ForAsset(wkPath, GetAssetPath(guid));
            }
        }

        internal static string GetLayoutsFolderRelativePath()
        {
            return string.Concat(mAssetsFolderRelativePath, "/Layouts");
        }

        internal static string GetStylesFolderRelativePath()
        {
            return string.Concat(mAssetsFolderRelativePath, "/Styles");
        }

        internal static string GetImagesFolderRelativePath()
        {
            return string.Concat(mAssetsFolderRelativePath, "/Images");
        }

        internal static string GetFontsFolderRelativePath()
        {
            return string.Concat(mAssetsFolderRelativePath, "/Fonts");
        }

        internal static string GetLibEditorFolderFullPath()
        {
            return mLibEditorFolderFullPath;
        }

        internal static string GetRelativePath(string fullPath)
        {
            return PathHelper.GetRelativePath(mProjectFullPath, fullPath).Substring(1);
        }

        internal static bool IsPackagesRootElement(string path)
        {
            return PathHelper.IsSamePath(mProjectPackagesFullPath, PathHelper.GetParentPath(path));
        }

        internal static bool IsScript(string path)
        {
            return Path.GetExtension(path).Equals(".cs");
        }

        internal static bool IsRunningAsUPMPackage()
        {
            return IsRunningAsUPMPackage(mLibEditorFolderFullPath);
        }

        static string GetAssetPath(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            return AssetDatabase.GUIDToAssetPath(guid);
        }

        static bool IsRunningAsUPMPackage(string libEditorFolderFullPath)
        {
            // The Lib/Editor path containing the "unityplastic.dll" ends with
            // - "Packages/com.unity.collab-proxy@xxx/Lib/Editor" when running as an UPM package
            // - "Assets/Plugins/PlasticSCM/Lib/Editor" in the development environment
            return libEditorFolderFullPath.Contains(PackageInfo.NAME);
        }

        static AssetsPath()
        {
            mLibEditorFolderFullPath = GetFullPath.ForPath(
                AssemblyLocation.GetAssemblyDirectory(
                    Assembly.GetAssembly(typeof(PlasticLocalization))));

            mAssetsFolderRelativePath = IsRunningAsUPMPackage(mLibEditorFolderFullPath) ?
                "Packages/com.unity.collab-proxy/Editor/Assets" :
                "Assets/Plugins/PlasticSCM/Editor/Assets";
        }

        static readonly string mProjectFullPath = ProjectPath.Get();
        static readonly string mProjectPackagesFullPath = Path.Combine(mProjectFullPath, "Packages");

        static string mLibEditorFolderFullPath;
        static string mAssetsFolderRelativePath;
    }
}
