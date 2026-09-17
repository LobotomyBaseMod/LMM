using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Utils
{
    internal static class CursorExtensions
    {
        internal static void SetMouseCursor(this VisualElement visualElement, MouseCursor mouseCursor)
        {
            var cursor = new Cursor();
            var prop = typeof(Cursor).GetProperty("defaultCursorId",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (prop != null)
            {
                object boxed = cursor;
                prop.SetValue(boxed, (int)mouseCursor);
                cursor = (Cursor)boxed;
            }

            visualElement.style.cursor = new StyleCursor(cursor);
        }
    }
}