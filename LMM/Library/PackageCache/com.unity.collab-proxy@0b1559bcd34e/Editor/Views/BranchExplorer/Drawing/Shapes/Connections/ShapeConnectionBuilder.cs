using UnityEngine;

using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections
{
    internal static class ShapeConnectionBuilder
    {
        internal static ParentLinkShape BuildParentLinkConnection(
            VirtualShape shape,
            VirtualShape src,
            VirtualShape dst,
            bool isRelevant)
        {
            ParentLinkShape result = new ParentLinkShape(shape, isRelevant);

            result.Source = new Rect(
                src.Bounds.x, src.Bounds.y,
                src.Bounds.width, src.Bounds.height);

            result.Destination = new Rect(
                dst.Bounds.x, dst.Bounds.y,
                dst.Bounds.width, dst.Bounds.height);

            return result;
        }

        internal static MergeLinkShape BuildMergeLinkConnection(
            VirtualShape shape, VirtualShape src, VirtualShape dst)
        {
            return new MergeLinkShape(
                shape,
                src.Bounds,
                dst.Bounds);
        }
    }
}
