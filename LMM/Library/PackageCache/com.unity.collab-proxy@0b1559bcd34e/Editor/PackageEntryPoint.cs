using System.Threading;

using UnityEditor;

using Unity.PlasticSCM.Editor.CloudDrive;
using Unity.PlasticSCM.Editor.Hub;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    [InitializeOnLoad]
    internal static class PackageEntryPoint
    {
        static PackageEntryPoint()
        {
            EditorDispatcher.InitializeMainThreadIdAndContext(
                Thread.CurrentThread.ManagedThreadId,
                SynchronizationContext.Current);

            ProcessHubCommand.Initialize();

            UVCSPlugin.InitializeIfNeeded();

            CloudDrivePlugin.InitializeIfNeeded();
        }
    }
}
