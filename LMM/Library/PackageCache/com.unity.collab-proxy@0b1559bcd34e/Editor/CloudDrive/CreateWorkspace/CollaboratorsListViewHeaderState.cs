using System;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.CloudDrive.CreateWorkspace
{
    internal enum CollaboratorsListColumn
    {
        User,
    }

    [Serializable]
    internal class CollaboratorsListViewHeaderState : MultiColumnHeaderState, ISerializationCallbackReceiver
    {
        internal static CollaboratorsListViewHeaderState GetDefault()
        {
            return new CollaboratorsListViewHeaderState(BuildColumns());
        }

        static string GetColumnName(CollaboratorsListColumn column)
        {
            switch (column)
            {
                case CollaboratorsListColumn.User:
                    return PlasticLocalization.Name.NameColumn.GetString();
                default:
                    return null;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (mHeaderTitles != null)
                TreeHeaderColumns.SetTitles(columns, mHeaderTitles);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        static Column[] BuildColumns()
        {
            return new Column[]
                {
                    new Column()
                    {
                        width = 800,
                        headerContent = new GUIContent(
                            GetColumnName(CollaboratorsListColumn.User)),
                        minWidth = 200,
                        allowToggleVisibility = false,
                        canSort = true,
                        sortingArrowAlignment = TextAlignment.Right
                    }
                };
        }

        CollaboratorsListViewHeaderState(Column[] columns)
            : base(columns)
        {
            if (mHeaderTitles == null)
                mHeaderTitles = TreeHeaderColumns.GetTitles(columns);
        }

        [SerializeField]
        string[] mHeaderTitles;
    }
}
