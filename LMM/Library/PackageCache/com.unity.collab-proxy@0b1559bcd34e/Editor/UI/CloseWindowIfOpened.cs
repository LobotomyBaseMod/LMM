using Unity.PlasticSCM.Editor.CloudDrive;
using Unity.PlasticSCM.Editor.Views.BranchExplorer;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Options;
using UnityEditor;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class CloseWindowIfOpened
    {
        internal static void UVCS()
        {
            if (!EditorWindow.HasOpenInstances<UVCSWindow>())
                return;

            UVCSWindow window = EditorWindow.
                GetWindow<UVCSWindow>(null, false);

            window.Close();
        }

        internal static void BranchExplorer()
        {
            if (!EditorWindow.HasOpenInstances<BranchExplorerWindow>())
                return;

            BranchExplorerWindow window = EditorWindow.
                GetWindow<BranchExplorerWindow>(null, false);

            window.Close();
        }

        internal static void BranchExplorerOptions()
        {
            if (!EditorWindow.HasOpenInstances<BranchExplorerOptionsWindow>())
                return;

            BranchExplorerOptionsWindow window = EditorWindow.
                GetWindow<BranchExplorerOptionsWindow>(null, false);

            window.Close();
        }

        internal static void CloudDrive()
        {
            if (!EditorWindow.HasOpenInstances<CloudDriveWindow>())
                return;

            CloudDriveWindow window = EditorWindow.
                GetWindow<CloudDriveWindow>(null, false);

            window.Close();
        }
    }
}
