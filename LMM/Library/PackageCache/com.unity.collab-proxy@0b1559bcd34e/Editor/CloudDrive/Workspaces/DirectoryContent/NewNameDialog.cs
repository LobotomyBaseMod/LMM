using System.IO;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    internal class NewNameDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 500, 200);
            }
        }

        internal static string GetNewNameForCreate(
            string parentPath,
            bool bIsDirectory,
            EditorWindow parentWindow)
        {
            return GetNewName(
                parentPath, string.Empty, bIsDirectory, true, parentWindow);
        }

        internal static string GetNewNameForRename(
            string parentPath,
            string currentName,
            bool bIsDirectory,
            EditorWindow parentWindow)
        {
            return GetNewName(
                parentPath, currentName, bIsDirectory, false, parentWindow);
        }

        static string GetNewName(
            string parentPath,
            string currentName,
            bool bIsDirectory,
            bool bIsCreation,
            EditorWindow parentWindow)
        {
            NewNameDialog dialog = Create(
                parentPath,
                currentName,
                bIsDirectory,
                bIsCreation);

            ResponseType dialogResult = dialog.RunModal(parentWindow);

            if (dialogResult != ResponseType.Ok)
                return string.Empty;

            return dialog.mNewName;
        }

        static NewNameDialog Create(
            string parentPath,
            string currentName,
            bool bIsDirectory,
            bool bIsCreation)
        {
            var instance = CreateInstance<NewNameDialog>();

            instance.mParentPath = parentPath;
            instance.mCurrentName = currentName;
            instance.mNewName = currentName;
            instance.mIsCreation = bIsCreation;
            instance.mIsDirectory = bIsDirectory;

            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            instance.mOkButtonText = bIsCreation ?
                PlasticLocalization.Name.CreateButton.GetString() :
                PlasticLocalization.Name.RenameButton.GetString();

            return instance;
        }

        protected override string GetTitle()
        {
            if (mIsCreation)
                return mIsDirectory ?
                    PlasticLocalization.Name.NewDirectoryTitle.GetString() :
                    PlasticLocalization.Name.NewFileTitle.GetString();

            return mIsDirectory ?
                PlasticLocalization.Name.RenameDirectoryTitle.GetString() :
                PlasticLocalization.Name.RenameFileTitle.GetString();
        }

        protected override void DoComponentsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string labelText = mIsCreation ?
                    PlasticLocalization.Name.Name.GetString() :
                    PlasticLocalization.Name.NewName.GetString();

                GUILayout.Label(labelText, GUILayout.ExpandWidth(false));

                GUILayout.Space(10f);

                GUI.SetNextControlName(NEW_NAME_TEXTAREA_NAME);

                mNewName = GUILayout.TextField(
                    mNewName,
                    GUILayout.ExpandWidth(true));

                FocusTextAreaIfNeeded(NEW_NAME_TEXTAREA_NAME);
            }
        }

        internal override void OkButtonAction()
        {
            if (!IsValidInput(mParentPath, mCurrentName, mNewName, mProgressControls))
                return;

            base.OkButtonAction();
        }

        static bool IsValidInput(
            string parentPath,
            string oldName,
            string newName,
            IProgressControls progressControls)
        {
            if (string.IsNullOrEmpty(newName))
            {
                progressControls.ShowError(
                    PlasticLocalization.Name.ItemNameEmpty.GetString());
                return false;
            }

            if (newName == oldName)
            {
                progressControls.ShowError(
                    PlasticLocalization.Name.ProvideDifferentItemName.GetString());
                return false;
            }

            if (!PathHelper.IsCaseInsensitiveFsSameName(oldName, newName) &&
                IsOnDisk(Path.Combine(parentPath, newName)))
            {
                progressControls.ShowError(
                    PlasticLocalization.Name.ItemExists.GetString(newName));
                return false;
            }

            return true;
        }

        static bool IsOnDisk(string path)
        {
            return Directory.Exists(path) || File.Exists(path);
        }

        string mParentPath;
        string mCurrentName;
        string mNewName;
        bool mIsCreation;
        bool mIsDirectory;

        const string NEW_NAME_TEXTAREA_NAME = "new_name_textarea";
    }
}
