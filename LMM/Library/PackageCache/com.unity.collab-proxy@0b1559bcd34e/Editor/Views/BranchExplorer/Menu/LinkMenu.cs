using PlasticGui;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu
{
    internal class LinkMenu
    {
        internal GenericMenu Menu { get { return mMenu; } }

        internal LinkMenu(ILinkMenuOperations linkOperations)
        {
            mLinkMenuOperations = linkOperations;

            BuildComponents();
        }

        internal void Popup()
        {
            mMenu = new GenericMenu();

            UpdateMenuItems(mMenu);

            mMenu.ShowAsContext();
        }

        void UpdateMenuItems(GenericMenu menu)
        {
            int selectedLinksCount = mLinkMenuOperations.GetSelectedLinksCount();

            if (selectedLinksCount != 1)
                return;

            menu.AddItem(
                mGoToSourceChangesetMenuItemContent,
                false,
                GoToSourceChangeset_Click);

            menu.AddItem(
                mGoToDestinationChangesetMenuItemContent,
                false,
                GoToDestinationChangeset_Click);
        }

        void GoToSourceChangeset_Click()
        {
            mLinkMenuOperations.GoToSourcechangeset();
        }

        void GoToDestinationChangeset_Click()
        {
            mLinkMenuOperations.GoToDestinationChangeset();
        }

        void BuildComponents()
        {
            mGoToSourceChangesetMenuItemContent = new GUIContent(
                PlasticLocalization.Name.GoToSourceChangesetMenuItem.GetString());

            mGoToDestinationChangesetMenuItemContent = new GUIContent(
                PlasticLocalization.Name.GoToDestinationChangesetMenuItem.GetString());
        }

        GenericMenu mMenu;

        GUIContent mGoToSourceChangesetMenuItemContent;
        GUIContent mGoToDestinationChangesetMenuItemContent;

        readonly ILinkMenuOperations mLinkMenuOperations;
    }
}
