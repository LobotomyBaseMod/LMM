using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class EntryBuilder
    {
        internal static string CreateTextEntry(
            string label,
            string value,
            float width,
            float x)
        {
            return CreateTextEntry(
                label,
                value,
                null,
                width,
                x);
        }

        internal static string CreateTextEntry(
            string label,
            string value,
            string controlName,
            float width,
            float x)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                CreateLabel(label);

                GUILayout.FlexibleSpace();

                var rt = GUILayoutUtility.GetRect(
                    new GUIContent(value), UnityStyles.Dialog.EntryLabel);
                rt.width = width;
                rt.x = x;

                if (!string.IsNullOrEmpty(controlName))
                    GUI.SetNextControlName(controlName);

                return GUI.TextField(rt, value);
            }
        }

        internal static string CreatePasswordEntry(
            string label,
            string value,
            float width,
            float x)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                CreateLabel(label);

                GUILayout.FlexibleSpace();

                var rt = GUILayoutUtility.GetRect(
                    new GUIContent(value), UnityStyles.Dialog.EntryLabel);
                rt.width = width;
                rt.x = x;

                return GUI.PasswordField(rt, value, '*');
            }
        }

        internal static bool CreateToggleEntry(
            string label,
            bool value,
            float width,
            float x)
        {
            var rt = GUILayoutUtility.GetRect(
                new GUIContent(label), UnityStyles.Dialog.EntryLabel);
            rt.width = width;
            rt.x = x;

            return GUI.Toggle(rt, value, label);
        }

        internal static bool CreateToggleEntry(
            string label,
            bool value)
        {
            var rt = GUILayoutUtility.GetRect(
                new GUIContent(label), UnityStyles.Dialog.EntryLabel);

            return GUI.Toggle(rt, value, label);
        }

        internal static string CreateComboBoxEntry(
            string label,
            string value,
            List<string> dropDownOptions,
            GenericMenu.MenuFunction2 optionSelected,
            float width,
            float x)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                CreateLabel(label);

                GUILayout.FlexibleSpace();

                var rt = GUILayoutUtility.GetRect(
                    new GUIContent(value), UnityStyles.Dialog.EntryLabel);
                rt.width = width;
                rt.x = x;

                return DropDownTextField.DoDropDownTextField(
                    value,
                    label,
                    dropDownOptions,
                    optionSelected,
                    rt);
            }
        }

        static void CreateLabel(string labelText)
        {
            GUIContent labelContent = new GUIContent(labelText);
            GUIStyle labelStyle = UnityStyles.Dialog.EntryLabel;

            Rect rt = GUILayoutUtility.GetRect(labelContent, labelStyle);

            GUI.Label(rt, labelText, EditorStyles.label);
        }
    }
}
