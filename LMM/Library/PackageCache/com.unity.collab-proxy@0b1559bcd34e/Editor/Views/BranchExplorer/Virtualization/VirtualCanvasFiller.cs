using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.LogWrapper;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal static class VirtualCanvasFiller
    {
        internal static void FillVirtualCanvas(
            VirtualCanvas canvas,
            BrExLayout layout,
            WorkspaceUIConfiguration config,
            ColorProvider colorProvider,
            AsyncTaskLoader taskLoader,
            AsyncUserNameResolver userNameResolver,
            AsyncChangesetCommentResolver commentResolver)
        {
            BuildVirtualShapes(canvas, layout, config, colorProvider, taskLoader, userNameResolver, commentResolver);
            canvas.RebuildVisuals();
        }

        static void BuildVirtualShapes(
            VirtualCanvas canvas,
            BrExLayout layout,
            WorkspaceUIConfiguration config,
            ColorProvider colorProvider,
            AsyncTaskLoader taskLoader,
            AsyncUserNameResolver userNameResolver,
            AsyncChangesetCommentResolver commentResolver)
        {
            int ini = Environment.TickCount;

            canvas.SetLayout(layout);

            canvas.ClearVirtualChildren();

            BuildColumnVirtualShapes(canvas, layout.ColumnDraws, config);
            BuildLabelVirtualShapes(canvas, layout.LabelDraws, config, colorProvider);
            BuildChangesetVirtualShapes(canvas, layout.ChangesetDraws, config, colorProvider, userNameResolver);
            BuildChangesetCommentVirtualShapes(canvas, layout.ChangesetDraws, config, commentResolver);
            BuildChangesetParentLinksVirtualShapes(canvas, layout.ChangesetDraws, config);
            BuildBranchParentLinksVirtualShapes(canvas, layout.BranchDraws, config);
            BuildLinkVirtualShapes(canvas, layout.LinkDraws, config);
            BuildBranchVirtualShapes(canvas, layout.BranchDraws, config, colorProvider, taskLoader);

            mLog.DebugFormat("CreateLayout time {0}",
                Environment.TickCount - ini);
        }

        static void BuildColumnVirtualShapes(
            VirtualCanvas canvas,
            List<ColumnDrawInfo> columnDraws,
            WorkspaceUIConfiguration config)
        {
            foreach (ColumnDrawInfo columnDraw in columnDraws)
            {
                Rect columnRect = new Rect(columnDraw.Bounds.X, columnDraw.Bounds.Y,
                    columnDraw.Bounds.Width, columnDraw.Bounds.Height);

                canvas.AddVirtualChild(new VirtualShape(
                    columnDraw,
                    columnRect,
                    ShapeType.ColumnHeader,
                    config));
                canvas.AddVirtualChild(new VirtualShape(
                    columnDraw,
                    columnRect,
                    ShapeType.Column,
                    config));
            }
        }

        static void BuildChangesetVirtualShapes(
            VirtualCanvas canvas,
            List<ChangesetDrawInfo> changesetDraws,
            WorkspaceUIConfiguration config,
            ColorProvider colorProvider,
            AsyncUserNameResolver userNameResolver)
        {
            foreach (ChangesetDrawInfo changesetDraw in changesetDraws)
            {
                VirtualShape virtualShape = new VirtualShape(
                    changesetDraw,
                    BuildRect(changesetDraw.Bounds),
                    ShapeType.Changeset,
                    config,
                    colorProvider,
                    null,
                    userNameResolver);
                changesetDraw.Visual = virtualShape;
                canvas.AddVirtualChild(virtualShape);
            }
        }

        static void BuildChangesetCommentVirtualShapes(
            VirtualCanvas canvas,
            List<ChangesetDrawInfo> changesetDraws,
            WorkspaceUIConfiguration config,
            AsyncChangesetCommentResolver commentResolver)
        {
            foreach (ChangesetDrawInfo changesetDraw in changesetDraws)
            {
                Rect commentRect = BuildCommentRect(changesetDraw.Bounds);

                VirtualShape commentShape = new VirtualShape(
                    changesetDraw,
                    commentRect,
                    ShapeType.ChangesetComment,
                    config,
                    commentResolver: commentResolver);

                canvas.AddVirtualChild(commentShape);
            }
        }

        static Rect BuildCommentRect(BrExRectangle csBounds)
        {
            return new Rect(
                csBounds.X,
                csBounds.Y + csBounds.Height + CommentVerticalMargin,
                csBounds.Width,
                CommentHeight);
        }

        static void BuildBranchVirtualShapes(
            VirtualCanvas canvas,
            List<BranchDrawInfo> branchDraws,
            WorkspaceUIConfiguration config,
            ColorProvider colorProvider,
            AsyncTaskLoader taskLoader)
        {
            foreach (BranchDrawInfo branchDraw in branchDraws)
            {
                VirtualShape branchShape = new VirtualShape(
                    branchDraw,
                    BuildRect(BuildBoundsWithSubBranches(branchDraw)),
                    ShapeType.Branch,
                    config,
                    colorProvider);
                branchDraw.Visual = branchShape;

                VirtualShape captionShape = new VirtualShape(
                    branchDraw,
                    BuildRect(BuildBoundsWithSubBranches(branchDraw)),
                    ShapeType.BranchCaption,
                    config,
                    null,
                    taskLoader);

                canvas.AddVirtualChild(branchShape);
                canvas.AddVirtualChild(captionShape);
            }
        }

        static void BuildLabelVirtualShapes(
            VirtualCanvas canvas,
            List<LabelDrawInfo> labelDraws,
            WorkspaceUIConfiguration config,
            ColorProvider colorProvider)
        {
            foreach (LabelDrawInfo labelDraw in labelDraws)
            {
                VirtualShape virtualShape = new VirtualShape(
                    labelDraw,
                    BuildRect(labelDraw.Bounds),
                    ShapeType.Label,
                    config,
                    colorProvider);
                labelDraw.Visual = virtualShape;
                canvas.AddVirtualChild(virtualShape);
            }
        }

        static void BuildChangesetParentLinksVirtualShapes(
            VirtualCanvas canvas,
            List<ChangesetDrawInfo> commitDraws,
            WorkspaceUIConfiguration config)
        {
            List<ChangesetDrawInfo> parentCommitLinksOnSameBranch;
            List<ChangesetDrawInfo> parentCommitLinksOnDifferentBranch;
            GetParentChangesetLinks(
                commitDraws,
                config,
                out parentCommitLinksOnSameBranch,
                out parentCommitLinksOnDifferentBranch);

            parentCommitLinksOnDifferentBranch.Sort(
                new ChangesetDrawInfoByBranchRowDescComparer());

            BuildParentLinkVirtualShapes(
                canvas,
                parentCommitLinksOnSameBranch,
                config);

            BuildParentLinkVirtualShapes(
                canvas,
                parentCommitLinksOnDifferentBranch,
                config);
        }

        static void BuildParentLinkVirtualShapes(
            VirtualCanvas canvas,
            List<ChangesetDrawInfo> changesetDraws,
            WorkspaceUIConfiguration config)
        {
            foreach (ChangesetDrawInfo changesetDraw in changesetDraws)
            {
                ChangesetDrawInfo parentDraw = (config.DisplayOptions.DisplayOnlyRelevantChangesets) ?
                    changesetDraw.RelevantParent :
                    changesetDraw.Parent;

                if (parentDraw == null)
                    continue;

                BrExRectangle rectangle = BrExConnectionRectangle.CreateConnectionRectangle(
                    changesetDraw.Bounds, parentDraw.Bounds);

                VirtualShape parentLinkShape = new ChangesetParentLinkVirtualShape(
                    changesetDraw,
                    BuildRect(rectangle),
                    config);

                changesetDraw.ParentLinkVisual = parentLinkShape;
                canvas.AddVirtualChild(parentLinkShape);
            }
        }

        static void BuildBranchParentLinksVirtualShapes(
            VirtualCanvas canvas,
            List<BranchDrawInfo> branchDraws,
            WorkspaceUIConfiguration config)
        {
            foreach (BranchDrawInfo branchDraw in branchDraws)
            {
                BrExBranch explorerBranch = branchDraw.Tag as BrExBranch;

                if (!explorerBranch.IsEmpty())
                    continue;

                if (branchDraw.HeadChangeset == null)
                    continue;

                BrExRectangle rectangle = BrExConnectionRectangle.CreateConnectionRectangle(
                    branchDraw.Bounds, branchDraw.HeadChangeset.Bounds);

                canvas.AddVirtualChild(new VirtualShape(
                    branchDraw,
                    BuildRect(rectangle),
                    ShapeType.BranchParentLink,
                    config));
            }
        }

        static void BuildLinkVirtualShapes(
            VirtualCanvas canvas,
            List<LinkDrawInfo> linkDraws,
            WorkspaceUIConfiguration config)
        {
            foreach (LinkDrawInfo linkDraw in linkDraws)
            {
                BrExRectangle rectangle = BrExConnectionRectangle.CreateConnectionRectangle(
                    linkDraw.SourceChangeset.Bounds, linkDraw.DestinationChangeset.Bounds);

                VirtualShape virtualShape = new VirtualShape(
                    linkDraw,
                    BuildRect(rectangle),
                    ShapeType.MergeLink,
                    config);
                linkDraw.Visual = virtualShape;
                canvas.AddVirtualChild(virtualShape);
            }
        }

        static Rect BuildRect(BrExRectangle rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        static BrExRectangle BuildBoundsWithSubBranches(
            BranchDrawInfo branchDraw)
        {
            if (branchDraw.SubBranchContainers == null ||
                branchDraw.SubBranchContainers.Length == 0)
                return branchDraw.Bounds;

            int maxBottom = branchDraw.Bounds.Bottom;

            foreach (var container in branchDraw.SubBranchContainers)
                maxBottom = Math.Max(maxBottom, container.Bounds.Bottom);

            return new BrExRectangle(
                branchDraw.Bounds.X,
                branchDraw.Bounds.Y,
                branchDraw.Bounds.Width,
                maxBottom - branchDraw.Bounds.Y + BrExDrawProperties.ChangesetRadius);
        }

        static void GetParentChangesetLinks(
            List<ChangesetDrawInfo> changesetDraws,
            WorkspaceUIConfiguration config,
            out List<ChangesetDrawInfo> parentChangesetLinksOnSameBranch,
            out List<ChangesetDrawInfo> parentChangesetLinksOnDifferentBranch)
        {
            parentChangesetLinksOnSameBranch = new List<ChangesetDrawInfo>();
            parentChangesetLinksOnDifferentBranch = new List<ChangesetDrawInfo>();

            foreach (ChangesetDrawInfo changesetDraw in changesetDraws)
            {
                ChangesetDrawInfo parentDraw = (config.DisplayOptions.DisplayOnlyRelevantChangesets) ?
                    changesetDraw.RelevantParent :
                    changesetDraw.Parent;

                if (parentDraw == null)
                    continue;

                if (changesetDraw.Branch == parentDraw.Branch)
                {
                    parentChangesetLinksOnSameBranch.Add(changesetDraw);
                    continue;
                }

                parentChangesetLinksOnDifferentBranch.Add(changesetDraw);
            }
        }

        class ChangesetDrawInfoByBranchRowDescComparer : IComparer<ChangesetDrawInfo>
        {
            int IComparer<ChangesetDrawInfo>.Compare(ChangesetDrawInfo xChangeset, ChangesetDrawInfo yChangeset)
            {
                if (xChangeset == null && yChangeset == null)
                    return 0;

                if (xChangeset == null)
                    return -1;

                if (yChangeset == null)
                    return 1;

                if (xChangeset.Equals(yChangeset))
                    return 0;

                if (xChangeset.Branch.Row == yChangeset.Branch.Row)
                    return 0;

                return xChangeset.Branch.Row > yChangeset.Branch.Row ? -1 : 1;
            }
        }

        const int CommentVerticalMargin = 4;
        const int CommentHeight = 24;

        static readonly ILog mLog = LogManager.GetLogger("VirtualCanvasFiller");
    }
}
