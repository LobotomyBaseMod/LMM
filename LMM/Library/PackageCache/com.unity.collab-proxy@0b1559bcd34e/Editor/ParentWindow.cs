using UnityEditor;

using Unity.PlasticSCM.Editor.CloudDrive;

namespace Unity.PlasticSCM.Editor
{
    internal class ParentWindow
    {
        internal static EditorWindow Get()
        {
            if (EditorWindow.HasOpenInstances<UVCSWindow>())
                return EditorWindow.GetWindow<UVCSWindow>(false, null, false);

            if (EditorWindow.HasOpenInstances<CloudDriveWindow>())
                return EditorWindow.GetWindow<CloudDriveWindow>(false, null, false);

            return EditorWindow.focusedWindow;
        }
    }
}
