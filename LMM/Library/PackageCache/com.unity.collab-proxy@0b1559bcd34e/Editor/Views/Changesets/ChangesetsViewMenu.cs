using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Changesets;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal class ChangesetsViewMenu
    {
        internal GenericMenu Menu { get { return mMenu; } }

        public interface IMenuOperations
        {
            void DiffBranch();
            ChangesetInfo GetSelectedChangeset();
        }

        internal ChangesetsViewMenu(
            IChangesetMenuOperations changesetMenuOperations,
            IMenuOperations menuOperations,
            IGetWorkingObject getWorkingObject,
            bool isGluonMode)
        {
            mChangesetMenuOperations = changesetMenuOperations;
            mMenuOperations = menuOperations;
            mGetWorkingObject = getWorkingObject;
            mIsGluonMode = isGluonMode;

            BuildComponents();
        }

        internal void Popup()
        {
            mMenu = new GenericMenu();

            UpdateMenuItems(mMenu);

            mMenu.ShowAsContext();
        }

        internal bool ProcessKeyActionIfNeeded(Event e)
        {
            int selectedChangesetsCount = mChangesetMenuOperations.GetSelectedChangesetsCount();

            ChangesetMenuOperations operationToExecute = GetMenuOperations(
                e, selectedChangesetsCount > 1);

            if (operationToExecute == ChangesetMenuOperations.None)
                return false;

            ChangesetMenuOperations operations = ChangesetMenuUpdater.GetAvailableMenuOperations(
                selectedChangesetsCount,
                mIsGluonMode,
                mMenuOperations.GetSelectedChangeset().BranchId,
                GetWorkingBranchId(mGetWorkingObject, mIsGluonMode),
                false);

            if (!operations.HasFlag(operationToExecute))
                return false;

            ProcessMenuOperation(operationToExecute);
            return true;
        }

        void DiffChangesetMenuItem_Click()
        {
            mChangesetMenuOperations.DiffChangeset();
        }

        void DiffSelectedChangesetsMenuItem_Click()
        {
            mChangesetMenuOperations.DiffSelectedChangesets();
        }

        void RevertToChangesetMenuItem_Click()
        {
            mChangesetMenuOperations.RevertToChangeset();
        }

        void DiffBranchMenuItem_Click()
        {
            mMenuOperations.DiffBranch();
        }

        void CreateBranchFromChangesetMenuItem_Click()
        {
            mChangesetMenuOperations.CreateBranch();
        }

        void LabelChangesetMenuItem_Click()
        {
            mChangesetMenuOperations.LabelChangeset();
        }

        void SwitchToChangesetMenuItem_Click()
        {
            mChangesetMenuOperations.SwitchToChangeset();
        }

        void MergeChangesetMenuItem_Click()
        {
            mChangesetMenuOperations.MergeChangeset();
        }

        void CreateCodeReviewMenuItem_Click()
        {
            mChangesetMenuOperations.CreateCodeReview();
        }

        void UpdateMenuItems(GenericMenu menu)
        {
            ChangesetInfo singleSelectedChangeset = mMenuOperations.GetSelectedChangeset();

            ChangesetMenuOperations operations = ChangesetMenuUpdater.GetAvailableMenuOperations(
                mChangesetMenuOperations.GetSelectedChangesetsCount(),
                mIsGluonMode,
                singleSelectedChangeset.BranchId,
                GetWorkingBranchId(mGetWorkingObject, mIsGluonMode),
                false);

            AddDiffChangesetMenuItem(
                mDiffChangesetMenuItemContent,
                menu,
                singleSelectedChangeset,
                operations,
                DiffChangesetMenuItem_Click);

            AddChangesetsMenuItem(
                mDiffSelectedChangesetsMenuItemContent,
                menu,
                operations,
                ChangesetMenuOperations.DiffSelectedChangesets,
                DiffSelectedChangesetsMenuItem_Click);

            if (singleSelectedChangeset is ChangesetExtendedInfo &&
                !IsOnMainBranch((ChangesetExtendedInfo)singleSelectedChangeset))
            {
                menu.AddSeparator(string.Empty);

                AddDiffBranchMenuItem(
                    mDiffBranchMenuItemContent,
                    menu,
                    (ChangesetExtendedInfo)singleSelectedChangeset,
                    operations,
                    DiffBranchMenuItem_Click);
            }

            menu.AddSeparator(string.Empty);

            AddChangesetsMenuItem(
                mCreateBranchMenuItemContent,
                menu,
                operations,
                ChangesetMenuOperations.CreateBranch,
                CreateBranchFromChangesetMenuItem_Click);

            if (!mIsGluonMode)
            {
                AddChangesetsMenuItem(
                    mLabelChangesetMenuItemContent,
                    menu,
                    operations,
                    ChangesetMenuOperations.LabelChangeset,
                    LabelChangesetMenuItem_Click);
            }

            AddChangesetsMenuItem(
                mSwitchToChangesetMenuItemContent,
                menu,
                operations,
                ChangesetMenuOperations.SwitchToChangeset,
                SwitchToChangesetMenuItem_Click);

            if (!mIsGluonMode)
            {
                AddChangesetsMenuItem(
                    mRevertToChangesetMenuItemContent,
                    menu,
                    operations,
                    ChangesetMenuOperations.RevertToChangeset,
                    RevertToChangesetMenuItem_Click);

                menu.AddSeparator(string.Empty);

                AddChangesetsMenuItem(
                    mMergeChangesetMenuItemContent,
                    menu,
                    operations,
                    ChangesetMenuOperations.MergeChangeset,
                    MergeChangesetMenuItem_Click);
            }

            menu.AddSeparator(string.Empty);

            AddChangesetsMenuItem(
                mCreateCodeReviewMenuItemContent,
                menu,
                operations,
                ChangesetMenuOperations.CreateCodeReview,
                CreateCodeReviewMenuItem_Click);
        }

        void ProcessMenuOperation(
            ChangesetMenuOperations operationToExecute)
        {
            if (operationToExecute == ChangesetMenuOperations.DiffChangeset)
            {
                DiffChangesetMenuItem_Click();
                return;
            }

            if (operationToExecute == ChangesetMenuOperations.DiffSelectedChangesets)
            {
                DiffSelectedChangesetsMenuItem_Click();
                return;
            }

            if (operationToExecute == ChangesetMenuOperations.LabelChangeset)
            {
                LabelChangesetMenuItem_Click();
                return;
            }

            if (operationToExecute == ChangesetMenuOperations.MergeChangeset)
            {
                MergeChangesetMenuItem_Click();
                return;
            }
        }

        static void AddChangesetsMenuItem(
            GUIContent menuItemContent,
            GenericMenu menu,
            ChangesetMenuOperations operations,
            ChangesetMenuOperations operationsToCheck,
            GenericMenu.MenuFunction menuFunction)
        {
            if (operations.HasFlag(operationsToCheck))
            {
                menu.AddItem(
                    menuItemContent,
                    false,
                    menuFunction);

                return;
            }

            menu.AddDisabledItem(menuItemContent);
        }

        static void AddDiffChangesetMenuItem(
            GUIContent menuItemContent,
            GenericMenu menu,
            ChangesetInfo changeset,
            ChangesetMenuOperations operations,
            GenericMenu.MenuFunction menuFunction)
        {
            string changesetName =
                changeset != null ?
                changeset.ChangesetId.ToString() :
                string.Empty;

            menuItemContent.text = string.Format("{0} {1}",
                    PlasticLocalization.GetString(
                        PlasticLocalization.Name.DiffChangesetMenuItem,
                        changesetName),
                    GetPlasticShortcut.ForDiff());

            if (operations.HasFlag(ChangesetMenuOperations.DiffChangeset))
            {
                menu.AddItem(
                    menuItemContent,
                    false,
                    menuFunction);
                return;
            }

            menu.AddDisabledItem(
                menuItemContent);
        }

        static void AddDiffBranchMenuItem(
            GUIContent menuItemContent,
            GenericMenu menu,
            ChangesetExtendedInfo changeset,
            ChangesetMenuOperations operations,
            GenericMenu.MenuFunction menuFunction)
        {
            menuItemContent.text =
                PlasticLocalization.Name.DiffBranchMenuItem.GetString(
                    GetShorten.BranchNameFromChangeset(changeset));

            if (operations.HasFlag(ChangesetMenuOperations.DiffChangeset))
            {
                menu.AddItem(
                    menuItemContent,
                    false,
                    menuFunction);
                return;
            }

            menu.AddDisabledItem(
                menuItemContent);
        }

        static bool IsOnMainBranch(ChangesetExtendedInfo singleSeletedChangeset)
        {
            if (singleSeletedChangeset == null)
                return false;

            return singleSeletedChangeset.BranchName == MAIN_BRANCH_NAME;
        }

        static long GetWorkingBranchId(IGetWorkingObject getWorkingObject, bool isGluonMode)
        {
            if (isGluonMode)
                return -1;

            return ((ChangesetInfo)getWorkingObject.Get()).BranchId;
        }

        static ChangesetMenuOperations GetMenuOperations(
            Event e, bool isMultipleSelection)
        {
            if (Keyboard.IsControlOrCommandKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.D))
                return isMultipleSelection ?
                    ChangesetMenuOperations.DiffSelectedChangesets :
                    ChangesetMenuOperations.DiffChangeset;

            if (Keyboard.IsControlOrCommandKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.L))
                return ChangesetMenuOperations.LabelChangeset;

            if (Keyboard.IsControlOrCommandKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.M))
                return ChangesetMenuOperations.MergeChangeset;

            return ChangesetMenuOperations.None;
        }

        void BuildComponents()
        {
            mDiffChangesetMenuItemContent = new GUIContent(string.Empty);
            mDiffSelectedChangesetsMenuItemContent = new GUIContent(string.Format("{0} {1}",
                PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetMenuItemDiffSelected),
                GetPlasticShortcut.ForDiff()));
            mDiffBranchMenuItemContent = new GUIContent();
            mCreateBranchMenuItemContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetMenuItemCreateBranch));
            mLabelChangesetMenuItemContent = new GUIContent(string.Format("{0} {1}",
                PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetMenuItemLabelChangeset),
                GetPlasticShortcut.ForLabel()));
            mSwitchToChangesetMenuItemContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetMenuItemSwitchToChangeset));
            mRevertToChangesetMenuItemContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetMenuItemRevertToChangeset));
            mMergeChangesetMenuItemContent = new GUIContent(string.Format("{0} {1}",
                PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetMenuItemMergeFromChangeset),
                GetPlasticShortcut.ForMerge()));
            mCreateCodeReviewMenuItemContent = new GUIContent(
                PlasticLocalization.Name.ChangesetMenuCreateANewCodeReview.GetString());
        }

        GenericMenu mMenu;

        GUIContent mDiffChangesetMenuItemContent;
        GUIContent mDiffSelectedChangesetsMenuItemContent;
        GUIContent mDiffBranchMenuItemContent;
        GUIContent mCreateBranchMenuItemContent;
        GUIContent mLabelChangesetMenuItemContent;
        GUIContent mSwitchToChangesetMenuItemContent;
        GUIContent mRevertToChangesetMenuItemContent;
        GUIContent mMergeChangesetMenuItemContent;
        GUIContent mCreateCodeReviewMenuItemContent;

        readonly IChangesetMenuOperations mChangesetMenuOperations;
        readonly IMenuOperations mMenuOperations;
        readonly IGetWorkingObject mGetWorkingObject;
        readonly bool mIsGluonMode;

        const string MAIN_BRANCH_NAME = "/main";
    }
}
