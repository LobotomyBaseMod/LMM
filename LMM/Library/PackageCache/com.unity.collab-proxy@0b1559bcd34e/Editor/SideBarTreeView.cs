using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

namespace Unity.PlasticSCM.Editor
{
    internal class SideBarTreeView : TreeView
    {
        internal SideBarTreeView(
            RepositorySpec repSpec,
            bool  isGluonMode,
            Action<ViewSwitcher.TabType> switchSwitchViewAction)
            : base(new TreeViewState())
        {
            mRepSpec = repSpec;
            mIsGluonMode = isGluonMode;
            mSwitchViewAction = switchSwitchViewAction;
            rowHeight = EditorGUIUtility.singleLineHeight;

            Reload();
        }

        internal void SetHistoryVisible(bool isVisible)
        {
            if (mIsHistoryItemVisible == isVisible)
                return;

            mIsHistoryItemVisible = isVisible;
            Reload();
        }

        internal void SetMergeVisible(bool isVisible)
        {
            if (mIsMergeItemVisible == isVisible)
                return;

            mIsMergeItemVisible = isVisible;
            Reload();
        }

        internal void SetSelectedItem(ViewSwitcher.TabType tabType)
        {
            if (mIsSelectionChanging)
                return;

            mIsSettingSelectedTab = true;

            if (tabType == ViewSwitcher.TabType.History)
                SetHistoryVisible(true);

            if (tabType == ViewSwitcher.TabType.Merge)
                SetMergeVisible(true);

            try
            {
                foreach (var item in rootItem.children)
                {
                    var sideBarItem = item as SideBarTreeViewItem;

                    if (sideBarItem == null)
                        continue;

                    if (sideBarItem.TabType != tabType)
                        continue;

                    SetSelection(new List<int> { sideBarItem.id });
                    FrameItem(sideBarItem.id);

                    break;
                }
            }
            finally
            {
                mIsSettingSelectedTab = false;
            }
        }

        internal float GetTotalWidth()
        {
            if (mTotalWidth == -1)
            {
                mTotalWidth = MeasureMaxWidth.ForTexts(
                    EditorStyles.label,
                    PlasticLocalization.GetString(PlasticLocalization.Name.PendingChangesViewTitle),
                    PlasticLocalization.GetString(PlasticLocalization.Name.IncomingChangesViewTitle),
                    PlasticLocalization.GetString(PlasticLocalization.Name.ChangesetsViewTitle),
                    PlasticLocalization.GetString(PlasticLocalization.Name.ShelvesViewTitle),
                    PlasticLocalization.GetString(PlasticLocalization.Name.BranchesViewTitle),
                    PlasticLocalization.GetString(PlasticLocalization.Name.Labels),
                    PlasticLocalization.GetString(PlasticLocalization.Name.LocksViewTitle),
                    PlasticLocalization.GetString(PlasticLocalization.Name.History),
                    PlasticLocalization.GetString(PlasticLocalization.Name.Merge));

                mTotalWidth += LEFT_MARGIN + ICON_SIZE + ICON_MARGIN + RIGHT_MARGIN;
            }

            return mTotalWidth;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as SideBarTreeViewItem;

            if (item == null)
                return;

            var contentRect = args.rowRect;
            contentRect.x += LEFT_MARGIN;
            contentRect.width -= LEFT_MARGIN;

            var iconRect = contentRect;
            iconRect.width = ICON_SIZE;
            iconRect.height = ICON_SIZE;
            iconRect.y += (contentRect.height - ICON_SIZE) * 0.5f;

            Texture icon = GetItemIcon(item.TabType);

            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon);
            }

            var labelRect = contentRect;
            labelRect.x += ICON_SIZE + ICON_MARGIN;
            labelRect.width -= ICON_SIZE + ICON_MARGIN;

