using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.QueryViews;

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal static class GetChangesetsQuery
    {
        internal static string For(DateFilter dateFilter)
        {
            if (dateFilter.FilterType == DateFilter.Type.AllTime)
                return QueryConstants.ChangesetsBeginningQuery;

            string whereClause = QueryConstants.GetDateWhereClause(
                dateFilter.GetTimeAgo());

            return string.Format("{0} {1}",
                QueryConstants.ChangesetsBeginningQuery,
                whereClause);
        }
    }
}
