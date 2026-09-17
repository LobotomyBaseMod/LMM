using UnityEngine;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow
{
    // Workaround for Unity 6.0-6.2 on Windows where clicking the window title or menus
    // while a popup is open causes a GUI rendering error.
    // Closes the popup when mouse goes above it (toward the risky window menu area).
    internal static class ToolbarMouseBoundary
    {
        internal static bool IsAboveToolbar(Rect popupRect, Vector2 mousePosition)
        {
            float topBoundary = popupRect.y - TOOLBAR_TOP_OFFSET;

            return mousePosition.y < topBoundary;
        }

        // Height of the toolbar button (20), his top margin (1) and the toolbar top padding (7),
        // also equivalent to button height (20) + ((toolbar height (36) - button height (20)) / 2).
        const float TOOLBAR_TOP_OFFSET = 28f;
    }
}
