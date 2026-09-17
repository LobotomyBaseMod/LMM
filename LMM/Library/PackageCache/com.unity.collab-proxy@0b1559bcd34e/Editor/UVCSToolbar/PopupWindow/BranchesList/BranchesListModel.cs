using System;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Topbar.WorkingObjectInfo.BranchesList;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList
{
    internal class BranchesListModel
    {
        internal ClassifiedBranchesList Branches { get; private set; }
        internal RepositorySpec RepSpec { get; private set; }
        internal Exception Exception { get; private set; }
        internal bool IsLoading { get { return Branches == null && Exception == null; } }
        internal bool HasErrors { get { return Exception != null; } }
        internal bool IsEmpty { get { return Branches != null && Branches.IsEmpty; } }

        internal static BranchesListModel FromException(Exception exception)
        {
            return new BranchesListModel(null, null, exception);
        }

        internal static BranchesListModel FromBranches(
            ClassifiedBranchesList branches,
            RepositorySpec repSpec)
        {
            return new BranchesListModel(branches, repSpec, null);
        }

        internal static BranchesListModel BuildEmpty()
        {
            return new BranchesListModel(null, null, null);
        }

        internal void ResetFilter()
        {
            if (Branches == null)
                return;

            Branches.ResetFilter();
        }

        internal void ApplyFilter(Filter filter)
        {
            if (Branches == null)
                return;

            Branches.ApplyFilter(filter);
        }

        BranchesListModel(ClassifiedBranchesList branches, RepositorySpec repSpec, Exception ex)
        {
            Branches = branches;
            RepSpec = repSpec;
            Exception = ex;
        }
    }
}
