using System.Collections.Generic;

using PlasticGui.WorkspaceWindow.Diff;

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal static class DiffSelection
    {
        internal static List<ClientDiffInfo> GetSelectedDiffs(
            DiffTreeView treeView)
        {
            return treeView.GetSelectedDiffs(true);
        }

        internal static List<ClientDiffInfo> GetSelectedDiffsWithoutMeta(
            DiffTreeView treeView)
        {
            return treeView.GetSelectedDiffs(false);
        }

        internal static ClientDiffInfo GetSelectedDiff(
            DiffTreeView treeView)
        {
            if (!treeView.HasSelection())
                return null;

            List<ClientDiffInfo> selectedDiffs = treeView.GetSelectedDiffs(false);

            return selectedDiffs.Count > 0 ? selectedDiffs[0] : null;
        }

        internal static bool IsApplicableDiffClientDiff(
            DiffTreeView treeView)
        {
            ClientDiffInfo selectedDiff = GetSelectedDiff(treeView);

            if (selectedDiff == null)
                return false;

            return DiffOperation.IsApplicableDiffClientDiff(selectedDiff.DiffWithMount.Difference);
        }
    }
}
