using System.Collections.Generic;
using System.Linq;

using Codice.CM.Common;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Views.Labels
{
    internal static class LabelsSelection
    {
        internal static void SelectLabels(
            LabelsListView listView,
            List<RepObjectInfo> labelsToSelect,
            int defaultRow)
        {
            if (labelsToSelect == null || labelsToSelect.Count == 0)
            {
                TableViewOperations.SelectFirstRow(listView);
                return;
            }

            listView.SelectRepObjectInfos(labelsToSelect);

            if (listView.HasSelection())
                return;

            TableViewOperations.SelectDefaultRow(listView, defaultRow);

            if (listView.HasSelection())
                return;

            TableViewOperations.SelectFirstRow(listView);
        }

        internal static List<RepObjectInfo> GetSelectedRepObjectInfos(
            LabelsListView listView)
        {
            return listView.GetSelectedRepObjectInfos();
        }

        internal static int GetSelectedVisibleLabelsCount(
            LabelsListView listView)
        {
            return listView.GetSelectedVisibleItems().Count;
        }

        internal static MarkerExtendedInfo GetSelectedLabel(
            LabelsListView listView)
        {
            List<RepObjectInfo> selectedRepObjectsInfos = listView.GetSelectedRepObjectInfos();

            if (selectedRepObjectsInfos.Count == 0)
                return null;

            return (MarkerExtendedInfo)selectedRepObjectsInfos[0];
        }

        internal static RepositorySpec GetSelectedRepository(
            LabelsListView listView)
        {
            List<RepositorySpec> selectedRepositories = listView.GetSelectedRepositories();

            if (selectedRepositories.Count == 0)
                return null;

            return selectedRepositories[0];
        }

        internal static List<RepObjectInfo> GetLabelsToSelect(
            LabelsListView labelsListView, List<object> entriesToSelect)
        {
            if (entriesToSelect == null || entriesToSelect.Count == 0)
                return GetSelectedRepObjectInfos(labelsListView);

            return entriesToSelect.Cast<RepObjectInfo>().ToList();
        }
    }
}
