using UnityEngine;
using UnityEngine.UIElements;

namespace Plugins.PlasticSCM.Editor.Diff
{
    internal class DiffMessagePanel : VisualElement
    {
        internal void ShowMessage(string message)
        {
            Clear();

            Label title = new Label("Diff Message Panel");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8;
            Add(title);

            Add(new Label($"Message: {message ?? "(null)"}"));
        }
    }
}
