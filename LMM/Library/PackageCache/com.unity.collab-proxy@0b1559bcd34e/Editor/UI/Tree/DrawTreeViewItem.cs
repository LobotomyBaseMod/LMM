using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
#endif

namespace Unity.PlasticSCM.Editor.UI.Tree
{
    internal static class DrawTreeViewItem
    {
        internal enum TextTrimming
        {
            None,
            Path,
        }
        internal static void InitializeStyles()
        {
            if (EditorStyles.label == null)
                return;

            TreeView.DefaultStyles.label = UnityStyles.Tree.Label;
            TreeView.DefaultStyles.boldLabel = UnityStyles.Tree.BoldLabel;
        }

        internal static void ForIndentedItem(
            Rect rowRect,
            int depth,
            string label,
            string infoLabel,
            bool isSelected,
            bool isFocused)
        {
            float indent = GetIndent(depth);

            rowRect.x += indent;
            rowRect.width -= indent;

            //add a little indentation
            rowRect.x += 5;
            rowRect.width -= 5;

            TreeView.DefaultGUI.Label(rowRect, label, isSelected, isFocused);

            if (!string.IsNullOrEmpty(infoLabel))
                DrawInfolabel(rowRect, label, infoLabel);
        }

        internal static void ForIndentedItemWithIcon(
            Rect rowRect,
            float rowHeight,
            int depth,
            string label,
            string infoLabel,
            Texture icon,
            bool isSelected,
            bool isFocused)
        {
            float indent = GetIndent(depth);

            rowRect.x += indent;
            rowRect.width -= indent;

            rowRect = DrawIconLeft(
                rowRect, rowHeight, icon, null, null);

            TreeView.DefaultGUI.Label(rowRect, label, isSelected, isFocused);

            if (!string.IsNullOrEmpty(infoLabel))
                DrawInfolabel(rowRect, label, infoLabel);
        }

        internal static bool ForCheckableIndentedItem(
            Rect rowRect,
            int depth,
            string label,
            string infoLabel,
            bool isSelected,
            bool isFocused,
            bool wasChecked,
            bool hadCheckedChildren,
            bool hadPartiallyCheckedChildren)
        {
            float indent = GetIndent(depth);

            rowRect.x += indent;
            rowRect.width -= indent;

            Rect checkRect = GetCheckboxRect(rowRect);

            if (!wasChecked && (hadCheckedChildren || hadPartiallyCheckedChildren))
                EditorGUI.showMixedValue = true;

            bool isChecked = EditorGUI.Toggle(checkRect, wasChecked);
            EditorGUI.showMixedValue = false;

            rowRect.x = checkRect.xMax - 4;
            rowRect.width -= checkRect.width;

            //add a little indentation
            rowRect.x += 5;
            rowRect.width -= 5;

            TreeView.DefaultGUI.Label(rowRect, label, isSelected, isFocused);

            if (!string.IsNullOrEmpty(infoLabel))
                DrawInfolabel(rowRect, label, infoLabel);

            return isChecked;
        }

        internal static void ForItemCell(
            Rect rect,
            float rowHeight,
            int depth,
            Texture icon,
            string iconTooltip,
            Texture overlayIcon,
            string label,
            bool isSelected,
            bool isFocused,
            bool isBoldText,
            bool isSecondaryLabel,
            TextTrimming textTrimming = TextTrimming.None)
        {
            float indent = GetIndent(depth);

            rect.x += indent;
            rect.width -= indent;

            rect = DrawIconLeft(
               rect, rowHeight, icon, iconTooltip, overlayIcon);

            if (isSecondaryLabel)
            {
                ForSecondaryLabel(rect, label, isSelected, isFocused, isBoldText, textTrimming);
                return;
            }

            ForLabel(rect, label, isSelected, isFocused, isBoldText, textTrimming);
        }

        internal static bool ForCheckableItemCell(
            Rect rect,
            float rowHeight,
            int depth,
            Texture icon,
            string iconTooltip,
            Texture overlayIcon,
            string label,
            bool isSelected,
            bool isFocused,
            bool isHighlighted,
            bool wasChecked,
            DrawTreeViewItem.TextTrimming textTrimming = TextTrimming.None)
        {
            float indent = GetIndent(depth);

            rect.x += indent;
            rect.width -= indent;

            Rect checkRect = GetCheckboxRect(rect);

            bool isChecked = EditorGUI.Toggle(checkRect, wasChecked);

            rect.x = checkRect.xMax;
            rect.width -= checkRect.width;

            rect = DrawIconLeft(
                rect, rowHeight, icon, iconTooltip, overlayIcon);

            GUIStyle style = isHighlighted ?
                UnityStyles.Tree.BoldLabel :
                UnityStyles.Tree.Label;

            if (Event.current.type != UnityEngine.EventType.Repaint)
                return isChecked;

            GUIContent content = GetGUIContent(label, style, rect, textTrimming);

            DrawLabel(
                content,
                style,
                rect,
                false,
                true,
                isSelected,
                isFocused);

            return isChecked;
        }

