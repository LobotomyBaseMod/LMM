using Codice.Client.BaseCommands.BranchExplorer;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu
{
    class BranchExplorerViewLinkMenuOperations : ILinkMenuOperations
    {
        internal BranchExplorerViewLinkMenuOperations(
            IBrExNavigate brExNavigate,
            BranchExplorerSelection selection)
        {
            mBrExNavigate = brExNavigate;
            mSelectionHandler = selection;
        }

        void ILinkMenuOperations.GoToSourcechangeset()
        {
            NavigateToChangeset(GetSelectedLink().SourceChangeset);
        }

        void ILinkMenuOperations.GoToDestinationChangeset()
        {
            NavigateToChangeset(GetSelectedLink().DestinationChangeset);
        }

        int ILinkMenuOperations.GetSelectedLinksCount()
        {
            return mSelectionHandler.GetSelectedLinksCount();
        }

        LinkDrawInfo GetSelectedLink()
        {
            return mSelectionHandler.GetSelectedLinks()[0];
        }

        void NavigateToChangeset(ChangesetDrawInfo changeset)
        {
            VirtualShape commitShape = (VirtualShape)changeset.Visual;
            commitShape.IsLinkNavigationTarget = true;

            mBrExNavigate.NavigateToShape(commitShape);
        }

        readonly IBrExNavigate mBrExNavigate;
        readonly BranchExplorerSelection mSelectionHandler;
    }
}

