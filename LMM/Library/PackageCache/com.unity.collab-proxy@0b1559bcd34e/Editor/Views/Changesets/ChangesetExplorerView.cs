using System;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.CM.Common;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Labels.Dialogs;
using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.QueryViews.Changesets;

namespace Unity.PlasticSCM.Editor.Views.Changesets
{
    internal class ChangesetExplorerView :
        IGetQueryText,
        IGetFilterText,
        FillChangesetsView.IShowContentView
    {
        internal ChangesetsListView Table => mChangesetsListView;
        internal ChangesetExplorerView(
            CreateLabelDialog parentWindow,
            WorkspaceInfo workspaceInfo,
            ProgressControlsForDialogs progressControls)
        {
            mParentWindow = parentWindow;
            mWkInfo = workspaceInfo;
            mProgressControls = progressControls;
            mEmptyStatePanel = new EmptyStatePanel(parentWindow.Repaint);

            mFillChangesetsView = new FillChangesetsView(
                mWkInfo,
                null,
                null,
                this,
                this,
                this);

            BuildComponents(mFillChangesetsView);

            Refresh();
        }

        internal void OnGUI()
        {
            bool isEnabled = !mProgressControls.ProgressData.IsWaitingAsyncResult;

            DoToolbarArea(
                mSearchField,
                mChangesetsListView,
                isEnabled,
                Refresh);

            DoListArea(
                mChangesetsListView,
                mEmptyStatePanel,
                isEnabled,
                mParentWindow.Repaint);
        }

        string IGetQueryText.Get()
        {
            return GetChangesetsQuery.For(mDateFilter);
        }

        string IGetFilterText.Get()
        {
            return mChangesetsListView.searchString;
        }

        void IGetFilterText.Clear()
        {
            // Not used by the Plugin, needed for the Reset filters button
        }

        void FillChangesetsView.IShowContentView.ShowContentPanel()
        {
            mEmptyStatePanel.UpdateContent(string.Empty);
        }

        void FillChangesetsView.IShowContentView.ShowEmptyStatePanel(
            string explanationText, bool showResetFilterButton)
        {
            mEmptyStatePanel.UpdateContent(explanationText);
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mChangesetsListView.SetFocusAndEnsureSelectedItem();
        }

        void Refresh()
        {
            mFillChangesetsView.FillView(
                mChangesetsListView,
                mProgressControls,
                null,
                null,
                null,
                null);
        }

        void DoToolbarArea(
            SearchField searchField,
            ChangesetsListView listView,
            bool isEnabled,
            Action refreshAction)
        {
            GUILayout.BeginVertical();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = isEnabled;

                if (GUILayout.Button(PlasticLocalization.GetString(
                        PlasticLocalization.Name.RefreshButton), EditorStyles.miniButton))
                {
                    refreshAction();
                }

                DrawDateFilter();

                GUILayout.FlexibleSpace();

                DrawSearchField.For(searchField, listView, SEARCH_FIELD_WIDTH);

                GUI.enabled = true;
            }

            GUILayout.Space(10);

            GUILayout.EndVertical();
        }

        void DrawDateFilter()
        {
            EditorGUI.BeginChangeCheck();

            mDateFilter.FilterType = (DateFilter.Type)
                EditorGUILayout.EnumPopup(mDateFilter.FilterType, GUILayout.Width(100));

            if (EditorGUI.EndChangeCheck())
                Refresh();
        }

        void DoListArea(
            ChangesetsListView listView,
            EmptyStatePanel emptyStatePanel,
            bool isEnabled,
            Action repaint)
        {
            GUILayout.BeginVertical();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = isEnabled;

                Rect treeRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

                listView.OnGUI(treeRect);

                if (Event.current.type == EventType.Layout)
                    mShouldShowEmptyState = !emptyStatePanel.IsEmpty();

                if (mShouldShowEmptyState)
                    emptyStatePanel.OnGUI(treeRect);

                GUI.enabled = true;

                GUILayout.Space(5);
            }

            GUILayout.Space(10);

            GUILayout.EndVertical();
        }

        void BuildComponents(FillChangesetsView fillChangesetsView)
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            DateFilter.Type dateFilterType =
                EnumPopupSetting<DateFilter.Type>.Load(
                    UnityConstants.CHANGESETS_DATE_FILTER_SETTING_NAME,
                    DateFilter.Type.LastMonth);
            mDateFilter = new DateFilter(dateFilterType);

            ChangesetsListHeaderState headerState =
                ChangesetsListHeaderState.GetDefault();
            TreeHeaderSettings.Load(headerState,
                UnityConstants.CHANGESETS_TABLE_SETTINGS_NAME,
                (int)ChangesetsListColumn.CreationDate);

            mChangesetsListView = new ChangesetsListView(
                headerState,
                ChangesetsListHeaderState.GetColumnNames(),
                null,
                fillChangesetsView,
                fillChangesetsView,
                delayedSelectionChangedAction: () => {},
                doubleClickAction: mParentWindow.OkButtonAction,
                afterItemsChangedAction: fillChangesetsView.ShowContentOrEmptyState);

            mChangesetsListView.Reload();
        }

        bool mShouldShowEmptyState;
        DateFilter mDateFilter;
        SearchField mSearchField;
        ChangesetsListView mChangesetsListView;

        readonly CreateLabelDialog mParentWindow;
        readonly EmptyStatePanel mEmptyStatePanel;
        readonly FillChangesetsView mFillChangesetsView;
        readonly WorkspaceInfo mWkInfo;
        readonly ProgressControlsForDialogs mProgressControls;

        const float SEARCH_FIELD_WIDTH = 450;
    }
}
