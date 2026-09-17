using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList
{
    internal class ErrorEmptyState : CenteredContentPanel
    {
        internal bool HasError
        {
            get { return mError != null; }
        }

        internal ErrorEmptyState(Action retryAction, Action repaintAction) : base(repaintAction)
        {
            mRetryAction = retryAction;
        }

        internal void SetError(Exception error)
        {
            mError = error;
            mIsRetryButtonEnabled = true;
        }

        protected override void DrawGUI()
        {
            CenterContent(
                () =>
                {
                    GUILayout.Label(
                        PlasticLocalization.Name.CannotLoadBranches.GetString(),
                        EditorStyles.boldLabel);

                    if (HasError)
                    {
                        GUILayout.Label(
                            new GUIContent(Images.GetInfoDialogIcon(), mError.Message),
                            GUILayout.Width(16), GUILayout.Height(16));
                    }
                },
                () =>
                {
                    GUIStyle labelWrapStyle = new GUIStyle(EditorStyles.label);
                    labelWrapStyle.wordWrap = true;
                    labelWrapStyle.alignment = TextAnchor.MiddleCenter;
                    labelWrapStyle.padding = new RectOffset(15, 15, 0, 0);
                    GUILayout.Label(
                        PlasticLocalization.Name.CannotLoadBranchesExplanation.GetString(),
                        labelWrapStyle);
                },
                () =>
                {
                    GUI.enabled = mIsRetryButtonEnabled;

                    if (GUILayout.Button(PlasticLocalization.Name.RetryButton.GetString()))
                    {
                        mRetryAction();
                        mIsRetryButtonEnabled = false;
                        mRepaintAction();
                    }

                    GUI.enabled = true;
                }
            );
        }

        readonly Action mRetryAction;

        Exception mError;
        bool mIsRetryButtonEnabled = true;
    }
}