            GUI.Label(labelRect, item.displayName, EditorStyles.label);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = -1, depth = -1 };
            root.children = new List<TreeViewItem>();

            root.AddChild(new SideBarTreeViewItem
            {
                id = 0,
                depth = 0,
                displayName = PlasticLocalization.Name.PendingChangesViewTitle.GetString(),
                TabType = ViewSwitcher.TabType.PendingChanges,
            });
            root.AddChild(new SideBarTreeViewItem
            {
                id = 2,
                depth = 0,
                displayName = PlasticLocalization.Name.IncomingChangesViewTitle.GetString(),
                TabType = ViewSwitcher.TabType.IncomingChanges,
            });
            root.AddChild(new SideBarTreeViewItem
            {
                id = 3,
                depth = 0,
                displayName = PlasticLocalization.Name.ChangesetsViewTitle.GetString(),
                TabType = ViewSwitcher.TabType.Changesets,
            });
            root.AddChild(new SideBarTreeViewItem
            {
                id = 4,
                depth = 0,
                displayName = PlasticLocalization.Name.ShelvesViewTitle.GetString(),
                TabType = ViewSwitcher.TabType.Shelves,
            });
            root.AddChild(new SideBarTreeViewItem
            {
                id = 5,
                depth = 0,
                displayName = PlasticLocalization.Name.BranchesViewTitle.GetString(),
                TabType = ViewSwitcher.TabType.Branches,
            });

            if (!mIsGluonMode)
            {
                root.AddChild(new SideBarTreeViewItem
                {
                    id = 6,
                    depth = 0,
                    displayName = PlasticLocalization.Name.Labels.GetString(),
                    TabType = ViewSwitcher.TabType.Labels,
                });
            }

            root.AddChild(new SideBarTreeViewItem
            {
                id = 7,
                depth = 0,
                displayName = PlasticLocalization.Name.LocksViewTitle.GetString(),
                TabType = ViewSwitcher.TabType.Locks,
            });

            if (mIsHistoryItemVisible)
            {
                root.AddChild(new SideBarTreeViewItem
                {
                    id = 8,
                    depth = 0,
                    displayName = PlasticLocalization.Name.History.GetString(),
                    TabType = ViewSwitcher.TabType.History,
                });
            }

            if (mIsMergeItemVisible)
            {
                root.AddChild(new SideBarTreeViewItem
                {
                    id = 9,
                    depth = 0,
                    displayName = PlasticLocalization.Name.Merge.GetString(),
                    TabType = ViewSwitcher.TabType.Merge,
                });
            }

            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (mIsSettingSelectedTab)
                return;

            if (selectedIds.Count == 0)
                return;

            var item = FindItem(selectedIds[0], rootItem) as SideBarTreeViewItem;

            if (item == null)
                return;

            mIsSelectionChanging = true;

            if (item.TabType == ViewSwitcher.TabType.Shelves)
            {
                TrackFeatureUseEvent.For(
                    mRepSpec,
                    TrackFeatureUseEvent.Features.UnityPackage.OpenShelvesView);
            }

            // Defer the view switch to the next editor frame to avoid a GUILayout group mismatch
            // when the selection changes during a MouseDown event.
            ViewSwitcher.TabType tabType = item.TabType;
            EditorApplication.delayCall += () =>
            {
                try
                {
                    mSwitchViewAction(tabType);
                }
                finally
                {
                    mIsSelectionChanging = false;
                }
            };
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            // Disable multi-selection
            return false;
        }

        Texture GetItemIcon(ViewSwitcher.TabType tabType)
        {
            switch (tabType)
            {
                case ViewSwitcher.TabType.PendingChanges:
                    return Images.GetPendingChangesViewIcon();
                case ViewSwitcher.TabType.IncomingChanges:
                    return Images.GetIncomingChangesViewIcon();
                case ViewSwitcher.TabType.Changesets:
                    return Images.GetChangesetsIcon();
                case ViewSwitcher.TabType.Shelves:
                    return Images.GetShelveIcon();
                case ViewSwitcher.TabType.Branches:
                    return Images.GetBranchesIcon();
                case ViewSwitcher.TabType.Labels:
                    return Images.GetLabelIcon();
                case ViewSwitcher.TabType.Locks:
                    return Images.GetLockIcon();
                case ViewSwitcher.TabType.History:
                    return Images.GetHistoryIcon();
                case ViewSwitcher.TabType.Merge:
                    return Images.GetMergeViewIcon();
            }

            return null;
        }

        class SideBarTreeViewItem : TreeViewItem
        {
            public ViewSwitcher.TabType TabType { get; set; }
        }

        bool mIsSelectionChanging;
        bool mIsSettingSelectedTab;
        bool mIsHistoryItemVisible = false;
        bool mIsMergeItemVisible = false;
        float mTotalWidth = -1;

        readonly Action<ViewSwitcher.TabType> mSwitchViewAction;
        readonly bool mIsGluonMode;
        readonly RepositorySpec mRepSpec;

        const int ICON_SIZE = 16;
        const int ICON_MARGIN = 4;
        const int LEFT_MARGIN = 15;
        const int RIGHT_MARGIN = 5;
    }
}
