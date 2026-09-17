using System;
using Codice.Client.BaseCommands;
using Codice.Client.BaseCommands.Differences;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Client.Differences;
using Codice.CM.Client.Differences.Graphic;
using Codice.CM.Common;
using Codice.CM.Common.Merge;
using Codice.CM.Common.Mount;
using PlasticGui.Diff;
using Plugins.PlasticSCM.Editor.Diff.Asset;
using Plugins.PlasticSCM.Editor.Diff.Purged;
using Plugins.PlasticSCM.Editor.Diff.Text;
using Plugins.PlasticSCM.Editor.Diff.Texture;
using Unity.PlasticSCM.Editor;
using UnityEngine.UIElements;
using XDiffGui;
using EditorWindow = UnityEditor.EditorWindow;

namespace Plugins.PlasticSCM.Editor.Diff
{
    internal class DiffWindow : EditorWindow
    {
        internal void ShowDiffFromChange(ChangeInfo changeInfo, ChangeInfo changedForMoved)
        {
            ShowDiffsFromAsyncData(() => mDiffViewerDataProvider.GetDiffViewerInfo(
                changeInfo, changedForMoved));
        }

        internal void ShowDiffFromHistory(
            HistoryRevision leftRevision,
            HistoryRevision rightRevision,
            RepositorySpec repSpec,
            string cmPath,
            long itemId)
        {
            ShowDiffsFromAsyncData(() => mDiffViewerDataProvider.GetDiffViewerInfo(
                leftRevision, rightRevision, repSpec, cmPath, itemId));
        }

        internal void ShowDiffFromDiff(MountPoint mount, Difference diff)
        {
            ShowDiffsFromAsyncData(() => mDiffViewerDataProvider.GetDiffViewerInfo(
                mount, diff));
        }

        void OnDisable()
        {
            mDiffViewerDataProvider?.Clear();
            mDiffViewManager?.Clear();
        }

        void OnEnable()
        {
            UVCSPlugin uvcsPlugin = UVCSPlugin.Instance;

            //titleContent.image = Images.GetDiffIcon();

            uvcsPlugin.Enable();

            if (!uvcsPlugin.ConnectionMonitor.IsConnected)
                return;

            mWkInfo = FindWorkspace.InfoForApplicationPath(
                ApplicationDataPath.Get(), PlasticGui.Plastic.API);

            if (mWkInfo == null)
                return;

            Init();
        }

        void Init()
        {
            mBranchResolver = new BranchResolver();

            mDiffViewManager = new DiffViewManager(
                mWkInfo,
                mBranchResolver,
                new BigFileChecker(),
                null);

            mDiffViewerDataProvider = PlasticGui.Plastic.API.GetDiffViewerDataProvider(
                mWkInfo,
                mDiffViewManager,
                mBranchResolver,
                null);

            mBigFileDownloader = mDiffViewManager.BigFileDownloader;
        }

