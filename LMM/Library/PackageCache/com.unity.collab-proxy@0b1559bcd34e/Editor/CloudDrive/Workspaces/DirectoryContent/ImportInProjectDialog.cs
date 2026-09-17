using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal class ImportInProjectDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 600, 200);
            }
        }

        internal static string GetProjectPathToImport(
            string currentCloudDrivePath,
            string workspacePath,
            EditorWindow parentWindow)
        {
            ImportInProjectDialog dialog = Create(currentCloudDrivePath, workspacePath);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            if (dialogResult != ResponseType.Ok)
                return string.Empty;

            return dialog.mRelativeProjectPath;
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.ImportInProjectDialogTitle.GetString();
        }

        protected override string GetExplanation()
        {
            return PlasticLocalization.Name.ImportInProjectDialogExplanation.GetString();
        }

        protected override void DoComponentsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                mRelativeProjectPath = EntryBuilder.CreateTextEntry(
                    PlasticLocalization.Name.ProjectPathLabel.GetString(),
                    mRelativeProjectPath,
                    ENTRY_WIDTH - BROWSE_BUTTON_WIDTH,
                    ENTRY_X);

                Rect browseButtonRect = new Rect(
                    ENTRY_X + ENTRY_WIDTH - BROWSE_BUTTON_WIDTH + BUTTON_MARGIN,
                    GUILayoutUtility.GetLastRect().y,
                    BROWSE_BUTTON_WIDTH - BUTTON_MARGIN,
                    20);

                if (GUI.Button(browseButtonRect, "..."))
                    DoBrowseForPath();
            }
        }

        static ImportInProjectDialog Create(string currentCloudDrivePath, string workspacePath)
        {
            var instance = CreateInstance<ImportInProjectDialog>();
            instance.IsResizable = false;

            instance.mRelativeProjectPath = GetProposedProjectPath(
                currentCloudDrivePath, workspacePath);

            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.ImportButton.GetString();

            return instance;
        }

        void DoBrowseForPath()
        {
            string projectPath = AssetsPath.GetFullPath.ForPath(ProjectPath.Get());

            string selectedPath = AssetsPath.GetFullPath.ForPath(EditorUtility.SaveFolderPanel(
                PlasticLocalization.Name.SelectProjectPathDialogTitle.GetString(),
                projectPath,
                ""));

            if (string.IsNullOrEmpty(selectedPath))
                return;

            if (!selectedPath.StartsWith(projectPath))
            {
                ((IProgressControls)mProgressControls).ShowError(
                    PlasticLocalization.Name.PathMustBeInsideProjectFolder.GetString());
                return;
            }

            ((IProgressControls)mProgressControls).HideProgress();

            if (selectedPath == projectPath)
            {
                mRelativeProjectPath = "/";
                return;
            }

            mRelativeProjectPath = selectedPath.Substring(projectPath.Length).Replace('\\', '/');
        }

        internal override void OkButtonAction()
        {
            if (!mRelativeProjectPath.StartsWith("/"))
            {
                ((IProgressControls)mProgressControls).ShowError(
                    PlasticLocalization.Name.PathMustStartWithSlash.GetString());
                return;
            }

            base.OkButtonAction();
        }

        static string GetProposedProjectPath(string currentCloudDrivePath, string workspacePath)
        {
            string relativeProjectPath =
                currentCloudDrivePath.Substring(workspacePath.Length).Replace('\\', '/');

            if (string.IsNullOrEmpty(relativeProjectPath))
                return "/";

            return relativeProjectPath;
        }

        string mRelativeProjectPath;

        const float ENTRY_WIDTH = 400;
        const float ENTRY_X = 120f;
        const float BROWSE_BUTTON_WIDTH = 30;
        const float BUTTON_MARGIN = 5;
    }
}
