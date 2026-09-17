using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.UI
{
    internal abstract class PlasticDialog : EditorWindow, IPlasticDialogCloser
    {
        internal enum SizeToContent
        {
            Manual = 0,
            Automatic = 1
        }

        protected virtual Rect DefaultRect
        {
            get
            {
                int pixelWidth = Screen.currentResolution.width;
                float x = (pixelWidth - DEFAULT_WIDTH) / 2;
                return new Rect(x, 200, DEFAULT_WIDTH, 1);
            }
        }

        protected virtual bool IsResizable { get; set; }

        internal void SetSizeToContent(SizeToContent sizeToContent)
        {
            mSizeToContent = sizeToContent;
        }

        internal virtual void OkButtonAction()
        {
            CompleteModal(ResponseType.Ok);
        }

        internal virtual void CancelButtonAction()
        {
            CompleteModal(ResponseType.Cancel);
        }

        internal void CloseButtonAction()
        {
            CompleteModal(ResponseType.None);
        }

        internal void ApplyButtonAction()
        {
            CompleteModal(ResponseType.Apply);
        }

        internal void AddControlConsumingEnterKey(string controlName)
        {
            mControlsConsumingEnterKey.Add(controlName);
        }

        internal void RunUtility(EditorWindow parentWindow)
        {
            InitializeVars(parentWindow);

            if (!IsResizable)
                MakeNonResizable();

            ShowUtility();
        }

        internal ResponseType RunModal(EditorWindow parentWindow)
        {
            InitializeVars(parentWindow);

            if (!IsResizable)
                MakeNonResizable();

            UI.RunModal.Dialog(this);
            return mAnswer;
        }

        internal static void DoButtonsWithPlatformOrdering(
            Action doPrimaryButton,
            Action doCloseButton,
            Action doCancelButton)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                doPrimaryButton();
                doCloseButton();
                doCancelButton();
                return;
            }

            doCancelButton();
            doCloseButton();
            doPrimaryButton();
        }

        protected virtual void OnGUI()
        {
            if (Event.current == null)
                return;

            // If the Dialog has been saved into the Unity editor layout and persisted between restarts, the methods
            // to configure the dialogs will be skipped. Simple fix here is to close it when this state is detected.
            // Fixes a NPE loop when the state mentioned above is occurring.
            if (!mIsConfigured)
            {
                Close();
                EditorGUIUtility.ExitGUI();
                return;
            }

            // When a modal dialog is displayed, Unity's SynchronizationContext.Post() callbacks are not processed
            // because the modal dialog runs its own event loop.
            // We need to explicitly pump the EditorDispatcher queue to ensure that callbacks from background threads
            // (like ThreadWaiter's afterOperationDelegate) are executed.
            if (Event.current.type == EventType.Layout)
            {
                EditorDispatcher.Update();
            }

            if (!mWasDialogFocusedOnce)
            {
                // Somehow the prevents the dialog from jumping when dragged
                // NOTE(rafa): We cannot do every frame because the modal kidnaps focus for all processes (in mac at least)
                Focus();
                mWasDialogFocusedOnce = true;
            }

            ProcessKeyActions();

            GUI.Box(new Rect(0, 0, position.width, position.height), GUIContent.none, EditorStyles.label);

            float margin = 25;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(margin);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(margin);

                    if (!string.IsNullOrEmpty(GetExplanation()))
                        Paragraph(GetExplanation());

                    DoComponentsArea();

                    GUILayout.Space(15);

                    DoButtonsArea();

                    DoCheckBoxArea_Legacy();

                    mProgressControls.UpdateProgress(this);

                    GUILayout.Space(margin);
                }
                GUILayout.Space(margin);
            }

            if (!IsResizable && mSizeToContent == SizeToContent.Automatic && Event.current.type == EventType.Repaint)
            {
                var lastRect = GUILayoutUtility.GetLastRect();
                float desiredHeight = lastRect.yMax;

                Rect newPos = position;
                newPos.height = desiredHeight;
                position = newPos;

                maxSize = newPos.size;
                minSize = maxSize;
            }

            if (Event.current.type != EventType.Layout)
                mFocusedControlName = GUI.GetNameOfFocusedControl();
        }

        void OnDestroy()
        {
            if (!mIsConfigured)
                return;

            SaveSettings();

            if (mParentWindow == null)
                return;

            mParentWindow.Focus();
        }

        protected virtual void SaveSettings() { }

        protected virtual void DoComponentsArea() { }

        protected abstract string GetTitle();

        protected void Paragraph(string text)
        {
            GUILayout.Label(text, UnityStyles.Paragraph);
            GUILayout.Space(DEFAULT_PARAGRAPH_SPACING);
        }
        protected virtual string GetExplanation() { return string.Empty; }

        protected void TextBlockWithEndLink(
            string url, string formattedExplanation, GUIStyle textblockStyle)
        {
            ExternalLink externalLink = new ExternalLink
            {
                Label = url,
                Url = url
            };

            DrawTextBlockWithLink.ForExternalLink(
                externalLink, formattedExplanation, textblockStyle);
        }

        protected static void Title(string text)
        {
            GUILayout.Label(text, UnityStyles.Dialog.Title);
        }

        protected static bool TitleToggle(string text, bool isOn)
        {
            return EditorGUILayout.ToggleLeft(text, isOn, UnityStyles.Dialog.Title);
        }

        protected static bool TitleToggle(string text, bool isOn, GUIStyle style)
        {
            return EditorGUILayout.ToggleLeft(text, isOn, style);
        }

        protected static bool NormalButton(string text)
        {
            int textWidth = (int)((GUIStyle)UnityStyles.Dialog.NormalButton)
                .CalcSize(new GUIContent(text)).x;

            return GUILayout.Button(
                text,
                UnityStyles.Dialog.NormalButton,
                GUILayout.Width(Math.Max(80, textWidth)));
        }

        protected virtual void DoCheckBoxArea_Legacy() { }

        protected virtual void DoButtonsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawProgressForDialogs.For(mProgressControls.ProgressData);

                GUILayout.Space(10);

                DoButtonsWithPlatformOrdering(DoOkButton, DoCloseButton, DoCancelButton);
            }
        }

        protected virtual void DoOkButton()
        {
            if (string.IsNullOrEmpty(mOkButtonText))
                return;

            if (!NormalButton(mOkButtonText))
                return;

            OkButtonAction();
        }

        protected void DoCancelButton()
        {
            if (string.IsNullOrEmpty(mCancelButtonText))
                return;

            if (!NormalButton(mCancelButtonText))
                return;

            CancelButtonAction();
        }

        protected virtual void DoCloseButton()
        {
            if (string.IsNullOrEmpty(mCloseButtonText))
                return;

            if (!NormalButton(mCloseButtonText))
                return;

            CloseButtonAction();
        }

        protected void FocusTextAreaIfNeeded(string controlName)
        {
            if (mWasTextAreaFocusedOnce)
                return;

            if (Event.current.type == EventType.Layout)
                return;

            UnityEditor.EditorGUI.FocusTextInControl(controlName);
            mWasTextAreaFocusedOnce = true;
        }

        void IPlasticDialogCloser.CloseDialog()
        {
            CompleteModal(ResponseType.Ok);
        }

        void ProcessKeyActions()
        {
            Event e = Event.current;

            if (mEnterKeyAction != null &&
                Keyboard.IsReturnOrEnterKeyPressed(e) &&
                !ControlConsumesKey(mControlsConsumingEnterKey, mFocusedControlName))
            {
                mEnterKeyAction();
                e.Use();
                return;
            }

            if (mEscapeKeyAction != null &&
                Keyboard.IsKeyPressed(e, KeyCode.Escape))
            {
                mEscapeKeyAction();
                e.Use();
                return;
            }
        }

        void CompleteModal(ResponseType answer)
        {
            mAnswer = answer;

            if (mParentWindow == null)
                return;

            Close();
            Repaint();
        }

        void InitializeVars(EditorWindow parentWindow)
        {
            mIsConfigured = true;
            mAnswer = ResponseType.None;

            titleContent = new GUIContent(GetTitle());

            mWasDialogFocusedOnce = false;
            mWasTextAreaFocusedOnce = false;

            position = DefaultRect;
            mParentWindow = parentWindow;
        }

        void MakeNonResizable()
        {
            maxSize = DefaultRect.size;
            minSize = maxSize;
        }

        static bool ControlConsumesKey(
            List<string> controlsConsumingKey,
            string focusedControlName)
        {
            if (string.IsNullOrEmpty(focusedControlName))
                return false;

            foreach (string controlName in controlsConsumingKey)
            {
                if (focusedControlName.Equals(controlName))
                    return true;
            }

            return false;
        }

        static GUISkin GetEditorSkin()
        {
            return EditorGUIUtility.isProSkin ?
                EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene) :
                EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        }

        string mFocusedControlName;

        bool mWasDialogFocusedOnce;
        bool mWasTextAreaFocusedOnce;
        bool mIsConfigured;
        ResponseType mAnswer;

        protected Action mEnterKeyAction = null;
        protected Action mEscapeKeyAction = null;

        protected string mOkButtonText = PlasticLocalization.Name.OkButton.GetString();
        protected string mCancelButtonText = PlasticLocalization.Name.CancelButton.GetString();
        protected string mCloseButtonText;

        EditorWindow mParentWindow;
        SizeToContent mSizeToContent = SizeToContent.Automatic;

        protected ProgressControlsForDialogs mProgressControls = new ProgressControlsForDialogs();

        List<string> mControlsConsumingEnterKey = new List<string>();

        const float DEFAULT_WIDTH = 500f;
        const float DEFAULT_PARAGRAPH_SPACING = 10f;
    }
}