        void ShowDiffsFromAsyncData(Func<DiffViewerData> getDiffViewerData)
        {
            int requestId = System.Threading.Interlocked.Increment(
                ref mCurrentDiffRequestId);

            DiffViewerData data = null;
            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    if (requestId != mCurrentDiffRequestId)
                        return;

                    data = getDiffViewerData();
                },
                /*afterOperationDelegate*/ delegate
                {
                    if (requestId != mCurrentDiffRequestId)
                        return;

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    ShowDiffs(data);
                });
        }

        void ShowDiffs(DiffViewerData data)
        {
            if (data == null)
                return;

            if (data.Left?.IsPurged == true ||
                data.Right?.IsPurged == true)
            {
                ShowPurgedDiff(data);
                return;
            }

            if (data.Left == null)
            {
                ShowMessage(data.Message);
                return;
            }

            if (data.Right == null)
            {
                ShowContent(data);
                return;
            }

            ShowDiff(data);
        }

        void ShowContent(DiffViewerData data)
        {
            if (data.IsImage())
            {
                ShowImageContentControl(data);
                return;
            }

            if (data.IsSerializedAsset())
            {
                ShowAssetContentControl(data);
            }

            ShowContentControl(data.Left, data.Message, data.PathForEdition);
        }

        void ShowDiff(DiffViewerData data)
        {
            if (data.IsImage())
            {
                ShowImageDiffControl(data);
                return;
            }

            if (data.IsSerializedAsset())
            {
                ShowAssetDiffControl(data);
            }

            ShowDiffControl(data);
        }

        void ShowContentControl(
            EntryData entryData, string message, string pathForEdition)
        {
            if (mContentControl == null)
                mContentControl = BuildContentControl();

            mContentControl.ShowData(
                entryData, message, pathForEdition);

            ReplaceView(mContentControl);
        }

        void ShowDiffControl(DiffViewerData data)
        {
            if (mDiffControl == null)
                mDiffControl = BuildDiffControl();

            mDiffControl.ShowData(data);

            ReplaceView(mDiffControl);
        }

        void ShowAssetContentControl(DiffViewerData data)
        {
            if (mAssetContentControl == null)
                mAssetContentControl = BuildAssetContentControl();

            mAssetContentControl.ShowData(data);

            ReplaceView(mAssetContentControl);
        }

        void ShowAssetDiffControl(DiffViewerData data)
        {
            if (mAssetDiffControl == null)
                mAssetDiffControl = BuildAssetDiffControl();

            mAssetDiffControl.ShowData(data);

            ReplaceView(mAssetDiffControl);
        }

        void ShowImageContentControl(DiffViewerData data)
        {
            if (mImageContentControl == null)
                mImageContentControl = BuildImageContentControl();

            mImageContentControl.ShowData(data);

            ReplaceView(mImageContentControl);
        }

        void ShowImageDiffControl(DiffViewerData data)
        {
            if (mImageDiffControl == null)
                mImageDiffControl = BuildImageDiffControl();

            mImageDiffControl.ShowData(data);

            ReplaceView(mImageDiffControl);
        }

        void ShowPurgedDiff(DiffViewerData data)
        {
            if (mPurgedRevisionControl == null)
                mPurgedRevisionControl = BuildPurgedRevisionControl();

            mPurgedRevisionControl.ShowData(data);

            ReplaceView(mPurgedRevisionControl);
        }

        void ShowMessage(string message)
        {
            if (mDiffMessagePanel == null)
                mDiffMessagePanel = BuildDiffMessagePanel();

            mDiffMessagePanel.ShowMessage(message);

            ReplaceView(mDiffMessagePanel);
        }

        ContentControl BuildContentControl()
        {
            return new ContentControl();
        }

        DiffControl BuildDiffControl()
        {
            return new DiffControl();
        }

        AssetContentControl BuildAssetContentControl()
        {
            return new AssetContentControl();
        }

        AssetDiffControl BuildAssetDiffControl()
        {
            return new AssetDiffControl();
        }

        ImageContentControl BuildImageContentControl()
        {
            return new ImageContentControl();
        }

        ImageDiffControl BuildImageDiffControl()
        {
            return new ImageDiffControl();
        }

        PurgedRevisionControl BuildPurgedRevisionControl()
        {
            return new PurgedRevisionControl();
        }

        DiffMessagePanel BuildDiffMessagePanel()
        {
            return new DiffMessagePanel();
        }

        void ReplaceView(VisualElement view)
        {
            if (mCurrentView != null && mCurrentView.Equals(view))
                return;

            if (rootVisualElement.Contains(mCurrentView))
                rootVisualElement.Remove(mCurrentView);

            rootVisualElement.Add(view);
            mCurrentView = view;
        }

        WorkspaceInfo mWkInfo;

        VisualElement mCurrentView;
        //NoItemsSelectedPanel mNoItemSelectedPanel;
        ContentControl mContentControl;
        DiffControl mDiffControl;

        ImageContentControl mImageContentControl;
        ImageDiffControl mImageDiffControl;

        AssetContentControl mAssetContentControl;
        AssetDiffControl mAssetDiffControl;

        PurgedRevisionControl mPurgedRevisionControl;
        DiffMessagePanel mDiffMessagePanel;
        BranchResolver mBranchResolver;
        //IRunAfterSetDifferencesInfo mRunAfterSetDifferencesInfo;

        DiffViewManager mDiffViewManager;
        //readonly IContainer mParentContainer;
        int mCurrentDiffRequestId;
        IBigFileDownloader mBigFileDownloader;
        IDiffViewerDataProvider mDiffViewerDataProvider;
    }
}
