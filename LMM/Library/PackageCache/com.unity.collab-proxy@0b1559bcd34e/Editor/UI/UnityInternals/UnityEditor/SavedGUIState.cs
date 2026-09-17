namespace Unity.PlasticSCM.Editor.UnityInternals.UnityEditor
{
    internal struct SavedGUIState
    {
        internal object InternalObject { get; }

        internal SavedGUIState(object savedGuiState)
        {
            InternalObject = savedGuiState;
        }

        internal void ApplyAndForget()
        {
            ApplyAndForgetInternal(this);
        }

        internal delegate SavedGUIState CreateDelegate();
        internal static CreateDelegate Create { get; set; }

        internal delegate void ApplyAndForgetDelegate(SavedGUIState savedGUIState);
        internal static ApplyAndForgetDelegate ApplyAndForgetInternal { get; set; }
    }
}
