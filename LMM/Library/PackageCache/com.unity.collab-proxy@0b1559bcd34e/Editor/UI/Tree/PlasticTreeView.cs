using System;
using System.Collections.Generic;
using Unity.PlasticSCM.Editor.UnityInternals.UnityEditor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

namespace Unity.PlasticSCM.Editor.UI.Tree
{
    internal class PlasticTreeView : TreeView
    {
        internal PlasticTreeView(bool showCustomBackground = true)
            : base(new TreeViewState())
        {
            mShowCustomBackground = showCustomBackground;

            rowHeight = UnityConstants.TREEVIEW_ROW_HEIGHT;
            treeViewRect = new Rect(0, 0, 0, rowHeight);
            showAlternatingRowBackgrounds = false;
        }

        public override IList<TreeViewItem> GetRows()
        {
            return mRows;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            bool currentlyHasFocus = HasKeyboardFocus();

            if (currentlyHasFocus && !mPreviouslyHadFocus)
            {
                OnFocusGained();
            }

            mPreviouslyHadFocus = currentlyHasFocus;
        }

        internal Rect GetTreeViewRect()
        {
            return treeViewRect;
        }

        internal Rect GetRowRectByIndex(int rowIndex)
        {
            return GetRowRect(rowIndex);
        }

        internal bool HasKeyboardFocus()
        {
            if (PlasticApp.IsUnitTesting)
                return true;

            bool hasKeyFocus = InternalEditorGUIUtility.HasCurrentWindowKeyFocus();

            return hasKeyFocus && treeViewControlID == GUIUtility.keyboardControl && GUI.enabled;
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem(0, -1, string.Empty);
        }

        protected override void BeforeRowsGUI()
        {
            DrawTreeViewItem.InitializeStyles();

            if (!mShowCustomBackground)
                return;

            int firstRowVisible;
            int lastRowVisible;
            GetFirstAndLastVisibleRows(out firstRowVisible, out lastRowVisible);

            GUI.DrawTexture(new Rect(0,
                firstRowVisible * rowHeight,
                GetRowRect(0).width + 1000,
                (lastRowVisible * rowHeight) + 1000),
                Images.GetTreeviewBackgroundTexture());

            base.BeforeRowsGUI();
        }

        protected virtual void OnFocusGained()
        {
            if (!HasSelection())
                return;

            SelectionChanged(GetSelection());
        }

        bool mPreviouslyHadFocus = false;

        readonly bool mShowCustomBackground;

        protected List<TreeViewItem> mRows = new List<TreeViewItem>();
    }
}
