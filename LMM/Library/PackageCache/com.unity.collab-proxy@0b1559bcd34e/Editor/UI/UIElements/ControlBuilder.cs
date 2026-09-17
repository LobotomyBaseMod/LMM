using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.UI.UIElements
{
    internal static class Styles
    {
        internal static class Toolbar
        {
            internal static void ApplyImageButtonStyle(ToolbarButton button)
            {
                button.style.width = ImageButtonWidth;
                button.style.justifyContent = Justify.Center;
                button.style.alignItems = Align.Center;
                button.style.paddingBottom = 1;
                button.focusable = false;
            }

            static StyleLength ImageButtonWidth = 28;
        }

        internal static class Button
        {
            internal static void ApplyImageButtonStyle(UnityEngine.UIElements.Button button)
            {
                button.style.justifyContent = Justify.Center;
                button.style.alignItems = Align.Center;
            }
        }

        internal static class ButtonGroup
        {
            internal static void ApplyTopButtonStyle(UnityEngine.UIElements.Button button)
            {
                button.style.marginBottom = 0;
                button.style.borderBottomLeftRadius = 0;
                button.style.borderBottomRightRadius = 0;
            }

            internal static void ApplyBottomButtonStyle(UnityEngine.UIElements.Button button)
            {
                button.style.marginTop = 0;
                button.style.borderTopWidth = 0;
                button.style.borderTopLeftRadius = 0;
                button.style.borderTopRightRadius = 0;
            }
        }
    }

    internal static class ControlBuilder
    {
        internal static class Toolbar
        {
            internal static UnityEditor.UIElements.Toolbar Create()
            {
                return new UnityEditor.UIElements.Toolbar();
            }

            internal static ToolbarSearchField CreateSearchField(string tooltip = null)
            {
                ToolbarSearchField searchField = new ToolbarSearchField();
                searchField.tooltip = tooltip;
                searchField.style.width = 200;
                return searchField;
            }

            internal static ToolbarButton CreateImageButtonLeft(
                Texture image,
                string tooltip,
                Action onClick)
            {
                ToolbarButton button = CreateImageButton(
                    image,
                    tooltip,
                    onClick);

                // apply a negative margin to blend with next button
                button.style.marginRight = -1;
                return button;
            }

            internal static ToolbarButton CreateImageButton(
                Texture image,
                string tooltip,
                Action onClick)
            {
                var button = new ToolbarButton(onClick)
                {
                    tooltip = tooltip,
                };

                var icon = new Image
                {
                    image = image,
                };

                Styles.Toolbar.ApplyImageButtonStyle(button);

                button.Add(icon);

                return button;
            }
        }

        internal static class Button
        {
            internal static UnityEngine.UIElements.Button CreateImageButton(
                Texture image,
                string tooltip,
                System.Action onClick)
            {
                UnityEngine.UIElements.Button button = new UnityEngine.UIElements.Button(onClick)
                {
                    tooltip = tooltip,
                    focusable = false
                };

                Image icon = new Image()
                {
                    image = image,
                };

                Styles.Button.ApplyImageButtonStyle(button);

                button.Add(icon);

                return button;
            }

            public static UnityEngine.UIElements.Button CreateButton(string text, Action handler)
            {
                UnityEngine.UIElements.Button result = new UnityEngine.UIElements.Button(handler);
                result.text = text;
                return result;
            }

            public static UnityEngine.UIElements.Button CreateDropDownButton(string text, Action handler)
            {
                UnityEngine.UIElements.Button button = new UnityEngine.UIElements.Button(handler);
                button.style.flexDirection = FlexDirection.Row;

                TextElement textElement = new TextElement();
                textElement.text = text;

                VisualElement dropDownIcon = new VisualElement();
                dropDownIcon.AddToClassList("unity-base-popup-field__arrow");
                dropDownIcon.style.marginLeft = 4;

                button.Add(textElement);
                button.Add(dropDownIcon);

                return button;
            }
        }

        internal static class ButtonGroup
        {
            internal static UnityEngine.UIElements.Button CreateImageTopButton(
                Texture image,
                string tooltip,
                System.Action onClick)
            {
                UnityEngine.UIElements.Button button =
                    Button.CreateImageButton(image, tooltip, onClick);

                Styles.ButtonGroup.ApplyTopButtonStyle(button);

                return button;
            }

            internal static UnityEngine.UIElements.Button CreateImageBottomButton(
                Texture image,
                string tooltip,
                System.Action onClick)
            {
                UnityEngine.UIElements.Button button =
                    Button.CreateImageButton(image, tooltip, onClick);

                Styles.ButtonGroup.ApplyBottomButtonStyle(button);

                return button;
            }
        }
    }
}
