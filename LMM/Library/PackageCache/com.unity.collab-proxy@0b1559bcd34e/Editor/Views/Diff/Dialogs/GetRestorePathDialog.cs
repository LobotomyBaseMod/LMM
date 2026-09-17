using System.IO;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Diff;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Diff.Dialogs
{
    internal class GetRestorePathDialog : PlasticDialog
    {
        internal static GetRestorePathData GetRestorePath(
            string wkPath,
            string restorePath,
            string explanation,
            bool isDirectory,
            bool showSkipButton,
            EditorWindow parentWindow)
        {
            GetRestorePathDialog dialog = Create(
                wkPath,
                GetProposedRestorePath.For(restorePath),
                explanation,
                isDirectory,
                showSkipButton);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            GetRestorePathData result = dialog.BuildGetRestorePathResult();

            result.Result = GetRestorePathResultType(dialogResult);
            return result;
        }

        protected override void DoComponentsArea()
        {
            GUILayout.Label(PlasticLocalization.GetString(
                PlasticLocalization.Name.EnterRestorePathFormTextBoxExplanation),
                EditorStyles.label);

            GUILayout.BeginHorizontal();

            Rect pathRect = GUILayoutUtility.GetRect(
                new GUIContent(string.Empty),
                EditorStyles.textField,
                GUILayout.ExpandWidth(true));

            mRestorePath = EditorGUI.TextField(pathRect, mRestorePath);

            if (GUILayout.Button("...", UnityStyles.Dialog.SmallButton))
            {
                mRestorePath = (mIsDirectory) ?
                    DoOpenFolderPanel(mRestorePath) :
                    DoOpenFilePanel(mRestorePath);
            }

            GUILayout.EndHorizontal();
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.GetString(
                PlasticLocalization.Name.EnterRestorePathFormTitle);
        }

        protected override string GetExplanation()
        {
            return mExplanation;
        }

        static string DoOpenFolderPanel(string actualPath)
        {
            string parentDirectory = null;
            string directoryName = null;

            if (!string.IsNullOrEmpty(actualPath))
            {
                parentDirectory = Path.GetDirectoryName(actualPath);
                directoryName = Path.GetFileName(actualPath);
            }

            string result = EditorUtility.SaveFolderPanel(
                PlasticLocalization.GetString(PlasticLocalization.Name.SelectPathToRestore),
                parentDirectory,
                directoryName);

            if (string.IsNullOrEmpty(result))
                return actualPath;

            return AssetsPath.GetFullPath.ForPath(result);
        }

        static string DoOpenFilePanel(string actualPath)
        {
            string parentDirectory = null;
            string fileName = null;
            string extension = null;

            if (!string.IsNullOrEmpty(actualPath))
            {
                parentDirectory = Path.GetDirectoryName(actualPath);
                fileName = Path.GetFileName(actualPath);
                extension = Path.GetExtension(actualPath);
            }

            string result = EditorUtility.SaveFilePanel(
                PlasticLocalization.GetString(PlasticLocalization.Name.SelectPathToRestore),
                parentDirectory,
                fileName,
                extension);

            if (string.IsNullOrEmpty(result))
                return actualPath;

            return AssetsPath.GetFullPath.ForPath(result);
        }

        protected override void DoCloseButton()
        {
            if (!mShowSkipButton)
                return;

            if (!NormalButton(PlasticLocalization.Name.SkipRestoreButton.GetString()))
                return;

            CloseButtonAction();
        }

        internal override void OkButtonAction()
        {
            GetRestorePathValidation.Validation(
                mWkPath, BuildGetRestorePathResult(),
                this, mProgressControls);
        }

        GetRestorePathData BuildGetRestorePathResult()
        {
            return new GetRestorePathData(mRestorePath);
        }

        static GetRestorePathData.ResultType GetRestorePathResultType(
            ResponseType dialogResult)
        {
            switch (dialogResult)
            {
                case ResponseType.None:
                    return GetRestorePathData.ResultType.Skip;
                case ResponseType.Ok:
                    return GetRestorePathData.ResultType.OK;
                case ResponseType.Cancel:
                    return GetRestorePathData.ResultType.Cancel;
            }

            return GetRestorePathData.ResultType.Cancel;
        }

        static GetRestorePathDialog Create(
            string wkPath,
            string restorePath,
            string explanation,
            bool isDirectory,
            bool showSkipButton)
        {
            var instance = CreateInstance<GetRestorePathDialog>();
            instance.mWkPath = wkPath;
            instance.mRestorePath = restorePath;
            instance.mExplanation = explanation;
            instance.mIsDirectory = isDirectory;
            instance.mShowSkipButton = showSkipButton;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        bool mIsDirectory;
        bool mShowSkipButton;
        string mExplanation = string.Empty;
        string mRestorePath = string.Empty;
        string mWkPath = string.Empty;
    }
}
