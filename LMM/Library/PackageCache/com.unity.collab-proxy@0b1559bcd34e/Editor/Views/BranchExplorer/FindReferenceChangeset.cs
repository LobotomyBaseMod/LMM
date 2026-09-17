using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes;
using System.Collections.Generic;
using Codice.Client.BaseCommands.BranchExplorer;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal static class FindReferenceChangeset
    {
        internal static ChangesetDrawInfo FromChangesetShapes(List<ChangesetShape> shapes)
        {
            BrExRectangle mostRightChangesetBounds = new BrExRectangle();
            ChangesetDrawInfo mostRightChangeset = null;

            foreach (ChangesetShape changesetShape in shapes)
            {
                if (changesetShape.ChangesetDraw.IsWorkspaceChangeset)
                    return changesetShape.ChangesetDraw;

                if (changesetShape.IsSelected)
                    return GetRelevantSelfOrParent(changesetShape.ChangesetDraw);

                if (changesetShape.ChangesetDraw.Bounds.X <= mostRightChangesetBounds.X)
                    continue;

                mostRightChangesetBounds = changesetShape.ChangesetDraw.Bounds;
                mostRightChangeset = changesetShape.ChangesetDraw;
            }

            return mostRightChangeset;
        }

        static ChangesetDrawInfo GetRelevantSelfOrParent(ChangesetDrawInfo changesetDrawInfo)
        {
            ChangesetDrawInfo current = changesetDrawInfo;

            while (current != null && !IsRelevant(current))
            {
                current = GetParentOrRelevantParent(current);
            }

            return current;
        }

        static ChangesetDrawInfo GetParentOrRelevantParent(ChangesetDrawInfo changesetDrawInfo)
        {
            if (changesetDrawInfo.Parent != null)
                return changesetDrawInfo.Parent;

            if (changesetDrawInfo.RelevantParent != null)
                return changesetDrawInfo.RelevantParent;

            return null;
        }

        static bool IsRelevant(ChangesetDrawInfo changesetDrawInfo)
        {
            return ((BrExChangeset)changesetDrawInfo.Tag).IsRelevant;
        }
    }
}
