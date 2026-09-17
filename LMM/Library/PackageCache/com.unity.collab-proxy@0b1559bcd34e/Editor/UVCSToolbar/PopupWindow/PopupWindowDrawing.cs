using System;

using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow
{
    using UI;

    internal static class PopupWindowDrawing
    {
        internal const int MENU_ITEM_HEIGHT = 26;
        internal const int DELIMITER_HEIGHT = 1;
        internal const int PROGRESS_BAR_HEIGHT = 3;

        internal static bool DrawMenuItem(
            string text,
            Texture icon,
            string shortcut,
            out Rect menuItemRect)
        {
            GUIStyle menuItemStyle = UnityStyles.EditorToolbar.Popup.Hover;

            menuItemRect = GUILayoutUtility.GetRect(
                new GUIContent(text),
                menuItemStyle,
                GUILayout.Height(MENU_ITEM_HEIGHT));

            bool isClicked = GUI.Button(menuItemRect, GUIContent.none, menuItemStyle);
            bool isHovered = menuItemRect.Contains(Event.current.mousePosition);

            GUIStyle labelStyle = isHovered ?
                UnityStyles.EditorToolbar.Popup.LabelHover :
                UnityStyles.EditorToolbar.Popup.Label;

            GUIStyle shortcutStyle = isHovered
                ? UnityStyles.EditorToolbar.Popup.ShortcutHover
                : UnityStyles.EditorToolbar.Popup.Shortcut;

            float padding = labelStyle.padding.left;

            if (icon != null)
            {
                Rect iconRect = new Rect(
                    menuItemRect.x + padding,
                    menuItemRect.y + (MENU_ITEM_HEIGHT - ICON_SIZE) / 2,
                    ICON_SIZE,
                    ICON_SIZE);

                float textOffset = icon != null ? ICON_SIZE + labelStyle.margin.left : 0f;
                Rect textRect = new Rect(
                    menuItemRect.x + textOffset,
                    menuItemRect.y,
                    menuItemRect.width - textOffset,
                    menuItemRect.height);

                GUI.DrawTexture(iconRect, icon);
                GUI.Label(textRect, text, labelStyle);
            }
            else
            {
                GUI.Label(menuItemRect, text, labelStyle);
            }

            if (!string.IsNullOrEmpty(shortcut))
                GUI.Label(menuItemRect, shortcut, shortcutStyle);

            return isClicked;
        }

        internal static void DrawDelimiterRect(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color color1 = GUI.color;
            GUI.color *= color;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = color1;
        }

        internal static int RepaintWhenHoveredMenuItemChanged(
            Action repaint,
            int lastHoveredIndex,
            params Rect[] rects)
        {
            Vector2 mousePos = Event.current.mousePosition;
            int hoveredIndex = -1;

            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].Contains(mousePos))
                {
                    hoveredIndex = i;
                    break;
                }
            }

            if (hoveredIndex != lastHoveredIndex)
            {
                repaint();
            }

            return hoveredIndex;
        }

        internal static void DrawPopupBorder(Rect rect)
        {
            GUI.Label(rect, GUIContent.none, "grey_border");
        }

        const int ICON_SIZE = 16;
    }
}
