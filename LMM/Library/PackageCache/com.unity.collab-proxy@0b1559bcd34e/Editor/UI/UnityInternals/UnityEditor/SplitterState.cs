namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal class SplitterState
    {
        internal object InternalObject;

        internal float[] relativeSizes => GetRelativeSizes(this);

        internal SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            InternalObject = Constructor(relativeSizes, minSizes, maxSizes);
        }

        internal delegate object ConstructorDelegate(float[] relativeSizes, int[] minSizes, int[] maxSizes);
        internal static ConstructorDelegate Constructor { get; set; }

        internal delegate float[] GetRelativeSizesDelegate(SplitterState splitterState);
        internal static GetRelativeSizesDelegate GetRelativeSizes { get; set; }
    }
}
