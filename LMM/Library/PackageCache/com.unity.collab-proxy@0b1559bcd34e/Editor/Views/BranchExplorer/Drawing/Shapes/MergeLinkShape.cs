using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.CM.Common;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class MergeLinkShape : BrExShape
    {
        internal LinkDrawInfo LinkDraw { get { return VirtualShape.DrawInfo as LinkDrawInfo; } }

        internal Rect Source { get; }

        internal Rect Destination { get; }

        internal MergeLinkShape(
            VirtualShape virtualShape,
            Rect source,
            Rect dst) : base(virtualShape)
        {
            Source = source;
            Destination = dst;

            SetupBounds();
        }

        internal override bool HasContextMenu() => true;

        internal override void OnScrollChanged()
        {
            if (!LinkDraw.Pending)
                return;

            MarkDirtyRepaint();
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (!DisplayOptions.DisplayMergeLinks)
                return false;

            return mHitTestPath != null &&
                   mHitTestPath.IsPointNear(localPoint, HIT_TEST_TOLERANCE);
        }

        void SetupBounds()
        {
            MergeLinkGeometry.CalculateHitTestPath(Source, Destination, out mHitTestPath);

            Rect boundingBox = mHitTestPath.CalculateBoundingBox()
                .Inflate(HIT_TEST_TOLERANCE);

            style.left = boundingBox.x;
            style.top = boundingBox.y;
            style.width = boundingBox.width;
            style.height = boundingBox.height;

            mBoundsOffset = new Vector2(boundingBox.x, boundingBox.y);

            TranslateHitTestPath(mHitTestPath, mBoundsOffset);

            mLocalSource = TranslateRect(Source, mBoundsOffset);
            mLocalDestination = TranslateRect(Destination, mBoundsOffset);
        }

        protected override void GenerateVisualContent(Painter2D painter)
        {
            if (!DisplayOptions.DisplayMergeLinks)
                return;

            painter.strokeColor = BrExColors.MergeLink.GetLineColor(
                LinkDraw.MergeType,
                IsSelected);
            painter.lineWidth = BrExLineWidths.MergeLink.GetWidth();
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            if (LinkDraw.Pending && TryGetLocalVisibleRect(out Rect localVisibleRect))
            {
                // use clipping when drawing pending links
                // as they use dashes and could exceed the
                // UIElements limit of 65535 vertices
                MergeLinkGeometry.Draw(
                    painter,
                    mLocalSource,
                    mLocalDestination,
                    BrExDashes.GetMergeLinkDashPattern(true),
                    localVisibleRect);
                return;
            }

            MergeLinkGeometry.Draw(
                painter,
                mLocalSource,
                mLocalDestination,
                null);
        }

        static void TranslateHitTestPath(List<Vector2> hitTestPath, Vector2 offset)
        {
            for (int i = 0; i < hitTestPath.Count; i++)
                hitTestPath[i] -= offset;
        }

        Rect TranslateRect(Rect rect, Vector2 offset)
        {
            return new Rect(
                rect.x - offset.x,
                rect.y - offset.y,
                rect.width,
                rect.height);
        }

        bool TryGetLocalVisibleRect(out Rect localVisibleRect)
        {
            VirtualCanvas virtualCanvas = this.GetFirstAncestorOfType<VirtualCanvas>();

            if (virtualCanvas == null)
            {
                localVisibleRect = default;
                return false;
            }

            Rect visibleRect = virtualCanvas.GetVisibleRectStrict();

            localVisibleRect = new Rect(
                visibleRect.x - mBoundsOffset.x,
                visibleRect.y - mBoundsOffset.y,
                visibleRect.width,
                visibleRect.height);

            return true;
        }

        const float HIT_TEST_TOLERANCE = 6f;

        Rect mLocalSource;
        Rect mLocalDestination;
        List<Vector2> mHitTestPath;
        Vector2 mBoundsOffset;
    }
}

