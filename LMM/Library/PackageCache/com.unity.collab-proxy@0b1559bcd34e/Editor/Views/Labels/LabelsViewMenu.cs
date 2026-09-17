using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Labels;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Labels
{
    internal class LabelsViewMenu
    {
        internal GenericMenu Menu { get { return mMenu; } }

        internal static LabelsViewMenu BuildForSubMenu(
            ILabelMenuOperations labelMenuOperations,
            string menuName)
        {
            return new LabelsViewMenu(labelMenuOperations, menuName);
        }

        internal static LabelsViewMenu BuildForContextMenu(ILabelMenuOperations labelMenuOperations)
        {
            return new LabelsViewMenu(labelMenuOperations, string.Empty);
        }

        LabelsViewMenu(ILabelMenuOperations labelMenuOperations, string menuName)
        {
            mLabelMenuOperations = labelMenuOperations;
            mMenuName = menuName;

            BuildComponents(menuName);
        }

        internal void Popup()
        {
            mMenu = new GenericMenu();

            UpdateMenuItems(mMenu);

            mMenu.ShowAsContext();
        }

        internal bool ProcessKeyActionIfNeeded(Event e)
        {
            LabelMenuOperations operationToExecute = GetMenuOperations(e);

            if (operationToExecute == LabelMenuOperations.None)
                return false;

            LabelMenuOperations operations = LabelMenuUpdater.GetAvailableMenuOperations(
                mLabelMenuOperations.GetSelectedLabelsCount(),
                false);

            if (!operations.HasFlag(operationToExecute))
                return false;

            ProcessMenuOperation(operationToExecute, mLabelMenuOperations);
            return true;
        }

        void CreateLabelMenuItem_Click()
        {
            mLabelMenuOperations.CreateLabel();
        }

        void ApplyLabelToWorkspaceMenuItem_Click()
        {
            mLabelMenuOperations.ApplyLabelToWorkspace();
        }

        void SwitchWorkspaceToLabelMenuItem_Click()
        {
            mLabelMenuOperations.SwitchToLabel();
        }

        void DiffSelectedLabelsMenuItem_Click()
        {
            mLabelMenuOperations.DiffSelectedLabels();
        }

        void CreateBranchFromLabelMenu_Click()
        {
            mLabelMenuOperations.CreateBranchFromLabel();
        }

        void MergeFromLabelMenuItem_Click()
        {
            mLabelMenuOperations.MergeLabel();
        }

        void RenameLabelMenuItem_Click()
        {
            mLabelMenuOperations.RenameLabel();
        }

        void DeleteLabelMenuItem_Click()
        {
            mLabelMenuOperations.DeleteLabel();
        }

        internal void UpdateMenuItems(GenericMenu menu)
        {
            LabelMenuOperations operations = LabelMenuUpdater.GetAvailableMenuOperations(
                mLabelMenuOperations.GetSelectedLabelsCount(),
                false);

            AddLabelMenuItem(
                mCreateLabelMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.CreateLabel,
                CreateLabelMenuItem_Click);

            menu.AddSeparator(UnityMenuItem.GetText(mMenuName, string.Empty));

            AddLabelMenuItem(
                mApplyLabelToWorkspaceMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.ApplyLabelToWorkspace,
                ApplyLabelToWorkspaceMenuItem_Click);

            AddLabelMenuItem(
                mSwitchWorkspaceToLabelMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.SwitchToLabel,
                SwitchWorkspaceToLabelMenuItem_Click);

            menu.AddSeparator(UnityMenuItem.GetText(mMenuName, string.Empty));

            AddLabelMenuItem(
                mDiffSelectedLabelsMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.DiffSelectedLabels,
                DiffSelectedLabelsMenuItem_Click);

            menu.AddSeparator(UnityMenuItem.GetText(mMenuName, string.Empty));

            AddLabelMenuItem(
                mMergeFromLabelMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.MergeLabel,
                MergeFromLabelMenuItem_Click);

            AddLabelMenuItem(
                mCreateBranchFromLabelMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.CreateBranch,
                CreateBranchFromLabelMenu_Click);

            menu.AddSeparator(UnityMenuItem.GetText(mMenuName, string.Empty));

            AddLabelMenuItem(
                mRenameLabelMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.Rename,
                RenameLabelMenuItem_Click);

            AddLabelMenuItem(
                mDeleteLabelMenuItemContent,
                menu,
                operations,
                LabelMenuOperations.Delete,
                DeleteLabelMenuItem_Click);
        }

        static void AddLabelMenuItem(
            GUIContent menuItemContent,
            GenericMenu menu,
            LabelMenuOperations operations,
            LabelMenuOperations operationsToCheck,
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

        static void ProcessMenuOperation(
            LabelMenuOperations operationToExecute,
            ILabelMenuOperations labelMenuOperations)
        {
            if (operationToExecute == LabelMenuOperations.ApplyLabelToWorkspace)
            {
                labelMenuOperations.ApplyLabelToWorkspace();
                return;
            }

            if (operationToExecute == LabelMenuOperations.SwitchToLabel)
            {
                labelMenuOperations.SwitchToLabel();
                return;
            }

            if (operationToExecute == LabelMenuOperations.DiffSelectedLabels)
            {
                labelMenuOperations.DiffSelectedLabels();
                return;
            }

            if (operationToExecute == LabelMenuOperations.MergeLabel)
            {
                labelMenuOperations.MergeLabel();
                return;
            }

            if (operationToExecute == LabelMenuOperations.Rename)
            {
                labelMenuOperations.RenameLabel();
                return;
            }

            if (operationToExecute == LabelMenuOperations.Delete)
            {
                labelMenuOperations.DeleteLabel();
                return;
            }
        }

        static LabelMenuOperations GetMenuOperations(Event e)
        {
            if (Keyboard.IsControlOrCommandKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.L))
                return LabelMenuOperations.ApplyLabelToWorkspace;

            if (Keyboard.IsControlOrCommandAndShiftKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.W))
                return LabelMenuOperations.SwitchToLabel;

            if (Keyboard.IsControlOrCommandKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.D))
                return LabelMenuOperations.DiffSelectedLabels;

            if (Keyboard.IsControlOrCommandKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.M))
                return LabelMenuOperations.MergeLabel;

            if (Keyboard.IsKeyPressed(e, KeyCode.Delete))
                return LabelMenuOperations.Delete;

            if (Keyboard.IsKeyPressed(e, KeyCode.F2))
                return LabelMenuOperations.Rename;

            return LabelMenuOperations.None;
        }

        void BuildComponents(string menuName)
        {
            mCreateLabelMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    PlasticLocalization.Name.LabelMenuItemCreateLabel.GetString()));

            mApplyLabelToWorkspaceMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    string.Format("{0} {1}",
                        PlasticLocalization.Name.LabelMenuItemApplyLabelToWorkspace.GetString(),
                        GetPlasticShortcut.ForLabel())));

            mSwitchWorkspaceToLabelMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    string.Format("{0} {1}",
                        PlasticLocalization.Name.LabelMenuItemSwitchToLabel.GetString(),
                        GetPlasticShortcut.ForSwitch())));

            mDiffSelectedLabelsMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    string.Format("{0} {1}",
                        PlasticLocalization.Name.LabelMenuItemDiffSelected.GetString(),
                        GetPlasticShortcut.ForDiff())));

            mMergeFromLabelMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    string.Format("{0} {1}",
                        PlasticLocalization.Name.LabelMenuItemMergeFromLabel.GetString(),
                        GetPlasticShortcut.ForMerge())));

            mCreateBranchFromLabelMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    PlasticLocalization.Name.LabelMenuItemCreateBranchFromLabel.GetString()));

            mRenameLabelMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    string.Format("{0} {1}",
                        PlasticLocalization.Name.LabelMenuItemRenameLabel.GetString(),
                        GetPlasticShortcut.ForRename())));

            mDeleteLabelMenuItemContent = new GUIContent(
                UnityMenuItem.GetText(
                    menuName,
                    string.Format("{0} {1}",
                        PlasticLocalization.Name.LabelMenuItemDeleteLabel.GetString(),
                        GetPlasticShortcut.ForDelete())));
        }

        GenericMenu mMenu;

        GUIContent mCreateLabelMenuItemContent;
        GUIContent mApplyLabelToWorkspaceMenuItemContent;
        GUIContent mSwitchWorkspaceToLabelMenuItemContent;
        GUIContent mDiffSelectedLabelsMenuItemContent;
        GUIContent mMergeFromLabelMenuItemContent;
        GUIContent mCreateBranchFromLabelMenuItemContent;
        GUIContent mRenameLabelMenuItemContent;
        GUIContent mDeleteLabelMenuItemContent;

        readonly string mMenuName;
        readonly ILabelMenuOperations mLabelMenuOperations;
    }
}
