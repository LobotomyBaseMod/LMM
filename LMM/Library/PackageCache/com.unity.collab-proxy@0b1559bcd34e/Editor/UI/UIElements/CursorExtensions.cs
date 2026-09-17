using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.UI.UIElements
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

        internal static void SetTextCursor(this TextField textField)
        {
            var textInput = textField.Q("unity-text-input");
            if (textInput == null)
                return;

            textInput.SetMouseCursor(MouseCursor.Text);

            var textElement = textInput.Q<TextElement>();
            if (textElement == null)
                return;

            textElement.SetMouseCursor(MouseCursor.Text);
        }
    }
}
