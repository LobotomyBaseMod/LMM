using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.CloudDrive.Workspaces;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace
{
    internal class CreateWorkspaceView :
        IPlasticDialogCloser
    {
        internal interface ICreateWorkspaceListener
        {
            void OnWorkspaceCreated(
                WorkspaceCreationData wkCreationData, WorkspaceInfo createdWorkspace);
        }

        internal CreateWorkspaceView(
            IPlasticWebRestApi restApi,
            IPlasticAPI plasticApi,
            ICreateWorkspaceListener listener,
            EditorWindow parentWindow)
        {
            mPlasticApi = plasticApi;
            mCreateWorkspaceListener = listener;
            mParentWindow = parentWindow;

            mProgressControls = new ProgressControlsForViews();

            mCreateWorkspacePanel = new CreateWorkspacePanel(
                string.Empty,
                string.Empty,
                restApi,
                plasticApi,
                parentWindow,
                mProgressControls);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI()
        {
            mCreateWorkspacePanel.OnGUI(mProgressControls.IsOperationRunning());

            GUILayout.Space(15);

            DoCreateWorkspaceArea(
                mCreateWorkspacePanel.IsInputValid(),
                mProgressControls,
                CreateWorkspaceButtonValidationAction);

            GUILayout.Space(15);

            DoNotificationArea(mProgressControls.ProgressData);
        }

        void IPlasticDialogCloser.CloseDialog()
        {
            mWkCreationData.Result = true;

            CreateWorkspaceOperation.CreateWorkspace(
                mWkCreationData, mPlasticApi, mProgressControls,
                (createdWorkspace) =>
                {
                    mCreateWorkspaceListener.OnWorkspaceCreated(mWkCreationData, createdWorkspace);
                });
        }

        void CreateWorkspaceButtonValidationAction()
        {
            mWkCreationData = mCreateWorkspacePanel.BuildCreationData();

            // It calls IPlasticDialogCloser.CloseDialog() when the validation is OK
            WorkspaceCreationValidation.AsyncValidation(
                mWkCreationData, this, mProgressControls);
        }

        static void DoCreateWorkspaceArea(
            bool isInputValid,
            ProgressControlsForViews progressControls,
            Action createWorkspaceButtonValidationAction)
        {
            Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));

            bool isButtonEnabled = isInputValid && !progressControls.IsOperationRunning();

            bool isButtonClicked = DoButton(
                PlasticLocalization.Name.CreateCloudWorkspaceButton.GetString(),
                GUI.skin.button,
                isButtonEnabled,
                CREATE_WORKSPACE_BUTTON_WIDTH,
                CREATE_WORKSPACE_BUTTON_MARGIN,
                rect.y + 2);

            GUILayout.Space(5);

            if (progressControls.IsOperationRunning())
                DoProgress(progressControls.ProgressData);

            EditorGUILayout.EndHorizontal();

            if (isButtonClicked)
                createWorkspaceButtonValidationAction();
        }

        static bool DoButton(
            string text,
            GUIStyle style,
            bool isEnabled,
            float width,
            float x = -1,
            float y = -1)
        {
            using (new GuiEnabled(isEnabled))
            {
                GUIContent buttonContent = new GUIContent(text);

                Rect rect = GUILayoutUtility.GetRect(
                    buttonContent, style,
                    GUILayout.MinWidth(width),
                    GUILayout.MaxWidth(width));

                if (x != -1)
                    rect.x = x;

                if (y != -1)
                    rect.y = y;

                bool result = GUI.Button(rect, buttonContent, style);

                return result;
            }
        }

        static void DoProgress(
            ProgressControlsForViews.Data data)
        {
            if (string.IsNullOrEmpty(data.ProgressMessage))
                return;

            DrawProgressForViews.ForIndeterminateProgressSpinner(data);
        }

        static void DoNotificationArea(
            ProgressControlsForViews.Data data)
        {
            if (string.IsNullOrEmpty(data.NotificationMessage))
                return;

            DrawProgressForViews.ForNotificationArea(data);
        }

        WorkspaceCreationData mWkCreationData;

        readonly CreateWorkspacePanel mCreateWorkspacePanel;
        readonly EditorWindow mParentWindow;
        readonly ProgressControlsForViews mProgressControls;
        readonly ICreateWorkspaceListener mCreateWorkspaceListener;
        readonly IPlasticAPI mPlasticApi;

        const float CREATE_WORKSPACE_BUTTON_MARGIN = 32;
        const float CREATE_WORKSPACE_BUTTON_WIDTH = 160;
    }
}
