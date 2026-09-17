using System;

using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class DrawActionButtonWithMenu
    {
        internal static void For(
            string text,
            string tooltip,
            Action buttonAction,
            GenericMenu actionMenu)
        {
            float width = MeasureMaxWidth.ForTexts(UnityStyles.PendingChangesTab.ActionButtonLeft, text);

            For(
                text,
                tooltip,
                width,
                buttonAction,
                actionMenu,
                UnityStyles.PendingChangesTab.ActionButtonLeft,
                UnityStyles.PendingChangesTab.DropDownButton);
        }

        internal static void ForTopbar(
            string text,
            string tooltip,
            Action buttonAction,
            GenericMenu actionMenu)
        {
            float width = MeasureMaxWidth.ForTexts(UnityStyles.Topbar.Button, text);

            For(
                text,
                tooltip,
                width,
                buttonAction,
                actionMenu,
                UnityStyles.Topbar.ButtonLeft,
                UnityStyles.Topbar.ButtonRight);
        }

        internal static void ForCommentsSection(
            string text,
            float totalWidth,
            Action buttonAction,
            GenericMenu actionMenu)
        {
            // Action button
            GUIContent buttonContent = new GUIContent(text);

            Rect rt = GUILayoutUtility.GetRect(
                buttonContent,
                UnityStyles.PendingChangesTab.ActionButtonLeft,
                GUILayout.MinWidth(totalWidth - DROPDOWN_BUTTON_WIDTH),
                GUILayout.MaxWidth(totalWidth - DROPDOWN_BUTTON_WIDTH));

            if (GUI.Button(rt, buttonContent, UnityStyles.PendingChangesTab.ActionButton))
            {
                buttonAction();
            }

            // Menu dropdown
            GUIContent dropDownContent = new GUIContent(
                string.Empty, Images.GetDropDownIcon());

            Rect dropDownRect = GUILayoutUtility.GetRect(
                dropDownContent,
                UnityStyles.PendingChangesTab.DropDownButton,
                GUILayout.MinWidth(DROPDOWN_BUTTON_WIDTH),
                GUILayout.MaxWidth(DROPDOWN_BUTTON_WIDTH));

            if (EditorGUI.DropdownButton(
                    dropDownRect,
                    dropDownContent,
                    FocusType.Passive,
                    UnityStyles.PendingChangesTab.DropDownButton))
            {
                actionMenu.DropDown(dropDownRect);
            }
        }

        static void For(
            string text,
            string tooltip,
            float width,
            Action buttonAction,
            GenericMenu actionMenu,
            GUIStyle buttonStyle,
            GUIStyle dropDownStyle)
        {
            // Action button
            GUIContent buttonContent = new GUIContent(text, tooltip);

            Rect rt = GUILayoutUtility.GetRect(
                buttonContent,
                buttonStyle,
                GUILayout.MinWidth(width),
                GUILayout.MaxWidth(width));

            if (GUI.Button(rt, buttonContent, buttonStyle))
            {
                buttonAction();
            }

            // Menu dropdown
            GUIContent dropDownContent = new GUIContent(
                string.Empty, Images.GetDropDownIcon());

            Rect dropDownRect = GUILayoutUtility.GetRect(
                dropDownContent,
                dropDownStyle,
                GUILayout.MinWidth(DROPDOWN_BUTTON_WIDTH),
                GUILayout.MaxWidth(DROPDOWN_BUTTON_WIDTH));

            if (EditorGUI.DropdownButton(
                    dropDownRect, dropDownContent, FocusType.Passive, dropDownStyle))
            {
                actionMenu.DropDown(dropDownRect);
            }
        }

        const int DROPDOWN_BUTTON_WIDTH = 16;
    }
}
