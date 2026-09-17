using System.Collections.Generic;

using Codice;
using Codice.CM.Common;
using Codice.Client.BaseCommands;
using Codice.Client.Commands.WkTree;
using PlasticGui.WorkspaceWindow;

namespace Unity.PlasticSCM.Editor.AssetsOverlays.Cache
{
    internal class LocalStatusCache
    {
        internal LocalStatusCache(WorkspaceInfo wkInfo)
        {
            mWkInfo = wkInfo;
        }

        internal AssetStatus GetStatus(string fullPath)
        {
            AssetStatus result;

            if (mStatusByPathCache.TryGetValue(fullPath, out result))
                return result;

            result = CalculateStatus(
                mWkInfo,
                fullPath,
                FilterManager.Get().GetIgnoredFilter(),
                FilterManager.Get().GetHiddenChangesFilter());

            mStatusByPathCache.Add(fullPath, result);

            return result;
        }

        internal void Clear()
        {
            mStatusByPathCache.Clear();
        }

        static AssetStatus CalculateStatus(
            WorkspaceInfo wkInfo,
            string fullPath,
            IgnoredFilesFilter ignoredFilter,
            HiddenChangesFilesFilter hiddenChangesFilter)
        {
            WorkspaceTreeNode node = PlasticGui.Plastic.API.GetWorkspaceTreeNode(
                wkInfo, fullPath);

            if (CheckWorkspaceTreeNodeStatus.IsPrivate(node))
            {
                return ignoredFilter.IsIgnored(fullPath) ?
                    AssetStatus.Ignored : AssetStatus.Private;
            }

            if (CheckWorkspaceTreeNodeStatus.IsAdded(node))
                return AssetStatus.Added;

            AssetStatus status = AssetStatus.Controlled;

            status |= CalculateControlledFlags(wkInfo, fullPath, node);
            status |= GetHiddenChangeFlag(fullPath, hiddenChangesFilter);

            return status;
        }

        static AssetStatus CalculateControlledFlags(
            WorkspaceInfo wkInfo,
            string fullPath,
            WorkspaceTreeNode node)
        {
            if (CheckWorkspaceTreeNodeStatus.IsCheckedOut(node))
                return AssetStatus.Checkout;

            if (CheckWorkspaceTreeNodeStatus.IsDirectory(node))
                return PlasticGui.Plastic.API.IsOnChangedTree(wkInfo, fullPath)
                    ? AssetStatus.ContainsChanges
                    : AssetStatus.None;

            return ChangedFileChecker.IsChanged(node.LocalInfo, fullPath, wkInfo.IsDynamic, false)
                ? AssetStatus.Changed
                : AssetStatus.None;
        }

        static AssetStatus GetHiddenChangeFlag(
            string fullPath,
            HiddenChangesFilesFilter hiddenChangesFilter)
        {
            return hiddenChangesFilter.IsHiddenChanged(fullPath)
                ? AssetStatus.HiddenChanged
                : AssetStatus.None;
        }

        readonly WorkspaceInfo mWkInfo;
        readonly Dictionary<string, AssetStatus> mStatusByPathCache =
            BuildPathDictionary.ForPlatform<AssetStatus>();
    }
}
