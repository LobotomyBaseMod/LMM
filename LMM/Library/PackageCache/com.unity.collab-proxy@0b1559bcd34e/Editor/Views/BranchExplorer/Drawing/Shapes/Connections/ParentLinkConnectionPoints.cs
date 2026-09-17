using System;
using Codice.Client.BaseCommands.BranchExplorer;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections
{
    internal static class ParentLinkConnectionPoints
    {
        internal static ConnectionPoints Build(Rect source, Rect destination)
        {
            ConnectionPoints result = new ConnectionPoints();
            result.Source = BuildSourcePoint(source);
            result.Destination = BuildDestinationPoint(source, destination);
            return result;
        }

        static Vector2 BuildSourcePoint(Rect source)
        {
            return new Vector2(
                source.x,
                source.y + source.height / 2);
        }

        static Vector2 BuildDestinationPoint(Rect source, Rect destination)
        {
            if (Math.Abs(destination.y - source.y) < BrExDrawProperties.ChangesetDrawingHeight)
            {
                return new Vector2(
                    destination.x + destination.width + START_PADDING,
                    destination.y + destination.height / 2);
            }

            if (destination.y > source.y)
            {
                return new Vector2(
                    destination.x + destination.width / 2,
                    destination.y - START_PADDING);
            }

            return new Vector2(
                destination.x + destination.width / 2,
                destination.y + source.height + START_PADDING);
        }

        const float START_PADDING = 3;
    }
}
