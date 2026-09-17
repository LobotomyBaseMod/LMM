using System;
using System.Collections.Generic;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Views.Shelves
{
    internal enum ShelvesListColumn
    {
        Comment,
        CreationDate,
        CreatedBy,
        Name
    }

    [Serializable]
    internal class ShelvesListHeaderState : MultiColumnHeaderState, ISerializationCallbackReceiver
    {
        internal static ShelvesListHeaderState GetDefault()
        {
            return new ShelvesListHeaderState(BuildColumns());
        }

        internal static List<string> GetColumnNames()
        {
            List<string> result = new List<string>();
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.CommentColumn));
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.CreationDateColumn));
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.CreatedByColumn));
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.NameColumn));
            return result;
        }

        internal static string GetColumnName(ShelvesListColumn column)
        {
            switch (column)
            {
                case ShelvesListColumn.Comment:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.CommentColumn);
                case ShelvesListColumn.CreationDate:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.CreationDateColumn);
                case ShelvesListColumn.CreatedBy:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.CreatedByColumn);
                case ShelvesListColumn.Name:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.NameColumn);
                default:
                    return null;
            }
        }

        internal static int[] GetDefaultVisibleColumns()
        {
            return new int[]
            {
                (int)ShelvesListColumn.Comment,
                (int)ShelvesListColumn.CreationDate,
                (int)ShelvesListColumn.CreatedBy,
            };
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (mHeaderTitles != null)
                TreeHeaderColumns.SetTitles(columns, mHeaderTitles);

            if (mColumnsAllowedToggleVisibility != null)
                TreeHeaderColumns.SetVisibilities(columns, mColumnsAllowedToggleVisibility);
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
                    width = UnityConstants.ShelvesColumns.COMMENT_WIDTH,
                    minWidth = UnityConstants.ShelvesColumns.COMMENT_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(ShelvesListColumn.Comment)),
                    allowToggleVisibility = false,
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.ShelvesColumns.CREATION_DATE_WIDTH,
                    minWidth = UnityConstants.ShelvesColumns.CREATION_DATE_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(ShelvesListColumn.CreationDate)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.ShelvesColumns.CREATEDBY_WIDTH,
                    minWidth = UnityConstants.ShelvesColumns.CREATEDBY_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(ShelvesListColumn.CreatedBy)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.ShelvesColumns.SHELVES_NAME_WIDTH,
                    minWidth = UnityConstants.ShelvesColumns.SHELVES_NAME_MIN_WIDTH,
                    headerContent = new GUIContent(
                        PlasticLocalization.Name.IdColumn.GetString()),
                    sortingArrowAlignment = TextAlignment.Right
                },
            };
        }

        ShelvesListHeaderState(Column[] columns)
            : base(columns)
        {
            if (mHeaderTitles == null)
                mHeaderTitles = TreeHeaderColumns.GetTitles(columns);

            if (mColumnsAllowedToggleVisibility == null)
                mColumnsAllowedToggleVisibility = TreeHeaderColumns.GetVisibilities(columns);
        }

        [SerializeField]
        string[] mHeaderTitles;

        [SerializeField]
        bool[] mColumnsAllowedToggleVisibility;
    }
}
