using System;

using UnityEditor;
using UnityEngine;

using Codice.Utils;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.Workspaces.Tree
{
    [Flags]
    internal enum CloudWorkspacesTreeOperations : short
    {
        None = 0,
        OpenInExplorer = 1 << 0,
        OpenUnityCloud = 1 << 1,
        Delete = 1 << 2,
        Share = 1 << 3,
        UnshareWithMe = 1 << 4,
    }

    internal static class CloudWorkspacesTreeMenuUpdater
    {
        internal static CloudWorkspacesTreeOperations GetAvailableMenuOperations(
            int selectedItemsCount,
            bool isAnyNonRootItemSelected,
            bool isAnySharedDriveSelected)
        {
            CloudWorkspacesTreeOperations result = CloudWorkspacesTreeOperations.None;

            if (selectedItemsCount != 1)
                return result;

            result |= CloudWorkspacesTreeOperations.OpenInExplorer;

            if (isAnyNonRootItemSelected)
                return result;

            result |= CloudWorkspacesTreeOperations.Delete |
                      CloudWorkspacesTreeOperations.OpenUnityCloud;

            if (isAnySharedDriveSelected)
                result |= CloudWorkspacesTreeOperations.UnshareWithMe;
            else
                result |= CloudWorkspacesTreeOperations.Share;

            return result;
        }
    }

    internal interface ICloudWorkspacesTreeMenuOperations
    {
        int GetSelectedItemsCount();
        bool IsAnyNonRootItemSelected();
        bool IsAnySharedDriveSelected();

        void OpenInExplorer();
        void OpenUnityCloud();
        void Delete();
        void Share();
        void UnshareWithMe();
    }

    internal class CloudWorkspacesTreeViewMenu
    {
        internal CloudWorkspacesTreeViewMenu(ICloudWorkspacesTreeMenuOperations operations)
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
            CloudWorkspacesTreeOperations operationToExecute = GetMenuOperation(e);

            if (operationToExecute == CloudWorkspacesTreeOperations.None)
                return false;

            int selectedCount = mOperations.GetSelectedItemsCount();
            bool isAnyNonRootItemSelected = mOperations.IsAnyNonRootItemSelected();
            bool isAnySharedDriveSelected = mOperations.IsAnySharedDriveSelected();

            CloudWorkspacesTreeOperations operations =
                CloudWorkspacesTreeMenuUpdater.GetAvailableMenuOperations(
                    selectedCount, isAnyNonRootItemSelected, isAnySharedDriveSelected);

            if (!operations.HasFlag(operationToExecute))
                return false;

            ProcessMenuOperation(operationToExecute);
            return true;
        }

        void OpenInExplorerMenuItem_Click()
        {
            mOperations.OpenInExplorer();
        }

        void OpenUnityCloud_Click()
        {
            mOperations.OpenUnityCloud();
        }

        void DeleteMenuItem_Click()
        {
            mOperations.Delete();
        }

        void ShareMenuItem_Click()
        {
            mOperations.Share();
        }

        void UnshareWithMeMenuItem_Click()
        {
            mOperations.UnshareWithMe();
        }

        void UpdateMenuItems(GenericMenu menu)
        {
            int selectedCount = mOperations.GetSelectedItemsCount();
            bool isAnyNonRootItemSelected = mOperations.IsAnyNonRootItemSelected();
            bool isAnySharedDriveSelected = mOperations.IsAnySharedDriveSelected();

            CloudWorkspacesTreeOperations operations =
                CloudWorkspacesTreeMenuUpdater.GetAvailableMenuOperations(
                    selectedCount, isAnyNonRootItemSelected, isAnySharedDriveSelected);

            AddMenuItem(
                mOpenInExplorerMenuItemContent,
                menu,
                operations,
                CloudWorkspacesTreeOperations.OpenInExplorer,
                OpenInExplorerMenuItem_Click);

            AddMenuItem(
                mOpenUnityCloudMenuItemContent,
                menu,
                operations,
                CloudWorkspacesTreeOperations.OpenUnityCloud,
                OpenUnityCloud_Click);

            AddMenuItem(
                mDeleteMenuItemContent,
                menu,
                operations,
                CloudWorkspacesTreeOperations.Delete,
                DeleteMenuItem_Click);

            if (!operations.HasFlag(CloudWorkspacesTreeOperations.Share) &&
                !operations.HasFlag(CloudWorkspacesTreeOperations.UnshareWithMe))
            {
                return;
            }

            menu.AddSeparator(string.Empty);

            if (operations.HasFlag(CloudWorkspacesTreeOperations.Share))
            {
                AddMenuItem(
                    mShareMenuItemContent,
                    menu,
                    operations,
                    CloudWorkspacesTreeOperations.Share,
                    ShareMenuItem_Click);
            }

            if (operations.HasFlag(CloudWorkspacesTreeOperations.UnshareWithMe))
            {
                AddMenuItem(
                    mUnshareWithMeMenuItemContent,
                    menu,
                    operations,
                    CloudWorkspacesTreeOperations.UnshareWithMe,
                    UnshareWithMeMenuItem_Click);
            }
        }

        static void AddMenuItem(
            GUIContent menuItemContent,
            GenericMenu menu,
            CloudWorkspacesTreeOperations operations,
            CloudWorkspacesTreeOperations operationsToCheck,
            GenericMenu.MenuFunction menuFunction)
        {
            if (operations.HasFlag(operationsToCheck))
            {
                menu.AddItem(menuItemContent, false, menuFunction);
                return;
            }

            menu.AddDisabledItem(menuItemContent);
        }

        void ProcessMenuOperation(CloudWorkspacesTreeOperations operationToExecute)
        {
            if (operationToExecute == CloudWorkspacesTreeOperations.OpenInExplorer)
            {
                OpenInExplorerMenuItem_Click();
                return;
            }

            if (operationToExecute == CloudWorkspacesTreeOperations.Delete)
            {
                DeleteMenuItem_Click();
                return;
            }
        }

        static CloudWorkspacesTreeOperations GetMenuOperation(Event e)
        {
            if (Keyboard.HasControlOrCommandModifier(e) &&
                Keyboard.HasShiftModifier(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.S))
            {
                return CloudWorkspacesTreeOperations.OpenInExplorer;
            }

            if (Keyboard.IsDeleteKeyPressed(e))
                return CloudWorkspacesTreeOperations.Delete;

            return CloudWorkspacesTreeOperations.None;
        }

        void BuildComponents()
        {
            mOpenInExplorerMenuItemContent = new GUIContent(
                string.Format("{0} {1}",
                    PlatformIdentifier.IsMac() ?
                        PlasticLocalization.Name.ItemsMenuItemRevealInFinder.GetString() :
                        PlasticLocalization.Name.ShowInExplorerMenuItem.GetString(),
                    GetPlasticShortcut.ForShowInExplorer()));

            mOpenUnityCloudMenuItemContent = new GUIContent(
                PlasticLocalization.Name.OpenUnityDashboard.GetString());

            mDeleteMenuItemContent = new GUIContent(
                string.Format("{0} {1}",
                    PlasticLocalization.Name.ItemsMenuItemDelete.GetString(),
                    GetPlasticShortcut.ForDelete()));

            mShareMenuItemContent = new GUIContent(
                PlasticLocalization.Name.ShareMenuItem.GetString());

            mUnshareWithMeMenuItemContent = new GUIContent(
                PlasticLocalization.Name.UnshareWithMeMenuItem.GetString());
        }

        GUIContent mOpenInExplorerMenuItemContent;
        GUIContent mOpenUnityCloudMenuItemContent;
        GUIContent mDeleteMenuItemContent;
        GUIContent mShareMenuItemContent;
        GUIContent mUnshareWithMeMenuItemContent;

        readonly ICloudWorkspacesTreeMenuOperations mOperations;
    }
}
