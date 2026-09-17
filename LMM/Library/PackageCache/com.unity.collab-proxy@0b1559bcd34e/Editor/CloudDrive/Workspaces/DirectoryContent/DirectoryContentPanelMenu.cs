using System;

using UnityEditor;
using UnityEngine;

using Codice.Utils;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.DirectoryContent
{
    [Flags]
    internal enum DirectoryContentMenuOperations : short
    {
        None = 0,
        CreateFolder = 1 << 0,
        OpenInExplorer = 1 << 1,
        OpenUnityCloud = 1 << 2,
        Open = 1 << 3,
        Delete = 1 << 4,
        Rename = 1 << 5,
        ImportInProject = 1 << 6
    }

    internal static class DirectoryContentMenuUpdater
    {
        internal static DirectoryContentMenuOperations GetAvailableMenuOperations(
            int selectedItemsCount, bool isAnyFileSelected, bool isPathSelected)
        {
            DirectoryContentMenuOperations result = DirectoryContentMenuOperations.None;

            if (!isPathSelected)
                return result;

            result |= DirectoryContentMenuOperations.OpenInExplorer |
                      DirectoryContentMenuOperations.Delete |
                      DirectoryContentMenuOperations.ImportInProject;

            if (selectedItemsCount <= 1)
                result |= DirectoryContentMenuOperations.Rename |
                          DirectoryContentMenuOperations.OpenUnityCloud;

            if (selectedItemsCount == 1)
                result |= DirectoryContentMenuOperations.Open;

            if ((selectedItemsCount == 1 && !isAnyFileSelected) ||
                 selectedItemsCount == 0)
                result |= DirectoryContentMenuOperations.CreateFolder;

            return result;
        }
    }

    internal interface IDirectoryContentMenuOperations
    {
        int GetSelectedItemsCount();
        bool IsAnyFileSelected();
        bool IsPathSelected();

        void CreateFolder();
        void OpenInExplorer();
        void OpenUnityCloud();
        void Open();
        void Delete();
        void Rename();
        void ImportInProject();
    }

    internal class DirectoryContentPanelMenu
    {
        internal DirectoryContentPanelMenu(IDirectoryContentMenuOperations operations)
        {
            mOperations = operations;

            BuildComponents();
        }

        internal void Popup()
        {
            GenericMenu menu = new GenericMenu();

            UpdateMenuItems(menu);

            menu.ShowAsContext();
        }

        internal bool ProcessKeyActionIfNeeded(Event e)
        {
            DirectoryContentMenuOperations operationToExecute = GetMenuOperation(e);

            if (operationToExecute == DirectoryContentMenuOperations.None)
                return false;

            int selectedCount = mOperations.GetSelectedItemsCount();
            bool isAnyFileSelected = mOperations.IsAnyFileSelected();
            bool isPathSelected = mOperations.IsPathSelected();

            DirectoryContentMenuOperations operations =
                DirectoryContentMenuUpdater.GetAvailableMenuOperations(
                    selectedCount, isAnyFileSelected, isPathSelected);

            if (!operations.HasFlag(operationToExecute))
                return false;

            ProcessMenuOperation(operationToExecute);
            return true;
        }

        void CreateFolderMenuItem_Click()
        {
            mOperations.CreateFolder();
        }

        void OpenInExplorerMenuItem_Click()
        {
            mOperations.OpenInExplorer();
        }

        void OpenUnityCloudMenuItem_Click()
        {
            mOperations.OpenUnityCloud();
        }

        void OpenMenuItem_Click()
        {
            mOperations.Open();
        }

        void DeleteMenuItem_Click()
        {
            mOperations.Delete();
        }

        void RenameMenuItem_Click()
        {
            mOperations.Rename();
        }

        void ImportInProjectMenuItem_Click()
        {
            mOperations.ImportInProject();
        }

        void UpdateMenuItems(GenericMenu menu)
        {
            int selectedCount = mOperations.GetSelectedItemsCount();
            bool isAnyFileSelected = mOperations.IsAnyFileSelected();
            bool isPathSelected = mOperations.IsPathSelected();

            DirectoryContentMenuOperations operations =
                DirectoryContentMenuUpdater.GetAvailableMenuOperations(
                    selectedCount, isAnyFileSelected, isPathSelected);

            AddMenuItem(
                mCreateFolderMenuItemContent,
                menu,
                operations,
                DirectoryContentMenuOperations.CreateFolder,
                CreateFolderMenuItem_Click);

            AddMenuItem(
                mOpenInExplorerMenuItemContent,
                menu,
                operations,
                DirectoryContentMenuOperations.OpenInExplorer,
                OpenInExplorerMenuItem_Click);

            AddMenuItem(
                mOpenUnityCloudMenuItemContent,
                menu,
                operations,
                DirectoryContentMenuOperations.OpenUnityCloud,
                OpenUnityCloudMenuItem_Click);

            AddMenuItem(
                mOpenMenuItemContent,
                menu,
                operations,
                DirectoryContentMenuOperations.Open,
                OpenMenuItem_Click);

            AddMenuItem(
                mDeleteMenuItemContent,
                menu,
                operations,
                DirectoryContentMenuOperations.Delete,
                DeleteMenuItem_Click);

            AddMenuItem(
                mRenameMenuItemContent,
                menu,
                operations,
                DirectoryContentMenuOperations.Rename,
                RenameMenuItem_Click);

            menu.AddSeparator(string.Empty);

            AddMenuItem(
                mImportInProjectMenuItemContent,
                menu,
                operations,
                DirectoryContentMenuOperations.ImportInProject,
                ImportInProjectMenuItem_Click);
        }

        static void AddMenuItem(
            GUIContent menuItemContent,
            GenericMenu menu,
            DirectoryContentMenuOperations operations,
            DirectoryContentMenuOperations operationsToCheck,
            GenericMenu.MenuFunction menuFunction)
        {
            if (operations.HasFlag(operationsToCheck))
            {
                menu.AddItem(menuItemContent, false, menuFunction);
                return;
            }

            menu.AddDisabledItem(menuItemContent);
        }

        void ProcessMenuOperation(DirectoryContentMenuOperations operationToExecute)
        {
            if (operationToExecute == DirectoryContentMenuOperations.OpenInExplorer)
            {
                OpenInExplorerMenuItem_Click();
                return;
            }

            if (operationToExecute == DirectoryContentMenuOperations.Open)
            {
                OpenMenuItem_Click();
                return;
            }

            if (operationToExecute == DirectoryContentMenuOperations.Delete)
            {
                DeleteMenuItem_Click();
                return;
            }

            if (operationToExecute == DirectoryContentMenuOperations.Rename)
            {
                RenameMenuItem_Click();
                return;
            }
        }

        static DirectoryContentMenuOperations GetMenuOperation(Event e)
        {
            if (Keyboard.HasControlOrCommandModifier(e) &&
                Keyboard.HasShiftModifier(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.S))
            {
                return DirectoryContentMenuOperations.OpenInExplorer;
            }

            if (Keyboard.IsReturnOrEnterKeyPressed(e))
                return DirectoryContentMenuOperations.Open;

            if (Keyboard.IsDeleteKeyPressed(e))
                return DirectoryContentMenuOperations.Delete;

            if (Keyboard.IsKeyPressed(e, KeyCode.F2))
                return DirectoryContentMenuOperations.Rename;

            return DirectoryContentMenuOperations.None;
        }

        void BuildComponents()
        {
            mCreateFolderMenuItemContent =
                new GUIContent(PlasticLocalization.Name.CreateFolderMenuItem.GetString());

            mOpenInExplorerMenuItemContent = new GUIContent(
                string.Format("{0} {1}",
                    PlatformIdentifier.IsMac() ?
                        PlasticLocalization.Name.ItemsMenuItemRevealInFinder.GetString() :
                        PlasticLocalization.Name.ShowInExplorerMenuItem.GetString(),
                    GetPlasticShortcut.ForShowInExplorer()));

            mOpenUnityCloudMenuItemContent = new GUIContent(
                PlasticLocalization.Name.OpenUnityDashboard.GetString());

            mOpenMenuItemContent = new GUIContent(
                string.Format("{0} {1}",
                    PlasticLocalization.Name.ItemsMenuItemOpen.GetString(),
                    GetPlasticShortcut.ForEnter()));

            mDeleteMenuItemContent = new GUIContent(
                string.Format("{0} {1}",
                    PlasticLocalization.Name.ItemsMenuItemDelete.GetString(),
                    GetPlasticShortcut.ForDelete()));

            mRenameMenuItemContent = new GUIContent(
                string.Format("{0} {1}",
                    PlasticLocalization.Name.ItemsMenuItemRename.GetString(),
                    GetPlasticShortcut.ForRename()));

            mImportInProjectMenuItemContent = new GUIContent(
                PlasticLocalization.Name.ImportInProjectMenuItem.GetString());
        }

        GUIContent mCreateFolderMenuItemContent;
        GUIContent mOpenInExplorerMenuItemContent;
        GUIContent mOpenUnityCloudMenuItemContent;
        GUIContent mOpenMenuItemContent;
        GUIContent mDeleteMenuItemContent;
        GUIContent mRenameMenuItemContent;
        GUIContent mImportInProjectMenuItemContent;

        readonly IDirectoryContentMenuOperations mOperations;
    }
}
