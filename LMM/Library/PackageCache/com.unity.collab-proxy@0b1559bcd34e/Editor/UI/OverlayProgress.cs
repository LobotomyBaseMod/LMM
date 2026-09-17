using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class OverlayProgress
    {
        internal static Rect CaptureViewRectangle()
        {
            // capture the initial x,y position of the view
            return GUILayoutUtility.GetRect(
                0,
                0);
        }

        internal static void DoOverlayProgress(
            Rect viewRect,
            float progressPercent,
            string progressMessage)
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();

            // capture the total width and height of the view
            // based on the last rect
            Rect overlayRect = new Rect(
                viewRect.x,
                viewRect.y,
                lastRect.xMax - viewRect.x,
                lastRect.yMax - viewRect.y - 1);

            DrawDebugRect(overlayRect, Color.green, 2);

            EditorGUI.DrawRect(overlayRect, UnityStyles.Colors.OverlayProgressBackgroundColor);

            const int progressBarHeight = 20;
            const int padding = 20;

            float progressBarWidth = Mathf.Clamp(
                overlayRect.width - (padding * 2),
                MIN_PROGRESS_BAR_WIDTH,
                MAX_PROGRESS_BAR_WIDTH);

            Rect progressRect = new Rect(
                overlayRect.x + ((overlayRect.width - progressBarWidth) / 2),
                overlayRect.y + ((overlayRect.height - progressBarHeight) / 2),
                progressBarWidth,
                progressBarHeight);

            EditorGUI.ProgressBar(
                progressRect,
                progressPercent,
                progressMessage);
        }

        static void DrawDebugRect(Rect rect, Color color, int thickness)
        {
            // keep this code commented for future debug purposes

            /*EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);*/
        }

        const float MAX_PROGRESS_BAR_WIDTH = 290;
        const float MIN_PROGRESS_BAR_WIDTH = 50;
    }
}
