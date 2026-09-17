using Codice.CM.Common;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Merge;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessMergeViewLauncher : IMergeViewLauncher
    {
        internal HeadlessMergeViewLauncher(UVCSPlugin uvcsPlugin)
        {
            mUVCSPlugin = uvcsPlugin;
        }

        IMergeView IMergeViewLauncher.FromCalculatedMerge(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            CalculatedMergeResult calculatedMergeResult,
            bool showDiscardChangesButton)
        {
            UVCSWindow window = SwitchUVCSPlugin.OnIfNeeded(mUVCSPlugin);

            return window.IMergeViewLauncher.FromCalculatedMerge(
                repSpec,
                objectInfo,
                mergeType,
                calculatedMergeResult,
                showDiscardChangesButton);
        }

        IMergeView IMergeViewLauncher.MergeFrom(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            bool showDiscardChangesButton)
        {
            UVCSWindow window = SwitchUVCSPlugin.OnIfNeeded(mUVCSPlugin);

            return window.IMergeViewLauncher.MergeFrom(
                repSpec,
                objectInfo,
                mergeType,
                showDiscardChangesButton);
        }

        IMergeView IMergeViewLauncher.MergeFrom(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            EnumMergeType mergeType,
            ShowIncomingChangesFrom from,
            bool showDiscardChangesButton)
        {
            UVCSWindow window = SwitchUVCSPlugin.OnIfNeeded(mUVCSPlugin);

            return window.IMergeViewLauncher.MergeFrom(
                repSpec,
                objectInfo,
                mergeType,
                from,
                showDiscardChangesButton);
        }

        IMergeView IMergeViewLauncher.MergeFromInterval(
            RepositorySpec repSpec,
            ObjectInfo objectInfo,
            ObjectInfo ancestorChangesetInfo,
            EnumMergeType mergeType,
            bool showDiscardChangesButton)
        {
            UVCSWindow window = SwitchUVCSPlugin.OnIfNeeded(mUVCSPlugin);

            return window.IMergeViewLauncher.MergeFromInterval(
                repSpec,
                objectInfo,
                ancestorChangesetInfo,
                mergeType,
                showDiscardChangesButton);
        }

        readonly UVCSPlugin mUVCSPlugin;
    }
}
