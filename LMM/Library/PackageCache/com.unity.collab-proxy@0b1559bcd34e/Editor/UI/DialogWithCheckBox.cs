#if UNITY_6000_3_OR_NEWER
using System;

using UnityEditor;

using Codice.Client.Common;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class DialogWithCheckBox
    {
        internal static GuiMessage.GuiMessageResponseButton Show(
            string title,
            string message,
            string positiveButtonText,
            string neutralButtonText,
            string negativeButtonText,
            GuiMessage.GuiMessageType messageType,
            MultiLinkLabelData dontShowAgainContent,
            EditorWindow parentWindow,
            out bool checkBoxValue)
        {
            string optOutKey = Guid.NewGuid().ToString();
            string prefixedOptOutKey = OPT_OUT_PREFIX + optOutKey;

            GuiMessage.GuiMessageResponseButton result;
            if (string.IsNullOrEmpty(negativeButtonText))
            {
                result = ShowTwoOptionsDialog(
                    title,
                    message,
                    positiveButtonText,
                    neutralButtonText,
                    optOutKey,
                    messageType);
            }
            else
            {
                result = ShowThreeOptionsDialog(
                    title,
                    message,
                    positiveButtonText,
                    neutralButtonText,
                    negativeButtonText,
                    optOutKey,
                    messageType);
            }

            checkBoxValue = EditorPrefs.HasKey(prefixedOptOutKey);

            EditorPrefs.DeleteKey(prefixedOptOutKey);
            SessionState.EraseInt(prefixedOptOutKey);

            return result;
        }

        static GuiMessage.GuiMessageResponseButton ShowTwoOptionsDialog(
            string title,
            string message,
            string positiveButtonText,
            string neutralButtonText,
            string optOutKey,
            GuiMessage.GuiMessageType messageType)
        {
            bool dialogResult = EditorDialog.DisplayDecisionDialogWithOptOut(
                title,
                message,
                positiveButtonText,
                neutralButtonText,
                DialogOptOutDecisionType.ForThisMachine,
                optOutKey,
                GetDialogIconType(messageType));

            return dialogResult ?
                GuiMessage.GuiMessageResponseButton.Positive :
                GuiMessage.GuiMessageResponseButton.Neutral;
        }

        static GuiMessage.GuiMessageResponseButton ShowThreeOptionsDialog(
            string title,
            string message,
            string positiveButtonText,
            string neutralButtonText,
            string negativeButtonText,
            string optOutKey,
            GuiMessage.GuiMessageType messageType)
        {
            DialogResult dialogResult = EditorDialog.DisplayComplexDecisionDialogWithOptOut(
                title,
                message,
                positiveButtonText,
                negativeButtonText,
                neutralButtonText,
                DialogOptOutDecisionType.ForThisMachine,
                optOutKey,
                GetDialogIconType(messageType));

            if (dialogResult == DialogResult.Cancel)
                return GuiMessage.GuiMessageResponseButton.Neutral;

            if (dialogResult == DialogResult.DefaultAction)
                return GuiMessage.GuiMessageResponseButton.Positive;

            return GuiMessage.GuiMessageResponseButton.Negative;
        }

        static DialogIconType GetDialogIconType(GuiMessage.GuiMessageType messageType)
        {
            switch (messageType)
            {
                case GuiMessage.GuiMessageType.Warning:
                    return DialogIconType.Warning;

                case GuiMessage.GuiMessageType.Critical:
                    return DialogIconType.Error;

                case GuiMessage.GuiMessageType.Informational:
                case GuiMessage.GuiMessageType.Question:
                default:
                    return DialogIconType.Info;
            }
        }

        const string OPT_OUT_PREFIX = "DialogOptOut."; // UnityEditor.EditorDialog.k_OptOutPrefix
    }
}
#endif
