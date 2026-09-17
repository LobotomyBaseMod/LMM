using System.Collections.Generic;

using UnityEngine;

using Codice.CM.Common;
using Unity.PlasticSCM.Editor.UI.Tree;

using EditorGUILayout = UnityEditor.EditorGUILayout;
using EditorStyles = UnityEditor.EditorStyles;

#if !UNITY_6000_3_OR_NEWER
using Unity.PlasticSCM.Editor.UnityInternals.UnityEditor;
#else
using UnityEditor;
#endif

namespace Unity.PlasticSCM.Editor.UI.Errors
{
    internal class ErrorsPanel
    {
        internal bool IsVisible { get; private set; }

        internal ErrorsPanel(
            string title,
            string treeSettingsName,
            string splitterSettingsName)
        {
            mTitle = title;
            mTreeSettingsName = treeSettingsName;
            mSplitterSettingsName = splitterSettingsName;

            ErrorsListHeaderState errorsListHeaderState =
                ErrorsListHeaderState.GetDefault();
            TreeHeaderSettings.Load(
                errorsListHeaderState,
                treeSettingsName,
                UnityConstants.UNSORT_COLUMN_ID);

            mErrorsListView = new ErrorsListView(errorsListHeaderState);
            mErrorsListView.Reload();

            mErrorDetailsSplitterState = new SplitterState(
                SplitterSettings.Load(
                    splitterSettingsName,
                    new float[] { 0.60f, 0.40f }),
                new int[] { 100, 100 },
                new int[] { 100000, 100000 }
            );
        }

        internal void UpdateErrorsList(List<ErrorMessage> errorMessages)
        {
            mErrorsListView.BuildModel(errorMessages);
            mErrorsListView.Reload();

            IsVisible = errorMessages.Count > 0;
        }

        internal void OnDisable()
        {
            TreeHeaderSettings.Save(
                mErrorsListView.multiColumnHeader.state,
                mTreeSettingsName);

            SplitterSettings.Save(
                mErrorDetailsSplitterState,
                mSplitterSettingsName);
        }

        internal void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            if (!string.IsNullOrEmpty(mTitle))
            {
                DrawSplitter.ForHorizontalIndicator();

                GUILayout.Label(
                    mTitle,
                    EditorStyles.boldLabel);
            }

            DoErrorsListSplitViewArea(
                mErrorsListView, mErrorDetailsSplitterState);

            EditorGUILayout.EndVertical();
        }

        void DoErrorsListSplitViewArea(
            ErrorsListView errorsListView,
            SplitterState splitterState)
        {
            EditorGUILayout.BeginHorizontal();

            SplitterGUILayout.BeginHorizontalSplit(splitterState);

            DoErrorsListViewArea(errorsListView);

            DoErrorDetailsTextArea(errorsListView.GetSelectedError());

            SplitterGUILayout.EndHorizontalSplit();

            EditorGUILayout.EndHorizontal();
        }

        static void DoErrorsListViewArea(
            ErrorsListView errorsListView)
        {
            Rect treeRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            errorsListView.OnGUI(treeRect);
        }

        void DoErrorDetailsTextArea(ErrorMessage selectedErrorMessage)
        {
            string errorDetailsText = selectedErrorMessage == null ?
                string.Empty : selectedErrorMessage.Error;

            mErrorDetailsScrollPosition = GUILayout.BeginScrollView(
                mErrorDetailsScrollPosition);

            GUILayout.TextArea(
                errorDetailsText, UnityStyles.TextFieldWithWrapping,
                GUILayout.ExpandHeight(true));

            GUILayout.EndScrollView();
        }

        Vector2 mErrorDetailsScrollPosition;

        readonly string mTitle;
        readonly string mTreeSettingsName;
        readonly string mSplitterSettingsName;
        readonly ErrorsListView mErrorsListView;
        readonly SplitterState mErrorDetailsSplitterState;
    }
}
