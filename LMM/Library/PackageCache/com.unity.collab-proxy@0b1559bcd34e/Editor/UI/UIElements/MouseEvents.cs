using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.UI.UIElements
{
    internal static class MouseEvents
    {
        internal static bool IsRightButtonPressed(PointerDownEvent evt)
        {
            return IsRightButtonPressed(evt.button);
        }

        internal static bool IsRightButtonPressed(int button)
        {
            return button == UnityConstants.RIGHT_MOUSE_BUTTON;
        }
    }
}
