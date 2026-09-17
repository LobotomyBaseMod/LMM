using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI.Progress
{
    internal static class LoadingSpinner
    {
        internal static void OnGUI(
            float progressPercent,
            string progressTooltip = null)
        {
            Matrix4x4 oldMatrix = GUI.matrix;

            Rect layoutRect = GUILayoutUtility.GetRect(
                SPINNER_SIZE, SPINNER_SIZE);

            Rect position = new Rect(
                layoutRect.x + (layoutRect.width - SPINNER_SIZE) / 2,
                layoutRect.y + (layoutRect.height - SPINNER_SIZE) / 2,
                SPINNER_SIZE, SPINNER_SIZE);

            Vector2 pivot = new Vector2(
                position.x + SPINNER_SIZE / 2f,
                position.y + SPINNER_SIZE / 2f);

            int rotation = (int)(360 * progressPercent);
            GUIUtility.RotateAroundPivot(rotation, pivot);

            GUI.Label(
                position,
                new GUIContent(
                    Images.GetImage(Images.Name.Loading),
                    progressTooltip),
                UnityStyles.StatusBar.Icon);

            GUI.matrix = oldMatrix;
        }

        const int SPINNER_SIZE = 16;
    }
}
