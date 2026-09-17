using Codice.CM.Client.Differences.Graphic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Plugins.PlasticSCM.Editor.Diff.Text
{
    internal class ContentControl : VisualElement
    {
        internal void ShowData(
            EntryData entryData,
            string message,
            string pathForEdition)
        {
            Clear();

            Add(BuildTitle("Text Content Control"));
            Add(BuildInfoLabel("Message", message));
            Add(BuildInfoLabel("Path for edition", pathForEdition));
            Add(BuildInfoLabel("File", entryData.File));
            Add(BuildInfoLabel("Entry is purged",
                entryData != null ? entryData.IsPurged.ToString() : "(null)"));
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
