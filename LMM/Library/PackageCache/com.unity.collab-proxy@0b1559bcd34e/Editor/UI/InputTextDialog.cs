using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.UI
{
    internal class InputTextDialog : PlasticDialog
    {
        internal delegate void OkEventHandler(
            string text,
            IPlasticDialogCloser closer,
            ProgressControlsForDialogs progressControls);

        internal string TextValue { get { return mTextValue; } }

        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, mWidth, baseRect.height);
            }
        }

        protected override string GetTitle()
        {
            return mTitle;
        }

        protected override void DoComponentsArea()
        {
            if (!string.IsNullOrEmpty(mExplanation))
            {
                Title(mTitle);
                GUILayout.Space(10f);
            }

            DoInputArea();
        }

        internal static InputTextDialogResult GetInputText(
            string title,
            string explanation,
            string labelText,
            string defaultValue,
            string okButtonText,
            OkEventHandler onOk,
            EditorWindow parentWindow,
            int width)
        {
            InputTextDialog dialog = Create(
                title,
                explanation,
                labelText,
                defaultValue,
                okButtonText,
                onOk,
                width,
                new ProgressControlsForDialogs());

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            InputTextDialogResult result = new InputTextDialogResult
            {
                Result = dialogResult == ResponseType.Ok,
                Text = dialog.mTextValue
            };

            return result;
        }

        static InputTextDialog Create(
            string title,
            string explanation,
            string labelText,
            string defaultValue,
            string okButtonText,
            OkEventHandler onOk,
            int width,
            ProgressControlsForDialogs progressControls)
        {
            var instance = CreateInstance<InputTextDialog>();
            instance.mTitle = title;
            instance.mExplanation = explanation;
            instance.mLabelText = labelText;
            instance.mTextValue = defaultValue;
            instance.mOkButtonText = okButtonText;
            instance.mOnOk = onOk;
            instance.mWidth = width;
            instance.mProgressControls = progressControls;
            instance.mEnterKeyAction = instance.OkButtonWithValidationAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        protected override void DoButtonsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.MinWidth(300)))
                {
                    GUILayout.Space(2);
                    DrawProgressForDialogs.For(
                        mProgressControls.ProgressData);
                    GUILayout.Space(2);
                }

                GUILayout.FlexibleSpace();

                DoButtonsWithPlatformOrdering(DoOkButton, () => { }, DoCancelButton);
            }
        }

        protected override void DoOkButton()
        {
            if (string.IsNullOrEmpty(mOkButtonText))
                return;

            if (!NormalButton(mOkButtonText))
                return;

            OkButtonWithValidationAction();
        }

        void DoInputArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(mLabelText, GUILayout.ExpandWidth(false));

                GUILayout.Space(10f);

                GUI.SetNextControlName(INPUT_TEXT_CONTROL_NAME);

                Rect nameRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mTextValue = EditorGUI.TextField(nameRect, mTextValue);

                FocusTextAreaIfNeeded(INPUT_TEXT_CONTROL_NAME);
            }
        }

        void OkButtonWithValidationAction()
        {
            mTextValue = mTextValue.Trim();
            mOnOk?.Invoke(mTextValue, this, mProgressControls);
        }

        string mTitle;
        string mExplanation;
        string mLabelText;
        string mTextValue;
        int mWidth;

        OkEventHandler mOnOk;

        const string INPUT_TEXT_CONTROL_NAME = "input_text_control";
    }

    internal class InputTextDialogResult
    {
        internal bool Result { get; set; }
        internal string Text { get; set; }
    }
}
