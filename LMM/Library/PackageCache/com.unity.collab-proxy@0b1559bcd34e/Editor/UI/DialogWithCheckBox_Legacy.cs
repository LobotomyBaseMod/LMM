#if !UNITY_6000_3_OR_NEWER
using UnityEditor;
using UnityEngine;

using Codice.Client.Common;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class DialogWithCheckBox : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 535, baseRect.height);
            }
        }

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
            checkBoxValue = false;

            DialogWithCheckBox dialog = Create(
                title,
                message,
                positiveButtonText,
                neutralButtonText,
                negativeButtonText,
                dontShowAgainContent);

            ResponseType result = dialog.RunModal(parentWindow);

            if (result == ResponseType.None)
                return GuiMessage.GuiMessageResponseButton.None;

            checkBoxValue = dialog.mCheckBox;

            if (result == ResponseType.Cancel)
                return GuiMessage.GuiMessageResponseButton.Neutral;

            if (result == ResponseType.Ok)
                return GuiMessage.GuiMessageResponseButton.Positive;

            return GuiMessage.GuiMessageResponseButton.Negative;
        }

        protected override string GetTitle()
        {
            return mTitle;
        }

        protected override string GetExplanation()
        {
            return mMessage;
        }

        protected override void DoCheckBoxArea_Legacy()
        {
            if (mDontShowAgainContent == null)
                return;

            GUILayout.Space(22f);

            Rect backgroundRect = new Rect(0, GUILayoutUtility.GetLastRect().yMax, position.width, 50);

            EditorGUI.DrawRect(backgroundRect, UnityStyles.Colors.DarkGray);

            GUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                mCheckBox = EditorGUILayout.ToggleLeft(
                    string.Empty,
                    mCheckBox,
                    EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(-22);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(22);
                DrawTextBlockWithLink.ForMultiLinkLabelInDialog(mDontShowAgainContent);
            }

            GUILayout.Space(-19);
        }

        protected override void DoButtonsArea()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Space(25f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    DoButtonsWithPlatformOrdering(DoOkButton, DoNegativeButton, DoCancelButton);
                }
            }
        }

        void DoNegativeButton()
        {
            if (string.IsNullOrEmpty(mNegativeButtonText))
                return;

            if (!NormalButton(mNegativeButtonText))
                return;

            ApplyButtonAction();
        }

        static DialogWithCheckBox Create(
            string title,
            string message,
            string positiveButtonText,
            string neutralButtonText,
            string negativeButtonText,
            MultiLinkLabelData dontShowAgainContent)
        {
            DialogWithCheckBox instance = CreateInstance<DialogWithCheckBox>();
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;

            instance.mTitle = title;
            instance.mMessage = message;
            instance.mOkButtonText = positiveButtonText;
            instance.mCancelButtonText = neutralButtonText;
            instance.mNegativeButtonText = negativeButtonText;
            instance.mDontShowAgainContent = dontShowAgainContent;

            return instance;
        }

        string mTitle;
        string mMessage;
        string mNegativeButtonText;
        MultiLinkLabelData mDontShowAgainContent;

        bool mCheckBox;
    }
}
#endif
