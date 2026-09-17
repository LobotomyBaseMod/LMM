using UnityEngine;
using UnityEngine.UIElements;

using Codice.CM.Common;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Connections;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class ParentLinkShape : BrExShape
    {
        internal Rect Source { get; set; }

        internal Rect Destination { get; set; }

        internal bool IsHighlightedByMergeLinkInterval { get; set; }

        internal MergeType MergeLinkInvervalType { get; set; }

        internal ParentLinkShape(
            VirtualShape virtualShape,
            bool isRelevant) : base(virtualShape)
        {
            mIsRelevant = isRelevant;
        }

        internal override void OnScrollChanged()
        {
            float opacity = IsPartiallyVisible() ? 0.7f : 1;
            style.opacity = opacity;
        }

        protected override void SetVirtualShape(VirtualShape virtualShape)
        {
            base.SetVirtualShape(virtualShape);

            if (!(virtualShape is ChangesetParentLinkVirtualShape))
                return;

            IsHighlightedByMergeLinkInterval = ((ChangesetParentLinkVirtualShape)virtualShape).IsHighlightedByMergeLinkInterval;
            MergeLinkInvervalType = ((ChangesetParentLinkVirtualShape)virtualShape).MergeLinkInvervalType;
        }

        protected override void GenerateVisualContent(Painter2D painter)
        {
            if (IsCrossBranchParentLink() && !DisplayOptions.DisplayCrossBranchChangesetLinks)
                return;

            ConnectionPoints connection = ParentLinkConnectionPoints.
                Build(Source, Destination);

            if (IsHighlightedByMergeLinkInterval)
            {
                DrawMergeIntervalHighlight(painter, connection);
            }

            // arrow line
            painter.strokeColor = BrExColors.ParentLink.GetLineColor();
            painter.lineWidth = BrExLineWidths.Changeset.GetParentLinkThickness();

            DrawLineArrow(
                painter,
                connection.Source,
                connection.Destination,
                out var arrowStartPoint);

            painter.Stroke();

            painter.strokeColor = BrExColors.ParentLink.GetLineColor();
            painter.fillColor = BrExColors.ParentLink.GetLineColor();
            painter.lineWidth = BrExLineWidths.Changeset.GetParentLinkThickness();
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            // arrow head
            DrawHeadArrow(
                painter,
                arrowStartPoint,
                connection.Destination);

            painter.Stroke();
            painter.Fill();
        }

        void DrawHeadArrow(
            Painter2D painter,
            Vector2 arrowStartPoint,
            Vector2 arrowEndPoint)
        {
            float theta = Mathf.Atan2(
                arrowStartPoint.y - arrowEndPoint.y,
                arrowStartPoint.x - arrowEndPoint.x);

            float sint = Mathf.Sin(theta);
            float cost = Mathf.Cos(theta);

            Vector2 p3 = new Vector2(
                arrowEndPoint.x + (ARROW_WIDTH * cost - ARROW_HEIGHT * sint),
                arrowEndPoint.y + (ARROW_WIDTH * sint + ARROW_HEIGHT * cost));

            Vector2 p4 = new Vector2(
                arrowEndPoint.x + (ARROW_WIDTH * cost + ARROW_HEIGHT * sint),
                arrowEndPoint.y - (ARROW_HEIGHT * cost - ARROW_WIDTH * sint));

            painter.BeginPath();
            painter.MoveTo(p3);
            painter.LineTo(p4);
            painter.LineTo(arrowEndPoint);
            painter.ClosePath();
        }

        void DrawLineArrow(
            Painter2D painter,
            Vector2 start,
            Vector2 end,
            out Vector2 arrowStartPoint)
        {
            Vector2 normal = (end - start).normalized;

            float distance = ARROW_HEIGHT + 3; // give some extra for the pen thickness

            Vector2 endLinePoint = new Vector2(
                end.x - (distance * normal.x),
                end.y - (distance * normal.y));

            arrowStartPoint = start;

            painter.StrokeLine(
                start,
                endLinePoint,
                BrExDashes.GetParentLinkDashPattern(mIsRelevant));
        }

        void DrawMergeIntervalHighlight(
            Painter2D painter,
            ConnectionPoints connection)
        {
            // arrow line
            painter.strokeColor = BrExColors.MergeLink.GetLineColor(MergeLinkInvervalType, false);
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.lineWidth = BrExLineWidths.MergeLink.GetIntervalHighlightWidth();

            DrawLineArrow(
                painter,
                connection.Source,
                connection.Destination,
                out _);

            painter.Stroke();
        }

        bool IsPartiallyVisible()
        {
            VirtualCanvas virtualCanvas = this.GetFirstAncestorOfType<VirtualCanvas>();

            return !IsVisibleRect(Source, virtualCanvas) || !IsVisibleRect(Destination, virtualCanvas);
        }

        bool IsCrossBranchParentLink()
        {
            return !Mathf.Approximately(Source.y, Destination.y);
        }

        static bool IsVisibleRect(Rect source, VirtualCanvas virtualCanvas)
        {
            if (virtualCanvas == null)
                return false;

            Rect visibleRect = virtualCanvas.GetVisibleRectStrict();

            Vector2 topLeft = new Vector2(source.xMin, source.yMin);
            Vector2 bottomRight = new Vector2(source.xMax, source.yMax);
            Vector2 topRight = new Vector2(source.xMax, source.yMax);
            Vector2 bottomLeft = new Vector2(source.xMin, source.yMax);

            return visibleRect.Contains(topLeft) || visibleRect.Contains(bottomRight) ||
                   visibleRect.Contains(topRight) || visibleRect.Contains(bottomLeft);
        }

        readonly bool mIsRelevant;

        const float ARROW_WIDTH = 4;
        const float ARROW_HEIGHT = 2;
    }
}
