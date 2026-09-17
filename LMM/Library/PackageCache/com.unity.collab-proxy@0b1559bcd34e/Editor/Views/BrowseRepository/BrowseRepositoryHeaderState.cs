using System;
using System.Collections.Generic;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.BrowseRepository;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Views.BrowseRepository
{
    internal enum BrowseRepositoryColumn
    {
        Item,
        Size,
        Type,
        Branch,
        Changeset,
        CreatedBy,
        DateModified,
        Repository
    }

    [Serializable]
    internal class BrowseRepositoryHeaderState : MultiColumnHeaderState, ISerializationCallbackReceiver
    {
        internal static BrowseRepositoryHeaderState GetDefault()
        {
            return new BrowseRepositoryHeaderState(BuildColumns());
        }

        internal static string GetColumnName(BrowseRepositoryColumn column)
        {
            switch (column)
            {
                case BrowseRepositoryColumn.Item:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.ItemColumn);
                case BrowseRepositoryColumn.Size:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.SizeColumn);
                case BrowseRepositoryColumn.Type:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.TypeColumn);
                case BrowseRepositoryColumn.Branch:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.BranchColumn);
                case BrowseRepositoryColumn.Changeset:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetColumn);
                case BrowseRepositoryColumn.CreatedBy:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.CreatedByColumn);
                case BrowseRepositoryColumn.DateModified:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.DateModifiedColumn);
                case BrowseRepositoryColumn.Repository:
                    return PlasticLocalization.GetString(PlasticLocalization.Name.RepositoryColumn);
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
                    width = UnityConstants.BrowseRepositoryColumns.ITEM_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.ITEM_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.Item)),
                    allowToggleVisibility = false,
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BrowseRepositoryColumns.SIZE_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.SIZE_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.Size)),
                    sortingArrowAlignment = TextAlignment.Right,
                },
                new Column()
                {
                    width = UnityConstants.BrowseRepositoryColumns.TYPE_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.TYPE_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.Type)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BrowseRepositoryColumns.BRANCH_NAME_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.BRANCH_NAME_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.Branch)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BrowseRepositoryColumns.CHANGESET_NUMBER_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.CHANGESET_NUMBER_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.Changeset)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BrowseRepositoryColumns.CREATEDBY_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.CREATEDBY_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.CreatedBy)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BrowseRepositoryColumns.MODIFICATION_DATE_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.MODIFICATION_DATE_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.DateModified)),
                    sortingArrowAlignment = TextAlignment.Right
                },
                new Column()
                {
                    width = UnityConstants.BrowseRepositoryColumns.REPOSITORY_WIDTH,
                    minWidth = UnityConstants.BrowseRepositoryColumns.REPOSITORY_MIN_WIDTH,
                    headerContent = new GUIContent(
                        GetColumnName(BrowseRepositoryColumn.Repository)),
                    sortingArrowAlignment = TextAlignment.Right
                },
            };
        }

        internal static Dictionary<BrowseRepositoryColumn, IComparer<BrowseRepositoryTreeNode>> BuildColumnComparers()
        {
            return new Dictionary<BrowseRepositoryColumn, IComparer<BrowseRepositoryTreeNode>>()
            {
                {
                    BrowseRepositoryColumn.Item,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.ItemColumn))
                },
                {
                    BrowseRepositoryColumn.Size,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.SizeColumn))
                },
                {
                    BrowseRepositoryColumn.Type,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.TypeColumn))
                },
                {
                    BrowseRepositoryColumn.Branch,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.BranchColumn))
                },
                {
                    BrowseRepositoryColumn.Changeset,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetColumn))
                },
                {
                    BrowseRepositoryColumn.CreatedBy,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.CreatedByColumn))
                },
                {
                    BrowseRepositoryColumn.DateModified,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.DateModifiedColumn))
                },
                {
                    BrowseRepositoryColumn.Repository,
                    new BrowseRepositoryTreeNodeComparer(
                        PlasticLocalization.GetString(PlasticLocalization.Name.RepositoryColumn))
                },
            };
        }

        BrowseRepositoryHeaderState(Column[] columns)
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
