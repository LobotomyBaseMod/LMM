using System;
using System.IO;
using UnityEngine;

using Codice.Client.Common;
using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Locks;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Tree;

using Time = Codice.Client.Common.Time;

namespace Unity.PlasticSCM.Editor.Views.Locks
{
    internal static class DrawLocksListViewItem
    {
        internal static void ForCell(
            RepositorySpec repSpec,
            string wkPath,
            Rect rect,
            float rowHeight,
            LockInfo lockInfo,
            LocksListColumn column,
            Action avatarLoadedAction,
            bool isSelected,
            bool isFocused)
        {
            var columnText = LockInfoView.GetColumnText(
                repSpec,
                lockInfo,
                LocksListHeaderState.GetColumnName(column));

            if (column == LocksListColumn.ItemPath)
            {
                DrawTreeViewItem.ForItemCell(
                    rect,
                    rowHeight,
                    -1,
                    GetIcon(wkPath, lockInfo),
                    null,
                    null,
                    columnText,
                    isSelected,
                    isFocused,
                    false,
                    false,
                    DrawTreeViewItem.TextTrimming.Path);

                return;
            }

            if (column == LocksListColumn.Owner)
            {
                DrawTreeViewItem.ForItemCell(
                    rect,
                    rowHeight,
                    -1,
                    GetAvatar.ForEmail(columnText, avatarLoadedAction),
                    null,
                    null,
                    columnText,
                    isSelected,
                    isFocused,
                    false,
                    false);

                return;
            }

            if (column == LocksListColumn.LockType)
            {
                DrawTreeViewItem.ForItemCell(
                    rect,
                    rowHeight,
                    -1,
                    lockInfo.Status == LockInfo.LockStatus.Locked ?
                        Images.GetLockIcon() : Images.GetLockRetainedIcon(),
                    null,
                    null,
                    columnText,
                    isSelected,
                    isFocused,
                    false,
                    false);
                return;
            }

            if (column == LocksListColumn.ModificationDate)
            {
                DrawTreeViewItem.ForLabel(
                    rect,
                    GetModificationDateContent(lockInfo.UtcTicks),
                    isSelected,
                    isFocused,
                    false);
                return;
            }

            DrawTreeViewItem.TextTrimming textTrimming = column == LocksListColumn.Branch ?
                DrawTreeViewItem.TextTrimming.Path : DrawTreeViewItem.TextTrimming.None;

            DrawTreeViewItem.ForLabel(
                rect,
                columnText,
                isSelected,
                isFocused,
                false,
                textTrimming);
        }

        static Texture GetIcon(
            string wkPath,
            LockInfo lockInfo)
        {
            string fullPath = WorkspacePath.GetWorkspacePathFromCmPath(
                wkPath,
                lockInfo.CmPath,
                Path.DirectorySeparatorChar);

            return Images.GetFileIcon(fullPath);
        }

        static GUIContent GetModificationDateContent(long utcTicks)
        {
            if (utcTicks <= 0)
                return new GUIContent(string.Empty);

            string dateFormat = ClientConfig.Get().GetClientConfigData().FindOutputDateFormat;
            DateTime localDateTime = new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();

            return new GUIContent(
                Time.GetLongTimeAgoString(localDateTime),
                string.Format(string.Format("{{0:{0}}}", dateFormat), localDateTime));
        }
    }
}
