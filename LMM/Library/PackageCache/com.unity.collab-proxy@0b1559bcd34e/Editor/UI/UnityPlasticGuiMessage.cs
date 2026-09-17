using UnityEditor;

using Codice.Client.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.CloudDrive;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class UnityPlasticGuiMessage : GuiMessage.IGuiMessage
    {
        internal UnityPlasticGuiMessage()
        {
            Execute.WhenEditorIsReady(() =>
            {
                mIsEditorReady = true;
            });
        }

        void GuiMessage.IGuiMessage.ShowMessage(
            string title,
            string message,
            GuiMessage.GuiMessageType messageType)
        {
            if (!UVCSPlugin.Instance.ConnectionMonitor.IsConnected)
                return;

            if (!mIsEditorReady)
            {
                LogMessage(title, message, messageType);
                return;
            }

            EditorUtility.DisplayDialog(
                GetDialogTitleForMessageType(title, messageType),
                message,
                PlasticLocalization.GetString(PlasticLocalization.Name.CloseButton));
        }

        void GuiMessage.IGuiMessage.ShowError(string message)
        {
            if (!UVCSPlugin.Instance.ConnectionMonitor.IsConnected)
                return;

            if (!mIsEditorReady)
            {
                LogMessage(GetDialogTitle(string.Empty), message, GuiMessage.GuiMessageType.Critical);
                return;
            }

            EditorUtility.DisplayDialog(
                GetDialogTitleForMessageType(null, GuiMessage.GuiMessageType.Critical),
                message,
                PlasticLocalization.GetString(PlasticLocalization.Name.CloseButton));
        }

        GuiMessage.GuiMessageResponseButton GuiMessage.IGuiMessage.ShowQuestion(
            string title,
            string message,
            string positiveActionButton,
            string neutralActionButton,
            string negativeActionButton)
        {
            if (string.IsNullOrEmpty(negativeActionButton))
            {
                bool result = EditorUtility.DisplayDialog(
                    title,
                    message,
                    positiveActionButton,
                    neutralActionButton);

                return (result) ?
                    GuiMessage.GuiMessageResponseButton.Positive :
                    GuiMessage.GuiMessageResponseButton.Neutral;
            }

            int intResult = EditorUtility.DisplayDialogComplex(
                title,
                message,
                positiveActionButton,
                neutralActionButton,
                negativeActionButton);

            return GetResponse(intResult);
        }

        bool GuiMessage.IGuiMessage.ShowQuestion(
            string title,
            string message,
            string yesButton)
        {
            return EditorUtility.DisplayDialog(
                title,
                message,
                yesButton,
                PlasticLocalization.GetString(PlasticLocalization.Name.NoButton));
        }

        bool GuiMessage.IGuiMessage.ShowQuestionWithLearnMore(
            string title,
            string message,
            string yesButton,
            string noButton,
            MultiLinkLabelData learnMoreContent)
        {
            return EditorUtility.DisplayDialog(
                title,
                message,
                yesButton,
                noButton);
        }

        bool GuiMessage.IGuiMessage.ShowYesNoQuestion(string title, string message)
        {
            return EditorUtility.DisplayDialog(
                title,
                message,
                PlasticLocalization.GetString(PlasticLocalization.Name.YesButton),
                PlasticLocalization.GetString(PlasticLocalization.Name.NoButton));
        }

        GuiMessage.GuiMessageResponseButton GuiMessage.IGuiMessage.ShowYesNoCancelQuestion(
            string title, string message)
        {
            int intResult = EditorUtility.DisplayDialogComplex(
                title,
                message,
                PlasticLocalization.GetString(PlasticLocalization.Name.YesButton),
                PlasticLocalization.GetString(PlasticLocalization.Name.CancelButton),
                PlasticLocalization.GetString(PlasticLocalization.Name.NoButton));

            return GetResponse(intResult);
        }

        bool GuiMessage.IGuiMessage.ShowYesNoQuestionWithType(
            string title, string message, GuiMessage.GuiMessageType messageType)
        {
            return EditorUtility.DisplayDialog(
                GetDialogTitleForMessageType(title, messageType),
                message,
                PlasticLocalization.GetString(PlasticLocalization.Name.YesButton),
                PlasticLocalization.GetString(PlasticLocalization.Name.NoButton));
        }

        GuiMessage.GuiMessageResponseButton GuiMessage.IGuiMessage.ShowQuestionWithCheckBox(
            string title,
            string message,
            string positiveButtonText,
            string neutralButtonText,
            string negativeButtonText,
            GuiMessage.GuiMessageType messageType,
            MultiLinkLabelData dontShowAgainContent,
            out bool checkBoxValue)
        {
            return DialogWithCheckBox.Show(
                title,
                message,
                positiveButtonText,
                neutralButtonText,
                negativeButtonText,
                messageType,
                dontShowAgainContent,
                ParentWindow.Get(),
                out checkBoxValue);
        }

        static string GetDialogTitle(string title)
        {
            string defaultWindowTitle =
                CloudDrivePlugin.Instance.IsEnabled() && !UVCSPlugin.Instance.IsEnabled() ?
                UnityConstants.CloudDrive.WINDOW_TITLE :
                UnityConstants.UVCS_WINDOW_TITLE;

            if (string.IsNullOrEmpty(title))
                return defaultWindowTitle;

            if (title.Contains(defaultWindowTitle))
                return title;

            return string.Format("{0} - {1}", defaultWindowTitle, title);
        }

        static string GetDialogTitleForMessageType(
            string title,
            GuiMessage.GuiMessageType messageType)
        {
            string alertTypeText = GetMessageTypeText(messageType);
            return string.Format("{0} - {1}", alertTypeText, GetDialogTitle(title));
        }

        static string GetMessageTypeText(GuiMessage.GuiMessageType messageType)
        {
            string alertTypeText = string.Empty;

            switch (messageType)
            {
                case GuiMessage.GuiMessageType.Informational:
                    alertTypeText = "Information";
                    break;
                case GuiMessage.GuiMessageType.Warning:
                    alertTypeText = "Warning";
                    break;
                case GuiMessage.GuiMessageType.Critical:
                    alertTypeText = "Error";
                    break;
                case GuiMessage.GuiMessageType.Question:
                    alertTypeText = "Question";
                    break;
            }

            return alertTypeText;
        }

        static GuiMessage.GuiMessageResponseButton GetResponse(int dialogResult)
        {
            switch (dialogResult)
            {
                case 0:
                    return GuiMessage.GuiMessageResponseButton.Positive;
                case 1:
                    return GuiMessage.GuiMessageResponseButton.Neutral;
                case 2:
                    return GuiMessage.GuiMessageResponseButton.Negative;
                default:
                    return GuiMessage.GuiMessageResponseButton.Neutral;
            }
        }

        static void LogMessage(
            string title,
            string message,
            GuiMessage.GuiMessageType messageType)
        {
            string fullMessage = string.Format(
                "{0}: {1}",
                GetDialogTitle(title),
                message);

            switch (messageType)
            {
                case GuiMessage.GuiMessageType.Critical:
                    UnityEngine.Debug.LogError(fullMessage);
                    break;
                case GuiMessage.GuiMessageType.Informational:
                    UnityEngine.Debug.Log(fullMessage);
                    break;
                case GuiMessage.GuiMessageType.Warning:
                    UnityEngine.Debug.LogWarning(fullMessage);
                    break;
            }
        }

        volatile bool mIsEditorReady = false;
    }
}