        internal static Rect DrawIconLeft(
            Rect rect,
            float rowHeight,
            Texture icon,
            string iconTooltip,
            Texture overlayIcon)
        {
            if (icon == null)
                return rect;

            float iconWidth = UnityConstants.TREEVIEW_ICON_SIZE * ((float)icon.width / icon.height);;

            Rect iconRect = new Rect(rect.x, rect.y, iconWidth, rowHeight);

            EditorGUI.LabelField(iconRect, new GUIContent(icon, iconTooltip), UnityStyles.Tree.IconStyle);

            if (overlayIcon != null)
            {
                Rect overlayIconRect = GetOverlayRect.ForPendingChanges(iconRect);

                GUI.DrawTexture(
                    overlayIconRect, overlayIcon,
                    ScaleMode.ScaleToFit);
            }

            rect.x += iconRect.width;
            rect.width -= iconRect.width;

            return rect;
        }

        static void DrawInfolabel(
            Rect rect,
            string label,
            string infoLabel)
        {
            Vector2 labelSize = ((GUIStyle)UnityStyles.Tree.Label)
                .CalcSize(new GUIContent(label));

            rect.x += labelSize.x;

            GUI.Label(rect, infoLabel, UnityStyles.Tree.InfoLabel);
        }

        static Rect GetCheckboxRect(Rect rect)
        {
            return new Rect(
                rect.x,
                rect.y + UnityConstants.TREEVIEW_CHECKBOX_Y_OFFSET,
                UnityConstants.TREEVIEW_CHECKBOX_SIZE,
                rect.height);
        }

        static float GetIndent(int depth)
        {
            return 16 * (depth + 1);
        }

        internal static void ForSecondaryLabelRightAligned(
            Rect rect,
            string label,
            bool isSelected,
            bool isFocused,
            bool isBoldText,
            TextTrimming textTrimming = TextTrimming.None)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            GUIStyle style = isBoldText ?
                UnityStyles.Tree.SecondaryLabelBoldRightAligned :
                UnityStyles.Tree.SecondaryLabelRightAligned;
            GUIContent content = GetGUIContent(label, style, rect, textTrimming);

            DrawLabel(
                content,
                style,
                rect,
                false,
                true,
                isSelected,
                isFocused);
        }

        internal static void ForSecondaryLabel(
            Rect rect,
            string label,
            bool isSelected,
            bool isFocused,
            bool isBoldText,
            TextTrimming textTrimming = TextTrimming.None)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            GUIStyle style = isBoldText ?
                UnityStyles.Tree.SecondaryBoldLabel :
                UnityStyles.Tree.SecondaryLabel;

            if (isBoldText)
                style.normal.textColor = Color.red;

            GUIContent content = GetGUIContent(label, style, rect, textTrimming);

            DrawLabel(
                content,
                style,
                rect,
                false,
                true,
                isSelected,
                isFocused);
        }

        internal static void ForLabel(
            Rect rect,
            GUIContent content,
            bool isSelected,
            bool isFocused,
            bool isBoldText)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            GUIStyle style = isBoldText ? UnityStyles.Tree.BoldLabel: UnityStyles.Tree.Label;

            DrawLabel(
                content,
                style,
                rect,
                false,
                true,
                isSelected,
                isFocused);
        }

        internal static void ForLabel(
            Rect rect,
            string label,
            bool isSelected,
            bool isFocused,
            bool isBoldText,
            TextTrimming textTrimming = TextTrimming.None)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            label = TruncateLabelIfNeeded(label);

            GUIStyle style = isBoldText ? UnityStyles.Tree.BoldLabel: UnityStyles.Tree.Label;
            GUIContent content = GetGUIContent(label, style, rect, textTrimming);

            DrawLabel(
                content,
                style,
                rect,
                false,
                true,
                isSelected,
                isFocused);
        }

        static void DrawLabel(
            GUIContent content,
            GUIStyle style,
            Rect rect,
            bool isHover,
            bool isActive,
            bool isSelected,
            bool isFocused)
        {
            style.Draw(
                rect, content, isHover, isActive, isSelected, isFocused);
        }

        static GUIContent GetGUIContent(string label, GUIStyle style, Rect rect, TextTrimming textTrimming)
        {
            if (textTrimming == TextTrimming.Path)
            {
                string truncatedLabel = PathTrimming.TruncatePath(
                    label,
                    rect.width,
                    CalcTextSize.FromStyle(style),
                    out var wasTrimmed);

                return new GUIContent(truncatedLabel, wasTrimmed ? label : null);
            }

            return new GUIContent(label);
        }

        static string TruncateLabelIfNeeded(string label)
        {
            if (string.IsNullOrEmpty(label))
                return label;

            if (label.Length <= MAX_LABEL_LENGTH)
                return label;

            return label.Substring(0, MAX_LABEL_LENGTH) + "...";
        }

        const int MAX_LABEL_LENGTH = 1024;
    }
}
