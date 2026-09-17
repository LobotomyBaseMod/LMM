using UnityEditor;

using Plugins.PlasticSCM.Editor.Diff;
using Unity.PlasticSCM.Editor.CloudDrive;
using Unity.PlasticSCM.Editor.Views.BranchExplorer;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Options;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class GetWindowIfOpened
    {
        internal static UVCSWindow UVCS()
        {
            if (!EditorWindow.HasOpenInstances<UVCSWindow>())
                return null;

            return EditorWindow.GetWindow<UVCSWindow>(null, false);
        }

        internal static BranchExplorerWindow BranchExplorer()
        {
            if (!EditorWindow.HasOpenInstances<BranchExplorerWindow>())
                return null;

            return EditorWindow.GetWindow<BranchExplorerWindow>(null, false);
        }

        internal static BranchExplorerOptionsWindow BranchExplorerOptions()
        {
            if (!EditorWindow.HasOpenInstances<BranchExplorerOptionsWindow>())
                return null;

            return EditorWindow.GetWindow<BranchExplorerOptionsWindow>(null, false);
        }

        internal static CloudDriveWindow CloudDrive()
        {
            if (!EditorWindow.HasOpenInstances<CloudDriveWindow>())
                return null;

            return EditorWindow.GetWindow<CloudDriveWindow>(null, false);
        }

        internal static DiffWindow Diff()
        {
            if (!EditorWindow.HasOpenInstances<DiffWindow>())
                return null;

            return EditorWindow.GetWindow<DiffWindow>(null, false);
        }
    }
}
