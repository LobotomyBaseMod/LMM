using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal class SwitchModeConfirmationDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 560, 180);
            }
        }

        internal static bool SwitchMode(
            bool isGluonMode,
            EditorWindow parentWindow)
        {
            SwitchModeConfirmationDialog dialog = Create(isGluonMode);
            return dialog.RunModal(parentWindow) == ResponseType.Ok;
        }

        protected override void DoComponentsArea()
        {
            PlasticLocalization.Name currentMode = mIsGluonMode ?
                PlasticLocalization.Name.GluonMode :
                PlasticLocalization.Name.DeveloperMode;

            PlasticLocalization.Name selectedMode = mIsGluonMode ?
                PlasticLocalization.Name.DeveloperMode :
                PlasticLocalization.Name.GluonMode;

            string formattedExplanation = PlasticLocalization.GetString(
                PlasticLocalization.Name.SwitchModeConfirmationDialogExplanation,
                PlasticLocalization.GetString(currentMode),
                PlasticLocalization.GetString(selectedMode));

            TextBlockWithEndLink(UnityUrl.UnityWebsite.Gluon(), formattedExplanation, UnityStyles.Paragraph);
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.GetString(
                PlasticLocalization.Name.SwitchModeConfirmationDialogTitle);
        }

        static SwitchModeConfirmationDialog Create(
            bool isGluonMode)
        {
            var instance = CreateInstance<SwitchModeConfirmationDialog>();
            instance.mIsGluonMode = isGluonMode;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.SwitchButton.GetString();
            return instance;
        }

        bool mIsGluonMode;
    }
}
