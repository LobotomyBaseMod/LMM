using System.Collections.Generic;
using System.Linq;

using Codice.CM.Common;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal static class ChangesetsSelection
    {
        internal static void SelectChangesets(
            ChangesetsListView listView,
            List<RepObjectInfo> csetsToSelect,
            int defaultRow)
        {
            if (csetsToSelect == null || csetsToSelect.Count == 0)
            {
                TableViewOperations.SelectFirstRow(listView);
                return;
            }

            listView.SelectRepObjectInfos(csetsToSelect);

            if (listView.HasSelection())
                return;

            TableViewOperations.SelectDefaultRow(listView, defaultRow);

            if (listView.HasSelection())
                return;

            TableViewOperations.SelectFirstRow(listView);
        }

        internal static List<RepObjectInfo> GetSelectedRepObjectInfos(
            ChangesetsListView listView)
        {
            return listView.GetSelectedRepObjectInfos();
        }

        internal static int GetSelectedChangesetsCount(
            ChangesetsListView listView)
        {
            return listView.GetSelection().Count;
        }

        internal static ChangesetExtendedInfo GetSelectedChangeset(
            ChangesetsListView listView)
        {
            List<RepObjectInfo> selectedRepObjectsInfos = listView.GetSelectedRepObjectInfos();

            if (selectedRepObjectsInfos.Count == 0)
                return null;

            return (ChangesetExtendedInfo)selectedRepObjectsInfos[0];
        }

        internal static RepositorySpec GetSelectedRepository(
            ChangesetsListView listView)
        {
            List<RepositorySpec> selectedRepositories = listView.GetSelectedRepositories();

            if (selectedRepositories.Count == 0)
                return null;

            return selectedRepositories[0];
        }

        internal static List<RepObjectInfo> GetChangesetsToSelect(
            ChangesetsListView listView, List<object> entriesToSelect)
        {
            if (entriesToSelect == null || entriesToSelect.Count == 0)
                return GetSelectedRepObjectInfos(listView);

            return entriesToSelect.Cast<RepObjectInfo>().ToList();
        }
    }
}
