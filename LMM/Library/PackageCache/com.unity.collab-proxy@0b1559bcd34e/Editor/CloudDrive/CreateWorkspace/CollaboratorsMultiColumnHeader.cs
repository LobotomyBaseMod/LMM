using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace
{
    internal class CollaboratorsMultiColumnHeader : MultiColumnHeader
    {
        internal CollaboratorsMultiColumnHeader(
            CollaboratorsListView listView,
            MultiColumnHeaderState headerState)
            : base(headerState)
        {
            mCollaboratorsListView = listView;
        }

        protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            if (columnIndex == 0)
            {
                bool isAnyItemChecked = mCollaboratorsListView.IsAnyItemChecked();
                bool areAllItemsChecked = mCollaboratorsListView.AreAllItemsChecked();

                var checkRect = new Rect(
                    headerRect.x + UnityConstants.TREEVIEW_BASE_INDENT,
                    headerRect.y + 3 + UnityConstants.TREEVIEW_HEADER_CHECKBOX_Y_OFFSET,  // Custom offset because header labels are not centered
                    UnityConstants.TREEVIEW_CHECKBOX_SIZE,
                    headerRect.height);

                EditorGUI.showMixedValue = !areAllItemsChecked && isAnyItemChecked;
                bool toggleValue = EditorGUI.Toggle(
                    checkRect,
                    areAllItemsChecked);
                EditorGUI.showMixedValue = false;

                if (toggleValue != areAllItemsChecked)
                    UpdateCheckedState(toggleValue);

                headerRect.x = checkRect.xMax;
                headerRect.xMax = column.width;
            }
            base.ColumnHeaderGUI(column, headerRect, columnIndex);
        }

        void UpdateCheckedState(bool isChecked)
        {
            if (isChecked)
                mCollaboratorsListView.CheckAllItems();
            else
                mCollaboratorsListView.UnCheckAllItems();
        }

        readonly CollaboratorsListView mCollaboratorsListView;
    }
}
