using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.UI;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs
{
    internal class FilterRulesConfirmationDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 520, 350);
            }
        }

        internal static FilterRulesConfirmationData AskForConfirmation(
            string[] rules,
            bool isAddOperation,
            bool isApplicableToAllWorkspaces,
            EditorWindow parentWindow)
        {
            string explanation = PlasticLocalization.GetString(isAddOperation ?
                PlasticLocalization.Name.FilterRulesConfirmationAddMessage :
                PlasticLocalization.Name.FilterRulesConfirmationRemoveMessage);

            FilterRulesConfirmationDialog dialog = Create(
                explanation, GetRulesText(rules), isApplicableToAllWorkspaces);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            FilterRulesConfirmationData result = new FilterRulesConfirmationData(
                dialog.mApplyRulesToAllWorkspace, dialog.GetRules());

            result.Result = dialogResult == ResponseType.Ok;
            return result;
        }

        protected override void DoComponentsArea()
        {
            Rect rulesRect = GUILayoutUtility.GetRect(
                new GUIContent(string.Empty),
                new GUIStyle(EditorStyles.textArea) { wordWrap = false, },
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true));

            GUI.SetNextControlName(RULES_TEXTAREA_CONTROL_NAME);
            mRulesText = EditorGUI.ScrollableTextAreaInternal(
                rulesRect,
                mRulesText,
                ref mScrollPosition,
                new GUIStyle(EditorStyles.textArea) { wordWrap = false, });

            mIsTextAreaFocused = FixTextAreaSelectionIfNeeded(mIsTextAreaFocused);

            if (!mIsApplicableToAllWorkspaces)
                return;

            mApplyRulesToAllWorkspace = EditorGUILayout.ToggleLeft(
                PlasticLocalization.GetString(PlasticLocalization.Name.ApplyRulesToAllWorkspaceCheckButton),
                mApplyRulesToAllWorkspace, GUILayout.ExpandWidth(true));
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.GetString(
                PlasticLocalization.Name.FilterRulesConfirmationTitle);
        }

        protected override string GetExplanation()
        {
            return mDialogExplanation;
        }

        string[] GetRules()
        {
            if (string.IsNullOrEmpty(mRulesText))
                return new string[0];

            return mRulesText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        }

        static bool FixTextAreaSelectionIfNeeded(bool isTextAreaFocused)
        {
            TextEditor textEditor = UnityInternals.UnityEditor.EditorGUI.activeEditor?.InternalObject;

            // text editor is null when it is not focused
            if (textEditor == null)
                return false;

            // restore the selection the first time that has selected text
            // because it is done automatically by Unity
            if (isTextAreaFocused)
                return true;

            if (string.IsNullOrEmpty(textEditor.SelectedText))
                return false;

            textEditor.SelectNone();
            textEditor.MoveTextEnd();
            return true;
        }

        static string GetRulesText(string[] rules)
        {
            if (rules == null)
                return string.Empty;

            return string.Join(Environment.NewLine, rules)
                 + Environment.NewLine;
        }

        static FilterRulesConfirmationDialog Create(
            string explanation,
            string rulesText,
            bool isApplicableToAllWorkspaces)
        {
            var instance = CreateInstance<FilterRulesConfirmationDialog>();
            instance.mDialogExplanation = explanation;
            instance.mRulesText = rulesText;
            instance.mIsApplicableToAllWorkspaces = isApplicableToAllWorkspaces;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.AddControlConsumingEnterKey(RULES_TEXTAREA_CONTROL_NAME);
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        bool mIsTextAreaFocused;
        Vector2 mScrollPosition;

        bool mApplyRulesToAllWorkspace;

        bool mIsApplicableToAllWorkspaces;
        string mRulesText;
        string mDialogExplanation;

        const string RULES_TEXTAREA_CONTROL_NAME = "RulesTextArea";
    }
}
