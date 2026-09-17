using System;
using Codice.Client.BaseCommands.BranchExplorer;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal static class VisualShapeFactory
    {
        internal static BrExShape CreateVisual(
            VirtualShape shape,
            WorkspaceUIConfiguration configuration,
            ColorProvider colorProvider,
            AsyncTaskLoader taskLoader,
            AsyncUserNameResolver userNameResolver,
            AsyncChangesetCommentResolver commentResolver)
        {
            switch (shape.ShapeType)
            {
                case ShapeType.ColumnHeader:
                    return CreateColumnHeaderShape(shape);
                case ShapeType.Column:
                    return CreateColumnShape(shape);
                case ShapeType.Changeset:
                    return CreateChangesetShape(shape, colorProvider, userNameResolver);
                case ShapeType.Branch:
                    return CreateBranchShape(shape, colorProvider);
                case ShapeType.BranchCaption:
                    return CreateBranchCaptionShape(shape, taskLoader);
                case ShapeType.Label:
                    return CreateLabelShape(shape, colorProvider);
                case ShapeType.ChangesetComment:
                    return CreateChangesetCommentShape(shape, commentResolver);
                case ShapeType.ChangesetParentLink:
                    return CreateChangesetParentLinkShape(
                        shape,
                        configuration.DisplayOptions.DisplayOnlyRelevantChangesets);
                case ShapeType.BranchParentLink:
                    return CreateBranchParentLinkShape(shape);
                case ShapeType.MergeLink:
                    return CreateMergeLinkShape(shape);
            }

            return null;
        }

        static BrExShape CreateColumnHeaderShape(VirtualShape shape)
        {
            return new ColumnHeaderShape(shape);
        }

        static BrExShape CreateColumnShape(VirtualShape shape)
        {
            return new ColumnShape(shape);
        }

        static BrExShape CreateChangesetShape(
            VirtualShape shape,
            ColorProvider colorProvider,
            AsyncUserNameResolver userNameResolver)
        {
            return new ChangesetShape(shape, colorProvider, userNameResolver);
        }

        static BrExShape CreateBranchShape(VirtualShape shape, ColorProvider colorProvider)
        {
            return new BranchShape(shape, colorProvider);
        }

        static BrExShape CreateBranchCaptionShape(VirtualShape shape, AsyncTaskLoader taskLoader)
        {
            return new BranchCaptionShape(shape, taskLoader);
        }

        static BrExShape CreateLabelShape(VirtualShape shape, ColorProvider colorProvider)
        {
            return new LabelShape(shape, colorProvider);
        }

        static BrExShape CreateChangesetCommentShape(
            VirtualShape shape,
            AsyncChangesetCommentResolver commentResolver)
        {
            return new ChangesetCommentShape(shape, commentResolver);
        }

        static BrExShape CreateChangesetParentLinkShape(
            VirtualShape shape,
            bool bDisplayOnlyRelevantCommits)
        {
            ChangesetDrawInfo changesetDrawInfo = (ChangesetDrawInfo)shape.DrawInfo;

            ChangesetDrawInfo parentDrawInfo = (bDisplayOnlyRelevantCommits) ?
                changesetDrawInfo.RelevantParent :
                changesetDrawInfo.Parent;

            return ShapeConnectionBuilder.BuildParentLinkConnection(
                shape,
                (VirtualShape)changesetDrawInfo.Visual,
                (VirtualShape)parentDrawInfo.Visual,
                parentDrawInfo != changesetDrawInfo.Parent);
        }

        static BrExShape CreateBranchParentLinkShape(VirtualShape shape)
        {
            BranchDrawInfo branchDrawInfo = (BranchDrawInfo)shape.DrawInfo;

            return ShapeConnectionBuilder.BuildParentLinkConnection(
                shape,
                (VirtualShape)branchDrawInfo.Visual,
                (VirtualShape)branchDrawInfo.HeadChangeset.Visual,
                false);
        }

        static BrExShape CreateMergeLinkShape(VirtualShape shape)
        {
            LinkDrawInfo linkDrawInfo = (LinkDrawInfo)shape.DrawInfo;

            return ShapeConnectionBuilder.BuildMergeLinkConnection(shape,
                (VirtualShape)linkDrawInfo.SourceChangeset.Visual,
                (VirtualShape)linkDrawInfo.DestinationChangeset.Visual);
        }
    }
}
