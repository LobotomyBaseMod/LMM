using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Configuration;
using PlasticGui.WorkspaceWindow.Filters;
using Unity.PlasticSCM.Editor.UI;
using LayoutFilters = Codice.Client.BaseCommands.LayoutFilters;

namespace Unity.PlasticSCM.Editor.Views.Filters.Date
{
    internal class DateFilterButtonPanel : VisualElement
    {
        internal DateFilterButtonPanel(
            RepositorySpec repSpec,
            FiltersPanel.IFilterableView filterableView,
            IFilterConfig<LayoutFilters.DateFilter> config)
        {
            mRepSpec = repSpec;
            mFilterableView = filterableView;
            mConfig = config;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            BuildComponents();
        }

        internal void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mRepSpec = repSpec;
        }

        internal void SetWorkspaceUIConfiguration(WorkspaceUIConfiguration config)
        {
            mConfig.SetWorkspaceUIConfiguration(config);
        }

        internal void LoadFilters()
        {
            mCurrentIndex = GetIndexFromFilter(mConfig.GetCurrentFilter());
        }

        void BuildComponents()
        {
            var dateFilterContainer = new IMGUIContainer(() =>
            {
                string[] displayOptions = new string[]
                {
                    "Last Week",
                    "Last 15 Days",
                    "Last Month",
                    "Last 3 Months",
                    "Last Year"
                };

                EditorGUI.BeginChangeCheck();

                int newIndex = EditorGUILayout.Popup(
                    mCurrentIndex,
                    displayOptions,
                    EditorStyles.toolbarDropDown,
                    GUILayout.Width(UnityConstants.TOOLBAR_DATE_FILTER_COMBO_WIDTH),
                    GUILayout.Height(20));

                if (EditorGUI.EndChangeCheck())
                {
                    LayoutFilters.DateFilter oldFilter = mConfig.GetCurrentFilter();
                    LayoutFilters.DateFilter newFilter = GetFilterFromIndex(newIndex);

                    mConfig.SetCurrentFilter(newFilter);

                    if (!mFilterableView.ApplyDateFilter(oldFilter, newFilter))
                        mConfig.SetCurrentFilter(oldFilter);
                }
            });

            Add(dateFilterContainer);
        }

        static LayoutFilters.DateFilter GetFilterFromIndex(int index)
        {
            LayoutFilters.SinceTimeType newFilterType;
            switch (index)
            {
                case 0:
                    newFilterType = LayoutFilters.SinceTimeType.OneWeekAgo;
                    break;
                case 1:
                    newFilterType = LayoutFilters.SinceTimeType.FifteenDaysAgo;
                    break;
                case 2:
                    newFilterType = LayoutFilters.SinceTimeType.OneMonthAgo;
                    break;
                case 3:
                    newFilterType = LayoutFilters.SinceTimeType.ThreeMonthsAgo;
                    break;
                case 4:
                    newFilterType = LayoutFilters.SinceTimeType.OneYearAgo;
                    break;
                default:
                    newFilterType = LayoutFilters.SinceTimeType.OneMonthAgo;
                    break;
            }
            return LayoutFilters.DateFilter.BuildFromTimeAgo(newFilterType);
        }

        static int GetIndexFromFilter(LayoutFilters.DateFilter filter)
        {
            if (filter == null)
                return 2; // Default to LastMonth

            switch (filter.SinceTimeType)
            {
                case LayoutFilters.SinceTimeType.OneWeekAgo:
                    return 0;
                case LayoutFilters.SinceTimeType.FifteenDaysAgo:
                    return 1;
                case LayoutFilters.SinceTimeType.OneMonthAgo:
                    return 2;
                case LayoutFilters.SinceTimeType.ThreeMonthsAgo:
                    return 3;
                case LayoutFilters.SinceTimeType.OneYearAgo:
                    return 4;
                default:
                    return 2; // Default to LastMonth
            }
        }

        int mCurrentIndex;

        // Not used yet, it will be used for the SavedFiltersPanel
        RepositorySpec mRepSpec;

        readonly IFilterConfig<LayoutFilters.DateFilter> mConfig;
        readonly FiltersPanel.IFilterableView mFilterableView;
    }
}
