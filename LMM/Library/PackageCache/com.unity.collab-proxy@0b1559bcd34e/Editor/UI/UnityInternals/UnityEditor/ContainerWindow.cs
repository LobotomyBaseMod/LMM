namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal class ContainerWindow
    {
        internal object InternalObject { get; }

        internal ContainerWindow(object containerWindow)
        {
            InternalObject = containerWindow;
        }
    }
}
