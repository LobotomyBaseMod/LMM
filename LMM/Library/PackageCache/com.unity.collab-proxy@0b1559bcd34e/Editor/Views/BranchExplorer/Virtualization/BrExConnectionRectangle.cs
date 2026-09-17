using System;
using Codice.Client.BaseCommands.BranchExplorer;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal static class BrExConnectionRectangle
    {
        internal static BrExRectangle CreateConnectionRectangle(
            BrExRectangle sourceBounds,
            BrExRectangle destinationBounds)
        {
            int left = Math.Min(sourceBounds.Left, destinationBounds.Left);
            int top = Math.Min(sourceBounds.Top, destinationBounds.Top);
            int maxRight = Math.Max(sourceBounds.Right, destinationBounds.Right);
            int maxBottom = Math.Max(sourceBounds.Bottom, destinationBounds.Bottom);

            int width = Math.Max(maxRight - left, 0);
            int height = Math.Max(maxBottom - top, 0);

            return new BrExRectangle(left, top, width, height);
        }
    }
}
