using Codice.CM.Client.Differences.Graphic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Plugins.PlasticSCM.Editor.Diff.Purged
{
    internal class PurgedRevisionControl : VisualElement
    {
        internal void ShowData(DiffViewerData data)
        {
            Clear();

            Add(BuildTitle("Purged Revision Control"));
            Add(BuildInfoLabel("Message", data.Message));
            Add(BuildInfoLabel("Path for edition", data.PathForEdition));
            Add(BuildInfoLabel("Extension", data.Extension));
            Add(BuildInfoLabel("Left file", data.Left?.File));
            Add(BuildInfoLabel("Right file", data.Right?.File));
            Add(BuildInfoLabel("Left is purged",
                data.Left != null ? data.Left.IsPurged.ToString() : "(null)"));
            Add(BuildInfoLabel("Right is purged",
                data.Right != null ? data.Right.IsPurged.ToString() : "(null)"));
        }

        static Label BuildTitle(string text)
        {
            Label label = new Label(text);
            label.style.fontSize = 16;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 8;
            return label;
        }

        static Label BuildInfoLabel(string key, string value)
        {
            return new Label($"{key}: {value ?? "(null)"}");
        }
    }
}
