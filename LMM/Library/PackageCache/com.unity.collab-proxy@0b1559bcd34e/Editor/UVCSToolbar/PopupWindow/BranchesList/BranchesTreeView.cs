using System;
using System.Collections.Generic;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Topbar.WorkingObjectInfo.BranchesList;
using Unity.PlasticSCM.Editor.UI;

using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList
{
    internal class BranchesTreeView : TreeView
    {
        internal interface IClickListener
        {
            void OnItemClicked(BranchTreeViewItem item);
        }

        internal BranchesTreeView(IClickListener listener) : base(new TreeViewState())
        {
            mClickListener = listener;
        }

        internal void SetWorkingBranch(Func<BranchInfo> fetchWorkingBranch)
        {
            mFetchWorkingBranch = fetchWorkingBranch;
        }

        internal void SetBranches(ClassifiedBranchesList branches)
        {
            mBranches = branches;

            if (!string.IsNullOrEmpty(mSearchString))
            {
                SetSearchString(mSearchString);
                return;
            }

            Reload();
        }

        internal void SetSearchString(string searchString)
        {
            mSearchString = searchString;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            if (IsCategory(item))
                return 26;
            else
                return 38f;
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove)
            {
                Vector2 localMousePos = Event.current.mousePosition - rect.position + state.scrollPos;

                int newHoveredId = GetHoveredItemId(localMousePos);
                if (newHoveredId != mLastHoveredItemId)
                {
                    mLastHoveredItemId = newHoveredId;
                    Repaint();
                }
            }

            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            Rect rowRect = args.rowRect;
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (rowRect.Contains(evt.mousePosition) &&
                        evt.button == 0)
                    {
                        mClickedItemId = args.item.id;
                        evt.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (rowRect.Contains(evt.mousePosition) &&
                        evt.button == 0 &&
                        mClickedItemId == args.item.id &&
                        !IsCategory(args.item))
                    {
                        mClickedItemId = -1;
                        evt.Use();
                        mClickListener.OnItemClicked((BranchTreeViewItem)args.item);
                    }

                    break;

                case EventType.Repaint:
                    DrawRowGUI(args);
                    break;
            }
        }

        bool IsCategory(TreeViewItem item)
        {
            return item is CategoryTreeViewItem;
        }

        void DrawRowGUI(RowGUIArgs args)
        {
            if (IsCategory(args.item))
            {
                DrawCategory(args);
                return;
            }

            DrawBranch(args);
        }

        void DrawBranch(RowGUIArgs args)
        {
            bool isHover = args.rowRect.Contains(Event.current.mousePosition);

            GUIStyle titleStyle = isHover ?
                UnityStyles.EditorToolbar.Popup.BranchesList.TitleHover :
                UnityStyles.EditorToolbar.Popup.BranchesList.Title;
            GUIStyle descriptionStyle = isHover ?
                UnityStyles.EditorToolbar.Popup.BranchesList.DescriptionHover :
                UnityStyles.EditorToolbar.Popup.BranchesList.Description;
            GUIStyle timeAgoStyle = isHover ?
                UnityStyles.EditorToolbar.Popup.BranchesList.TimeAgoHover :
                UnityStyles.EditorToolbar.Popup.BranchesList.TimeAgo;

            if (isHover)
            {
                GUI.Label(args.rowRect, GUIContent.none, UnityStyles.EditorToolbar.Popup.Hover);
            }

            BranchTreeViewItem branchItem = (BranchTreeViewItem)args.item;

            bool isWorkingBranch = branchItem.BranchInfo.Equals(mFetchWorkingBranch());
            Texture icon = isWorkingBranch ? Images.GetCurrentBranchIcon() : Images.GetBranchIcon();

            GUIContent titleContent = new GUIContent(args.item.displayName, icon);
            GUIContent descriptionContent = new GUIContent(branchItem.FormattedComment);

            string tooltip = string.Format("{0}, {1}",
                branchItem.BranchInfo.LocalTimeStamp.ToLongDateString(),
                branchItem.BranchInfo.LocalTimeStamp.ToLongTimeString());
            GUIContent timeAgoContent = new GUIContent(Codice.Client.Common.Time.GetTimeAgoString(
                branchItem.BranchInfo.LocalTimeStamp), tooltip);

            Vector2 titleContentSize = titleStyle.CalcSize(titleContent);
            Vector2 descriptionContentSize = descriptionStyle.CalcSize(descriptionContent);
            Vector2 timeAgoSize = timeAgoStyle.CalcSize(timeAgoContent);

            Rect timeAgoRect = new Rect(
                args.rowRect.x + args.rowRect.width - timeAgoSize.x,
                args.rowRect.y + (args.rowRect.height - timeAgoSize.y) / 2,
                timeAgoSize.x,
                timeAgoSize.y);

            float titleAndDescriptionHeight = titleContentSize.y + descriptionContentSize.y;

            Rect titleRect = new Rect(
                args.rowRect.x,
                args.rowRect.y + (args.rowRect.height - titleAndDescriptionHeight) / 2,
                args.rowRect.width - timeAgoSize.x,
                titleContentSize.y);

            Rect desciptionRect = new Rect(
                args.rowRect.x,
                args.rowRect.y + (args.rowRect.height - titleAndDescriptionHeight) / 2 + titleContentSize.y,
                args.rowRect.width - timeAgoSize.x,
                descriptionContentSize.y);

            // add a tooltip if title or description is not completely displayed
            if (titleContentSize.x > args.rowRect.width - timeAgoSize.x)
                titleContent.tooltip = titleContent.text;

            if (descriptionContentSize.x > args.rowRect.width - timeAgoSize.x)
                descriptionContent.tooltip = descriptionContent.text;

            DrawContentWithSearchString(
                titleRect,
                titleContent,
                mSearchString,
                titleStyle);

            DrawContentWithSearchString(
                desciptionRect,
                descriptionContent,
                mSearchString,
                descriptionStyle);

            GUI.Label(timeAgoRect, timeAgoContent, timeAgoStyle);
        }

        static void DrawCategory(RowGUIArgs args)
        {
            GUI.Label(
                args.rowRect,
                args.item.displayName,
                UnityStyles.EditorToolbar.Popup.BranchesList.Category);
        }

        static void DrawContentWithSearchString(
            Rect rect,
            GUIContent content,
            string searchString,
            GUIStyle style)
        {
            int titleSearchStringIndex = string.IsNullOrEmpty(searchString)
                ? -1
                : content.text.IndexOf(searchString, StringComparison.Ordinal);

            float iconWidth = content.image != null ? 16 : 0f;

            Rect iconRect = new Rect(
                rect.x + style.padding.left,
                rect.y,
                iconWidth,
                rect.height);

            Rect textRect = new Rect(
                rect.x + iconWidth + style.margin.left,
                rect.y,
                rect.width - iconWidth,
                rect.height);

            if (content.image != null)
            {
                GUI.DrawTexture(iconRect, content.image);
            }

            if (titleSearchStringIndex == -1)
            {
                GUI.Label(textRect, new GUIContent(content.text, content.tooltip), style);
            }
            else
            {
                style.DrawWithTextSelection(
                    textRect,
                    new GUIContent(content.text, content.tooltip),
                    -1,
                    titleSearchStringIndex,
                    titleSearchStringIndex + searchString.Length
                );
            }
        }

        int GetHoveredItemId(Vector2 mousePos)
        {
            int firstVisibleRow,
                lastVisibleRow;

            this.GetFirstAndLastVisibleRows(out firstVisibleRow, out lastVisibleRow);

            for (int i = firstVisibleRow; i <= lastVisibleRow; i++)
            {
                Rect rowRect = GetRowRect(i);

                if (rowRect.Contains(mousePos))
                {
                    return i;
                }
            }

            return -1; // No hover
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = -1, depth = -1 };
            root.children = new List<TreeViewItem>();

            if (mBranches == null)
                return root;

            int currentId = -1;

            if (mBranches.IsMainBranchVisible)
            {
                root.AddChild(new CategoryTreeViewItem(
                    currentId++,
                    PlasticLocalization.Name.MainBranchSection.GetString()));
                root.AddChild(new BranchTreeViewItem(currentId++, mBranches.MainBranch));
            }

            if (mBranches.AreRecentBranchesVisible)
            {
                root.AddChild(new CategoryTreeViewItem(
                    currentId++,
                    PlasticLocalization.Name.RecentBranchesSection.GetString()));
                foreach (BranchInfo branch in mBranches.RecentBranches)
                {
                    root.AddChild(new BranchTreeViewItem(currentId++, branch));
                }
            }

            if (mBranches.AreOtherBranchesVisible)
            {
                root.AddChild(new CategoryTreeViewItem(
                    currentId++,
                    PlasticLocalization.Name.OtherBranchesSection.GetString()));
                foreach (BranchInfo branch in mBranches.OtherBranches)
                {
                    root.AddChild(new BranchTreeViewItem(currentId++, branch));
                }
            }

            return root;
        }

        readonly IClickListener mClickListener;
        int mClickedItemId;
        string mSearchString;
        ClassifiedBranchesList mBranches;
        Func<BranchInfo> mFetchWorkingBranch;
        int mLastHoveredItemId = -1;
    }

    class BranchTreeViewItem : TreeViewItem
    {
        string mFormattedComment;
        internal BranchInfo BranchInfo { get; private set; }

        public string FormattedComment
        {
            get
            {
                if (string.IsNullOrEmpty(mFormattedComment))
                    mFormattedComment = GetFormattedComment(BranchInfo.Comment);

                return mFormattedComment;
            }
        }

        internal BranchTreeViewItem(int id, BranchInfo branchInfo)
        {
            this.id = id;
            this.displayName = branchInfo.Name;

            BranchInfo = branchInfo;
        }

        static string GetFormattedComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return PlasticLocalization.Name.NoCommentSet.GetString();
            return CommentFormatter.GetFormattedComment(comment);
        }
    }

    class CategoryTreeViewItem : TreeViewItem
    {
        internal CategoryTreeViewItem(int id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
        }
    }
}
