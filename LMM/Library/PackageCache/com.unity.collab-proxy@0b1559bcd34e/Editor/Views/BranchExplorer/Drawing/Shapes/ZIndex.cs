using Codice.Client.BaseCommands.BranchExplorer;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal static class ZIndex
    {
        // Base multiplier for position calculation.
        // Positions are calculated as: (zIndex * BASE_MULTIPLIER) + originalIndex
        // This ensures proper z-ordering while maintaining relative order within each z-index level.
        internal const int BASE_MULTIPLIER = 1000000;

        internal static int GetZIndex(VirtualShape virtualShape)
        {
            switch (virtualShape.ShapeType)
            {
                case ShapeType.Column:
                    return COLUMN_ZINDEX;

                case ShapeType.ColumnHeader:
                    return COLUMN_HEADER_ZINDEX;

                case ShapeType.ChangesetParentLink:
                    return GetChangesetParentLinkZIndex(virtualShape);

                case ShapeType.BranchParentLink:
                    return DEFAULT_ZINDEX;

                case ShapeType.Branch:
                case ShapeType.EmptyBranch:
                    return BRANCH_ZINDEX;

                case ShapeType.BranchCaption:
                    return BRANCH_CAPTION_ZINDEX;

                case ShapeType.Changeset:
                    return CHANGESET_ZINDEX;

                case ShapeType.ChangesetComment:
                    return CHANGESET_COMMENT_ZINDEX;

                case ShapeType.Label:
                    return LABEL_ZINDEX;

                case ShapeType.MergeLink:
                    return DEFAULT_ZINDEX;

                default:
                    return DEFAULT_ZINDEX;
            }
        }

        static int GetParentLinkShapeZIndex(ParentLinkShape shape)
        {
            if (shape.VirtualShape.DrawInfo is ChangesetDrawInfo)
            {
                ChangesetDrawInfo commitDraw =
                    (ChangesetDrawInfo)shape.VirtualShape.DrawInfo;

                ChangesetDrawInfo parent = commitDraw.Parent ?? commitDraw.RelevantParent;

                if (parent == null)
                    return DEFAULT_ZINDEX;

                return commitDraw.Branch == parent.Branch ?
                    PARENT_LINK_ON_SAME_BRANCH_ZINDEX : DEFAULT_ZINDEX;
            }

            return DEFAULT_ZINDEX;
        }

        static int GetChangesetParentLinkZIndex(VirtualShape virtualShape)
        {
            if (!(virtualShape.DrawInfo is ChangesetDrawInfo))
                return DEFAULT_ZINDEX;

            ChangesetDrawInfo commitDraw = (ChangesetDrawInfo)virtualShape.DrawInfo;

            ChangesetDrawInfo parent = commitDraw.Parent ?? commitDraw.RelevantParent;

            if (parent == null)
                return DEFAULT_ZINDEX;

            return commitDraw.Branch == parent.Branch ?
                PARENT_LINK_ON_SAME_BRANCH_ZINDEX : DEFAULT_ZINDEX;
        }

        const int COLUMN_ZINDEX = 0;
        internal const int DEFAULT_ZINDEX = 1;
        const int BRANCH_ZINDEX = 2;
        const int CHANGESET_COMMENT_ZINDEX = 3;
        const int LABEL_ZINDEX = 4;
        const int CHANGESET_ZINDEX = 5;
        internal const int PARENT_LINK_ON_SAME_BRANCH_ZINDEX = 6;
        const int BRANCH_CAPTION_ZINDEX = 7;
        const int COLUMN_HEADER_ZINDEX = 8;
    }
}
