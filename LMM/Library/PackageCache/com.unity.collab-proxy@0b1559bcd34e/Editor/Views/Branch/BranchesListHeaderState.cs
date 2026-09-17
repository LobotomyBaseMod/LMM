using System;
using System.Collections.Generic;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Views.Branches
{
    internal enum BranchesListColumn
    {
        Name,
        Comment,
        CreatedBy,
        CreationDate,
    }

    [Serializable]
    internal class BranchesListHeaderState : MultiColumnHeaderState, ISerializationCallbackReceiver
    {
        internal static BranchesListHeaderState GetDefault()
        {
            return new BranchesListHeaderState(BuildColumns());
        }

        internal static List<string> GetColumnNames()
        {
            List<string> result = new List<string>();
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.NameColumn));
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.CommentColumn));
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.CreatedByColumn));
            result.Add(PlasticLocalization.GetString(PlasticLocalization.Name.CreationDateColumn));
            return result;
        }

        internal static string GetColumnName(BranchesListColumn column)
        {
            switch (column)
            {
                case BranchesListColumn.Name:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.NameColumn);
                case BranchesListColumn.Comment:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.CommentColumn);
                case BranchesListColumn.CreatedBy:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.CreatedByColumn);
                case BranchesListColumn.CreationDate:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.CreationDateColumn);
                default:
                    return null;
            }
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
                    width = UnityConstants.BranchesColumns.BRANCHES_NAME_WIDTH,
                    minWidth = UnityConstants.BranchesColumns.BRANCHES_NAME_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BranchesListColumn.Name)),
                    allowToggleVisibility = false,
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BranchesColumns.COMMENT_WIDTH,
                    minWidth = UnityConstants.BranchesColumns.COMMENT_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BranchesListColumn.Comment)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BranchesColumns.CREATEDBY_WIDTH,
                    minWidth = UnityConstants.BranchesColumns.CREATEDBY_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BranchesListColumn.CreatedBy)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BranchesColumns.CREATION_DATE_WIDTH,
                    minWidth = UnityConstants.BranchesColumns.CREATION_DATE_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BranchesListColumn.CreationDate)),
                    sortingArrowAlignment = TextAlignment.Right
                },
            };
        }

        BranchesListHeaderState(Column[] columns)
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
