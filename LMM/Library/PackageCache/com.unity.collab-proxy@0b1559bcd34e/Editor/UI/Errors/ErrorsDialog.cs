using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;

namespace Unity.PlasticSCM.Editor.UI.Errors
{
    internal class ErrorsDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 810, 385);
            }
        }

        internal static void ShowDialog(
            string title,
            string explanation,
            List<ErrorMessage> errors,
            EditorWindow parentWindow)
        {
            ErrorsDialog dialog = Create(title, explanation, errors);
            dialog.RunModal(parentWindow);
        }

        protected override void DoComponentsArea()
        {
            mErrorsPanel.OnGUI();
        }

        protected override string GetTitle()
        {
            return mTitle;
        }

        protected override string GetExplanation()
        {
            return mExplanation;
        }

        static ErrorsDialog Create(
            string title,
            string explanation,
            List<ErrorMessage> errors)
        {
            var instance = CreateInstance<ErrorsDialog>();
            instance.mEscapeKeyAction = instance.CloseButtonAction;
            instance.mOkButtonText = string.Empty;
            instance.mCancelButtonText = string.Empty;
            instance.mCloseButtonText = PlasticLocalization.Name.CloseButton.GetString();
            instance.mTitle = title;
            instance.mExplanation = explanation;
            instance.BuildComponents();
            instance.SetErrorsList(errors);
            return instance;
        }

        void SetErrorsList(List<ErrorMessage> errors)
        {
            mErrorsPanel.UpdateErrorsList(errors);
        }

        void BuildComponents()
        {
            mErrorsPanel = new ErrorsPanel(
                string.Empty,
                UnityConstants.CloudDrive.ERRORS_DIALOG_TREE_SETTINGS_NAME,
                UnityConstants.CloudDrive.ERRORS_DIALOG_SPLITTER_SETTINGS_NAME);
        }

        ErrorsPanel mErrorsPanel;
        string mTitle;
        string mExplanation;
    }
}
