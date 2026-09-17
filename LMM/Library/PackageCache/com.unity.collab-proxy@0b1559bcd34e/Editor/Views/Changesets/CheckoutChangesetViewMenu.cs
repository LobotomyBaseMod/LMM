using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Changesets;

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal class CheckoutChangesetViewMenu
    {
        internal CheckoutChangesetViewMenu(
            ICheckoutChangesetMenuOperations checkoutChangesetsViewMenuOperations)
        {
            mCheckoutChangesetsViewMenuOperations = checkoutChangesetsViewMenuOperations;

            BuildComponents();
        }

        internal void Popup()
        {
            mMenu = new GenericMenu();

            UpdateMenuItems(mMenu);

            mMenu.ShowAsContext();
        }

        void ShowPendingChanges_Click()
        {
            mCheckoutChangesetsViewMenuOperations.ShowPendingChangesView();
        }

        void UpdateMenuItems(GenericMenu menu)
        {
            menu.AddItem(
                mShowPendingChangesViewMenuItemContent,
                false,
                ShowPendingChanges_Click);
        }

        void BuildComponents()
        {
            mShowPendingChangesViewMenuItemContent = new GUIContent(
                PlasticLocalization.Name.CheckoutChangesetMenuItemShowPendingChangesView.GetString());
        }

        GenericMenu mMenu;

        GUIContent mShowPendingChangesViewMenuItemContent;

        readonly ICheckoutChangesetMenuOperations mCheckoutChangesetsViewMenuOperations;
    }
}
