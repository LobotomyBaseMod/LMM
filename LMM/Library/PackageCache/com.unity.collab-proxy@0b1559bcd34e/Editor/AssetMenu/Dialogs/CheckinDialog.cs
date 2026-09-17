using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.Gluon;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.AssetsOverlays;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Tree;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.AssetMenu.Dialogs
{
    internal class CheckinDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 700, 450);
            }
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.GetString(
                PlasticLocalization.Name.CheckinChanges);
        }

        internal static bool CheckinPaths(
            WorkspaceInfo wkInfo,
            List<string> paths,
            IAssetStatusCache assetStatusCache,
            bool isGluonMode,
            IWorkspaceWindow workspaceWindow,
            ViewHost viewHost,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            IPendingChangesUpdater pendingChangesUpdater,
            ISaveAssets saveAssets,
            GuiMessage.IGuiMessage guiMessage,
            IMergeViewLauncher mergeViewLauncher,
            IGluonViewSwitcher gluonViewSwitcher)
        {
            MetaCache metaCache = new MetaCache();
            metaCache.Build(paths);

            CheckinDialog dialog = Create(
                wkInfo,
                paths,
                assetStatusCache,
                metaCache,
                isGluonMode,
                workspaceWindow,
                viewHost,
                workspaceOperationsMonitor,
                pendingChangesUpdater,
                saveAssets,
                guiMessage,
                mergeViewLauncher,
                gluonViewSwitcher);

            return dialog.RunModal(focusedWindow) == ResponseType.Ok;
        }

        protected override void DoComponentsArea()
        {
            Title(PlasticLocalization.GetString(PlasticLocalization.Name.CheckinOnlyComment));

            Rect commentRect = GUILayoutUtility.GetRect(
                new GUIContent(string.Empty),
                EditorStyles.textArea,
                GUILayout.MinHeight(120),
                GUILayout.ExpandWidth(true));

            GUI.SetNextControlName(CHECKIN_TEXTAREA_NAME);

            mComment = EditorGUI.ScrollableTextAreaInternal(
                commentRect,
                mComment,
                ref mScrollPosition,
                EditorStyles.textArea);

            FocusTextAreaIfNeeded(CHECKIN_TEXTAREA_NAME);

            Title(PlasticLocalization.GetString(PlasticLocalization.Name.Files));

            DoFileList(
                mWkInfo,
                mPaths,
                mAssetStatusCache,
                mMetaCache);
        }

        void DoFileList(
            WorkspaceInfo wkInfo,
            List<string> paths,
            IAssetStatusCache assetStatusCache,
            MetaCache metaCache)
        {
            mFileListScrollPosition = GUILayout.BeginScrollView(
                mFileListScrollPosition,
                EditorStyles.helpBox,
                GUILayout.ExpandHeight(true));

            foreach (string path in paths)
            {
                if (MetaPath.IsMetaPath(path))
                    continue;

                Texture fileIcon = Directory.Exists(path) ?
                    Images.GetFolderIcon() :
                    Images.GetFileIcon(path);

                string label = WorkspacePath.GetWorkspaceRelativePath(
                    wkInfo.ClientPath, path);

                if (metaCache.HasMeta(path))
                    label = string.Concat(label, UnityConstants.TREEVIEW_META_LABEL);

                AssetsOverlays.AssetStatus assetStatus =
                    assetStatusCache.GetStatus(path);

                Rect selectionRect = EditorGUILayout.GetControlRect(
                    true,
                    UnityConstants.TREEVIEW_ROW_HEIGHT);

                DoListViewItem(selectionRect, fileIcon, label, assetStatus);
            }

            GUILayout.EndScrollView();
        }

        void DoListViewItem(
            Rect itemRect,
            Texture fileIcon,
            string label,
            AssetsOverlays.AssetStatus statusToDraw)
        {
            int iconPadding = 2;

            Texture overlayIcon = DrawAssetOverlayIcon.GetOverlayIcon(statusToDraw);

            itemRect = DrawTreeViewItem.DrawIconLeft(
                itemRect,
                itemRect.height - 2 * iconPadding,
                fileIcon,
                null,
                overlayIcon);

            GUI.Label(itemRect, label);
        }

        internal override void OkButtonAction()
        {
            if (!IsCheckinButtonEnabled())
                return;

            bool isCancelled;
            mSaveAssets.ForPathsWithConfirmation(
                mWkInfo.ClientPath, mPaths, mWorkspaceOperationsMonitor,
                out isCancelled);

            if (isCancelled)
                return;

            mIsRunningCheckin = true;

            mPaths.AddRange(mMetaCache.GetExistingMeta(mPaths));

            if (mIsGluonMode)
            {
                CheckinDialogOperations.CheckinPathsPartial(
                    mWkInfo,
                    mPaths,
                    mComment,
                    mViewHost,
                    this,
                    mGuiMessage,
                    mProgressControls,
                    mGluonViewSwitcher,
                    mPendingChangesUpdater);
                return;
            }

            CheckinDialogOperations.CheckinPaths(
                mWkInfo,
                mPaths,
                mComment,
                mWorkspaceWindow,
                this,
                mGuiMessage,
                mProgressControls,
                mMergeViewLauncher,
                mPendingChangesUpdater);
        }

        protected override void DoOkButton()
        {
            GUI.enabled = IsCheckinButtonEnabled();

            try
            {
                if (!NormalButton(PlasticLocalization.GetString(
                        PlasticLocalization.Name.CheckinButton)))
                    return;
            }
            finally
            {
                if (!mSentCheckinTrackEvent)
                {
                    TrackFeatureUseEvent.For(
                        PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                        TrackFeatureUseEvent.Features.UnityPackage.ContextMenuCheckinDialogCheckin);

                    mSentCheckinTrackEvent = true;
                }

                GUI.enabled = true;
            }

            OkButtonAction();
        }

        internal override void CancelButtonAction()
        {
            if (!mSentCancelTrackEvent)
            {
                TrackFeatureUseEvent.For(
                    PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                    TrackFeatureUseEvent.Features.UnityPackage.ContextMenuCheckinDialogCancel);

                mSentCancelTrackEvent = true;
            }

            base.CancelButtonAction();
        }

        bool IsCheckinButtonEnabled()
        {
            return !string.IsNullOrEmpty(mComment) && !mIsRunningCheckin;
        }

        static CheckinDialog Create(
            WorkspaceInfo wkInfo,
            List<string> paths,
            IAssetStatusCache assetStatusCache,
            MetaCache metaCache,
            bool isGluonMode,
            IWorkspaceWindow workspaceWindow,
            ViewHost viewHost,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            IPendingChangesUpdater pendingChangesUpdater,
            ISaveAssets saveAssets,
            GuiMessage.IGuiMessage guiMessage,
            IMergeViewLauncher mergeViewLauncher,
            IGluonViewSwitcher gluonViewSwitcher)
        {
            var instance = CreateInstance<CheckinDialog>();
            instance.IsResizable = true;
            instance.minSize = new Vector2(520, 370);
            instance.mWkInfo = wkInfo;
            instance.mPaths = paths;
            instance.mAssetStatusCache = assetStatusCache;
            instance.mMetaCache = metaCache;
            instance.mIsGluonMode = isGluonMode;
            instance.mWorkspaceWindow = workspaceWindow;
            instance.mViewHost = viewHost;
            instance.mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            instance.mPendingChangesUpdater = pendingChangesUpdater;
            instance.mSaveAssets = saveAssets;
            instance.mGuiMessage = guiMessage;
            instance.mMergeViewLauncher = mergeViewLauncher;
            instance.mGluonViewSwitcher = gluonViewSwitcher;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.AddControlConsumingEnterKey(CHECKIN_TEXTAREA_NAME);
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        WorkspaceInfo mWkInfo;
        List<string> mPaths;
        IAssetStatusCache mAssetStatusCache;
        MetaCache mMetaCache;
        bool mIsGluonMode;
        string mComment;

        bool mIsRunningCheckin;
        Vector2 mFileListScrollPosition;

        // IMGUI evaluates every frame, need to make sure feature tracks get sent only once
        bool mSentCheckinTrackEvent = false;
        bool mSentCancelTrackEvent = false;

        IWorkspaceWindow mWorkspaceWindow;
        WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        IPendingChangesUpdater mPendingChangesUpdater;
        ISaveAssets mSaveAssets;
        ViewHost mViewHost;
        IMergeViewLauncher mMergeViewLauncher;
        IGluonViewSwitcher mGluonViewSwitcher;
        GuiMessage.IGuiMessage mGuiMessage;

        const string CHECKIN_TEXTAREA_NAME = "checkin_textarea";

        Vector2 mScrollPosition;

        class MetaCache
        {
            internal bool HasMeta(string path)
            {
                return mCache.Contains(MetaPath.GetMetaPath(path));
            }

            internal List<string> GetExistingMeta(List<string> paths)
            {
                List<string> result = new List<string>();

                foreach (string path in paths)
                {
                    string metaPath = MetaPath.GetMetaPath(path);

                    if (!mCache.Contains(metaPath))
                        continue;

                    result.Add(metaPath);
                }

                return result;
            }

            internal void Build(List<string> paths)
            {
                HashSet<string> indexedKeys = BuildIndexedKeys(paths);

                for (int i = paths.Count - 1; i >= 0; i--)
                {
                    string currentPath = paths[i];

                    if (!MetaPath.IsMetaPath(currentPath))
                        continue;

                    string realPath = MetaPath.GetPathFromMetaPath(currentPath);

                    if (!indexedKeys.Contains(realPath))
                        continue;

                    // found foo.c and foo.c.meta
                    // with the same chage types - move .meta to cache
                    mCache.Add(currentPath);
                    paths.RemoveAt(i);
                }
            }

            static HashSet<string> BuildIndexedKeys(List<string> paths)
            {
                HashSet<string> result = new HashSet<string>();

                foreach (string path in paths)
                {
                    if (MetaPath.IsMetaPath(path))
                        continue;

                    result.Add(path);
                }

                return result;
            }

            HashSet<string> mCache = new HashSet<string>();
        }
    }
}
