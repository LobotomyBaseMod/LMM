using UnityEngine;

using Codice.Client.BaseCommands.BranchExplorer;
using UnityEngine.UIElements;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization
{
    internal class VirtualShape : IVirtualChild, IVirtualShape
    {
        internal VirtualShape(
            ObjectDrawInfo drawInfo,
            Rect bounds,
            ShapeType shapeType,
            WorkspaceUIConfiguration config,
            ColorProvider colorProvider = null,
            AsyncTaskLoader taskLoader = null,
            AsyncUserNameResolver userNameResolver = null,
            AsyncChangesetCommentResolver commentResolver = null)
        {
            mDrawInfo = drawInfo;
            mShapeType = shapeType;
            Bounds = bounds;
            mConfig = config;
            mTaskLoader = taskLoader;
            mUserNameResolver = userNameResolver;
            mColorProvider = colorProvider;
            mCommentResolver = commentResolver;
        }

        internal Rect Bounds
        {
            get { return mBounds; }
            set { mBounds = value; }
        }

        internal VisualElement Visual
        {
            get { return mVisual; }
            set { mVisual = (BrExShape)value; }
        }

        internal Rect BoundsForNavigation
        {
            get
            {
                return (ShapeType != ShapeType.Branch) ? Bounds :
                    new Rect(
                        Bounds.x + Bounds.width - BrExDrawProperties.ChangesetWidth,
                        Bounds.y, BrExDrawProperties.ChangesetWidth, Bounds.height);
            }
        }

        public bool IsSelected
        {
            get { return mIsSelected; }
            set
            {
                mIsSelected = value;

                if (mVisual == null)
                    return;

                mVisual.IsSelected = value;
            }
        }

        public bool IsSearchResult
        {
            get { return mIsSearchResult; }
            set
            {
                mIsSearchResult = value;

                if (mVisual == null)
                    return;

                mVisual.IsSearchResult = value;
            }
        }

        public bool IsCurrentSearchResult
        {
            get { return mIsCurrentSearchResult; }
            set
            {
                mIsCurrentSearchResult = value;

                if (mVisual == null)
                    return;

                mVisual.IsCurrentSearchResult = value;
            }
        }

        internal bool IsLinkNavigationTarget
        {
            get { return mIsLinkNavigationTarget; }
            set
            {
                mIsLinkNavigationTarget = value;

                if (mVisual == null)
                    return;

                mVisual.IsLinkNavigationTarget = value;
            }
        }

        public ShapeType ShapeType
        {
            get { return mShapeType; }
        }

        public ObjectDrawInfo DrawInfo
        {
            get { return mDrawInfo; }
        }

        internal WorkspaceUIConfiguration Config
        {
            get { return mConfig; }
        }

        Rect IVirtualChild.Bounds
        {
            get { return Bounds; }
        }

        VisualElement IVirtualChild.Visual
        {
            get { return Visual; }
        }

        VisualElement IVirtualChild.CreateVisual()
        {
            if (mVisual != null)
                return mVisual;

            mVisual = VisualShapeFactory.CreateVisual(
                this,
                mConfig,
                mColorProvider,
                mTaskLoader,
                mUserNameResolver,
                mCommentResolver);

            return mVisual;
        }

        void IVirtualChild.DisposeVisual()
        {
            if (mVisual != null)
                mVisual.Dispose();

            mVisual = null;
        }

        void IVirtualShape.InvalidateVisual()
        {
            if (mVisual == null)
                return;

            mVisual.Redraw();
        }

        protected BrExShape mVisual;

        bool mIsSelected;
        bool mIsSearchResult;
        bool mIsCurrentSearchResult;
        bool mIsLinkNavigationTarget;

        Rect mBounds;

        readonly ObjectDrawInfo mDrawInfo;
        readonly ShapeType mShapeType;
        readonly WorkspaceUIConfiguration mConfig;
        readonly AsyncTaskLoader mTaskLoader;
        readonly AsyncUserNameResolver mUserNameResolver;
        readonly AsyncChangesetCommentResolver mCommentResolver;
        readonly ColorProvider mColorProvider;
    }
}
