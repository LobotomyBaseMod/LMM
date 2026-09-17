using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    internal static class SpritePivotUtility
    {
        private const float kPivotFieldPrecision = 0.0001f;

        internal static Vector2 ConvertFromNormalizedToRectSpace(Vector2 normalizedPos, Rect rect)
        {
            Vector2 rectPos = new Vector2(rect.width * normalizedPos.x, rect.height * normalizedPos.y);

            // This is to combat the lack of precision formating on the UI controls.
            rectPos.x = Mathf.Round(rectPos.x / kPivotFieldPrecision) * kPivotFieldPrecision;
            rectPos.y = Mathf.Round(rectPos.y / kPivotFieldPrecision) * kPivotFieldPrecision;

            return rectPos;
        }

        internal static Vector2 ConvertFromRectToNormalizedSpace(Vector2 rectPos, Rect rect)
        {
            return new Vector2(rectPos.x / rect.width, rectPos.y / rect.height);
        }
    }
}
