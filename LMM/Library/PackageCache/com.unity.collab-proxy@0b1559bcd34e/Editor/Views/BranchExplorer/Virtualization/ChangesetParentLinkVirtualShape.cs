using UnityEngine;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;

using ShapeType = PlasticGui.WorkspaceWindow.BranchExplorer.ShapeType;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal class ChangesetParentLinkVirtualShape : VirtualShape
    {
        internal MergeType MergeLinkInvervalType
        {
            get { return mMergeLinkInvervalType; }
            set
            {
                mMergeLinkInvervalType = value;

                if (mVisual == null)
                    return;

                ((ParentLinkShape)mVisual).MergeLinkInvervalType = value;
            }
        }

        internal bool IsHighlightedByMergeLinkInterval
        {
            get { return mIsHighlightedByMergeLinkInterval; }
            set
            {
                mIsHighlightedByMergeLinkInterval = value;

                if (mVisual == null)
                    return;

                ((ParentLinkShape)mVisual).IsHighlightedByMergeLinkInterval = value;
            }
        }

        internal ChangesetParentLinkVirtualShape(
            ObjectDrawInfo drawInfo,
            Rect bounds,
            WorkspaceUIConfiguration config,
            ColorProvider colorProvider = null) : base(
            drawInfo, bounds, ShapeType.ChangesetParentLink, config, colorProvider)
        {
        }

        bool mIsHighlightedByMergeLinkInterval;
        MergeType mMergeLinkInvervalType;
    }
}
