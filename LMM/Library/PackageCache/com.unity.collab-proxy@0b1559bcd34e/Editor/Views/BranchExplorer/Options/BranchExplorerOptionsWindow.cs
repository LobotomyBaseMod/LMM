using System;
using System.Collections.Generic;
using Codice.CM.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Options.ConditionalFormat;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Options.DisplayOptions;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Options
{
    internal class BranchExplorerOptionsWindow : EditorWindow
    {
        internal interface IBranchExplorerView
        {
            void Redraw();
            void Refresh();
            void ClearSearchResults();
        }

        internal static void ShowWindow()
        {
            BranchExplorerOptionsWindow window =
                GetWindow<BranchExplorerOptionsWindow>(
                    false,
                    PlasticLocalization.GetString(
                        PlasticLocalization.Name.BrexOptionsWindowTitle));

            window.titleContent.image = Images.GetSettingsIcon();
            window.minSize = new Vector2(545, 320);
            window.Show();
        }

        void OnEnable()
        {
            mUVCSPlugin = UVCSPlugin.Instance;
            mUVCSPlugin.Enable();

            if (!mUVCSPlugin.ConnectionMonitor.IsConnected)
                return;

            mWkInfo = FindWorkspace.InfoForApplicationPath(
                ApplicationDataPath.Get(), PlasticGui.Plastic.API);
        }

        void CreateGUI()
        {
            if (TryInitialize())
                return;

            // On domain reload, the BranchExplorerWindow may not have
            // finished its own CreateGUI yet
            EditorApplication.delayCall += DelayedInitialize;
        }

        void DelayedInitialize()
        {
            if (!TryInitialize())
                Close();
        }

        bool TryInitialize()
        {
            BranchExplorerWindow brExWindow = GetWindowIfOpened.BranchExplorer();

            if (brExWindow == null || mWkInfo == null)
                return false;

            mBrExWindow = brExWindow;
            BuildComponents();
            LoadConfiguration();
            return true;
        }

        void OnDestroy()
        {
            if (mSectionListView != null)
#pragma warning disable CS0618 // onSelectionChange is obsolete in newer Unity but needed for backward compatibility
                mSectionListView.onSelectionChange -= OnSectionSelectionChanged;
#pragma warning restore CS0618

            if (mDisplayOptionsPanel != null)
                mDisplayOptionsPanel.Dispose();

            if (mFiltersAndConditionalFormatPanel != null)
                mFiltersAndConditionalFormatPanel.Dispose();
        }

        void LoadConfiguration()
        {
            WorkspaceUIConfiguration config =
                WorkspaceUIConfiguration.Get(mWkInfo);

            if (config == null)
                return;

            mDisplayOptionsPanel.SetWorkspaceUIConfiguration(config);
            mDisplayOptionsPanel.LoadConfiguration();

            mFiltersAndConditionalFormatPanel.SetWorkspaceUIConfiguration(config);
            mFiltersAndConditionalFormatPanel.LoadConfigRules();
        }

        void BuildComponents()
        {
            rootVisualElement.Clear();

            TwoPaneSplitView splitView = new TwoPaneSplitView(
                0,
                LEFT_PANE_INITIAL_WIDTH,
                TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;

            VisualElement leftPane = CreateLeftPane();
            VisualElement rightPane = CreateRightPane();

            splitView.Add(leftPane);
            splitView.Add(rightPane);

            SetSplitViewDragLineColor(splitView);

            rootVisualElement.Add(splitView);

            mSectionListView.selectedIndex = DISPLAY_OPTIONS_INDEX;
        }

        VisualElement CreateLeftPane()
        {
            VisualElement leftPane = new VisualElement();
            leftPane.style.marginTop = 4;
            leftPane.style.minWidth = 140;

            mSectionNames = new List<string>
            {
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.BrexDisplayOptions),
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.FiltersAndConditionalFormat)
            };

            mSectionListView = new ListView();
            mSectionListView.itemsSource = mSectionNames;
            mSectionListView.makeItem = CreateSectionItem;
            mSectionListView.bindItem = BindSectionItem;
            mSectionListView.fixedItemHeight = 16;
            mSectionListView.selectionType = SelectionType.Single;
            mSectionListView.style.flexGrow = 1;

#pragma warning disable CS0618 // onSelectionChange is obsolete in newer Unity but needed for backward compatibility
            mSectionListView.onSelectionChange += OnSectionSelectionChanged;
#pragma warning restore CS0618

            leftPane.Add(mSectionListView);

            return leftPane;
        }

        VisualElement CreateRightPane()
        {
            mRightPane = new VisualElement();
            mRightPane.style.flexGrow = 1;

            mSectionHeader = new Label();
            mSectionHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            mSectionHeader.style.fontSize = 18;
            mSectionHeader.style.paddingLeft = 12;
            mSectionHeader.style.paddingTop = 8;
            mSectionHeader.style.paddingBottom = 8;
            mRightPane.Add(mSectionHeader);

            mDisplayOptionsPanel = new DisplayOptionsPanel(
                mWkInfo,
                GetBranchExplorerView);
            mDisplayOptionsPanel.style.display = DisplayStyle.None;
            mRightPane.Add(mDisplayOptionsPanel);

            mFiltersAndConditionalFormatPanel = new FiltersAndConditionalFormatPanel(
                mWkInfo,
                GetBranchExplorerView);
            mFiltersAndConditionalFormatPanel.style.display = DisplayStyle.None;
            mRightPane.Add(mFiltersAndConditionalFormatPanel);

            return mRightPane;
        }

        VisualElement CreateSectionItem()
        {
            Label label = new Label();
            label.style.paddingLeft = 18;
            label.style.paddingRight = 8;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        void BindSectionItem(VisualElement element, int index)
        {
            ((Label)element).text = mSectionNames[index];
        }

        void OnSectionSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (mSectionListView.selectedIndex < 0)
                return;

            SelectSection(mSectionListView.selectedIndex);
        }

        void SelectSection(int sectionIndex)
        {
            mSelectedSectionIndex = sectionIndex;
            UpdateRightPaneContent();
        }

        void UpdateRightPaneContent()
        {
            mDisplayOptionsPanel.style.display = DisplayStyle.None;
            mFiltersAndConditionalFormatPanel.style.display = DisplayStyle.None;

            switch (mSelectedSectionIndex)
            {
                case DISPLAY_OPTIONS_INDEX:
                    mSectionHeader.text = PlasticLocalization.GetString(
                        PlasticLocalization.Name.BrexDisplayOptions);
                    mDisplayOptionsPanel.style.display = DisplayStyle.Flex;
                    break;

                case FILTERS_AND_CONDITIONAL_FORMAT_INDEX:
                    mSectionHeader.text = PlasticLocalization.GetString(
                        PlasticLocalization.Name.FiltersAndConditionalFormat);
                    mFiltersAndConditionalFormatPanel.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        static void SetSplitViewDragLineColor(TwoPaneSplitView splitView)
        {
            VisualElement dragLineAnchor =
                splitView.Q("unity-dragline-anchor");

            if (dragLineAnchor == null)
                return;

            dragLineAnchor.style.backgroundColor =
                UnityStyles.Colors.SplitLineColor;
        }

        IBranchExplorerView GetBranchExplorerView()
        {
            return mBrExWindow.BranchExplorerView;
        }

        WorkspaceInfo mWkInfo;
        BranchExplorerWindow mBrExWindow;
        int mSelectedSectionIndex;

        VisualElement mRightPane;
        Label mSectionHeader;
        ListView mSectionListView;
        List<string> mSectionNames;

        DisplayOptionsPanel mDisplayOptionsPanel;
        FiltersAndConditionalFormatPanel mFiltersAndConditionalFormatPanel;

        UVCSPlugin mUVCSPlugin;

        const int DISPLAY_OPTIONS_INDEX = 0;
        const int FILTERS_AND_CONDITIONAL_FORMAT_INDEX = 1;
        const float LEFT_PANE_INITIAL_WIDTH = 180;
    }
}
