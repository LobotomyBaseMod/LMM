using System;
using System.Linq;
using System.Reflection;

using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.UI;

using PackageManager = UnityEditor.PackageManager;

namespace Unity.PlasticSCM.Editor
{
    internal static class PackageInfo
    {
        internal const string NAME = "com.unity.collab-proxy";

        internal static VersionData Data = VersionData.Default;

        internal class VersionData
        {
            internal readonly string Version;

            internal string LatestVersion { get; private set; }

            internal static VersionData Default = new VersionData("0.0.0", "0.0.0");

            internal VersionData(string version, string latestVersion)
            {
                Version = version;
                LatestVersion = latestVersion;
            }

            internal void SetLatestVersion(string latestVersion)
            {
                LatestVersion = latestVersion;
            }

            internal bool IsLatestVersion()
            {
                if (string.IsNullOrEmpty(LatestVersion) ||
                    LatestVersion == Default.LatestVersion)
                    return true;

                return Version == LatestVersion;
            }
        }

        internal static void Initialize()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            PackageManager.PackageInfo packageInfo = FindPackageInfo(assembly);

            if (packageInfo == null)
                return;

            Data = new VersionData(
                packageInfo.version,
                VersionData.Default.LatestVersion);

            SearchPackageAsync(
                Data,
                PackageManager.Client.Search(packageInfo.name),
                OnSearchPackageCompleted);
        }

        static PackageManager.PackageInfo FindPackageInfo(Assembly assembly)
        {
            PackageManager.PackageInfo packageInfo =
                PackageManager.PackageInfo.FindForAssembly(assembly);

            if (packageInfo == null)
            {
                mLog.DebugFormat("No package found for {0} (dev env plugin)", assembly);
                return null;
            }

            if (packageInfo.name != NAME)
            {
                mLog.ErrorFormat("Package {0} doesn't match with {1}", packageInfo.name, NAME);
                return null;
            }

            mLog.DebugFormat("Package {0} version: {1}", NAME, packageInfo.version);
            return packageInfo;
        }

        static void SearchPackageAsync(
            VersionData versionData,
            PackageManager.Requests.SearchRequest searchRequest,
            Action<VersionData, PackageManager.PackageInfo> onCompleted)
        {
            EditorDispatcher.Dispatch(delegate
            {
                if (!searchRequest.IsCompleted)
                {
                    SearchPackageAsync(versionData, searchRequest, onCompleted);
                    return;
                }

                if (searchRequest.Status != PackageManager.StatusCode.Success)
                {
                    mLog.ErrorFormat("Search failed: {0}", searchRequest.Error.message);
                    return;
                }

                // As we are requesting the data of a single package,
                // we should only ever get a single result, but the API returns a list
                PackageManager.PackageInfo packageInfo = searchRequest.Result.FirstOrDefault();
                if (packageInfo == null)
                {
                    mLog.ErrorFormat("Search returned no results for package {0}", NAME);
                    return;
                }

                if (packageInfo.name != NAME)
                {
                    mLog.ErrorFormat("Package {0} doesn't match with {1}", packageInfo.name, NAME);
                    return;
                }

                onCompleted(versionData, packageInfo);
            });
        }

        static void OnSearchPackageCompleted(
            VersionData versionData,
            PackageManager.PackageInfo packageInfo)
        {
            versionData.SetLatestVersion(packageInfo.versions.latest);
        }

        static readonly ILog mLog = PlasticApp.GetLogger("PackageInfo");
    }
}
