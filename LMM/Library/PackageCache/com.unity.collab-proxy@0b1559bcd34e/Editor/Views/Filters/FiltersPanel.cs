using UnityEngine.UIElements;

using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Configuration;
using PlasticGui.WorkspaceWindow.Filters;
using PlasticGui.WorkspaceWindow.Filters.Date;
using Unity.PlasticSCM.Editor.Views.Filters.Date;
using LayoutFilters = Codice.Client.BaseCommands.LayoutFilters;

namespace Unity.PlasticSCM.Editor.Views.Filters
{
    internal class FiltersPanel : VisualElement
    {
        internal DateFilterButtonPanel DateFilterButtonPanel { get { return mDateFilterButtonPanel; } }

        internal interface IFilterableView
        {
            void ApplyFilter();
            bool ApplyDateFilter(LayoutFilters.DateFilter oldFilter, LayoutFilters.DateFilter newFilter);
        }

        internal FiltersPanel(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            FilterableViewType viewType,
            IFilterableView filterableView)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            BuildComponents(wkInfo, repSpec, viewType, filterableView);
        }

        internal void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mDateFilterButtonPanel.UpdateRepositorySpec(repSpec);
        }

        internal void SetWorkspaceUIConfiguration(WorkspaceUIConfiguration config)
        {
            mDateFilterButtonPanel.SetWorkspaceUIConfiguration(config);
        }

        internal void LoadConfiguration()
        {
            mDateFilterButtonPanel.LoadFilters();
        }

        void BuildComponents(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            FilterableViewType viewType,
            IFilterableView filterableView)
        {
            mDateFilterButtonPanel = new DateFilterButtonPanel(
                repSpec,
                filterableView,
                new DateFilterConfig(wkInfo, viewType));
            Add(mDateFilterButtonPanel);
        }

        DateFilterButtonPanel mDateFilterButtonPanel;
    }
}
