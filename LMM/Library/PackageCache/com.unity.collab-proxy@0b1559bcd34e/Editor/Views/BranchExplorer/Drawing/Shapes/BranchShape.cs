using System.Collections.Generic;
using Codice.Client.BaseCommands.BranchExplorer;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class BranchShape : BrExShape
    {
        // internal for testing purposes
        internal bool IsHovered { get { return mIsHovered; } }

        internal BranchDrawInfo BranchDrawInfo { get { return VirtualShape.DrawInfo as BranchDrawInfo; } }

        internal BranchShape(VirtualShape virtualShape, ColorProvider colorProvider) : base(virtualShape)
        {
            mColorProvider = colorProvider;

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        internal override void Dispose()
        {
            base.Dispose();

            UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        internal override bool HasContextMenu() => true;

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (!DisplayOptions.DisplayBranches)
                return false;

            return mHitTestPolygon.ContainsPoint(localPoint);
        }

        protected override void GenerateVisualContent(Painter2D painter)
        {
            if (!DisplayOptions.DisplayBranches)
                return;

            Rect geometryRect = new Rect(
                0, 0,
                BranchDrawInfo.Bounds.Width,
                BranchDrawInfo.Bounds.Height);

            SubBranchContainerDrawInfo[] containers = TranslateSubBranchContainers(
                BranchDrawInfo.SubBranchContainers,
                BranchDrawInfo.Bounds);

            Color branchColor = mColorProvider.GetBranchColor(this, IsMultiBranchSelected());
            Color borderColor = BrExColors.Branch.GetBorderColor(
                IsSelected, IsMultiBranchSelected(), IsCurrentSearchResult);

            if (mIsHovered)
            {
                branchColor = BrightenColor(branchColor, HoverBrightenAmount);
                borderColor = BrightenColor(borderColor, HoverBrightenAmount);
            }

            painter.fillColor = branchColor;
            painter.strokeColor = borderColor;
            painter.lineWidth = mIsHovered
                ? BrExLineWidths.Branch.GetBorderWidth()
                : BrExLineWidths.Branch.GetBorderWidth();

            BranchGeometry.Draw(
                painter,
                geometryRect,
                containers,
                out mHitTestPolygon);

            painter.Stroke();
            painter.Fill();

            if (BranchDrawInfo.IsWorkspaceBranch && BranchDrawInfo.IsEmpty())
            {
                DrawHomeGlyph(painter, geometryRect);
            }
        }

        static void DrawHomeGlyph(Painter2D painter, Rect branchBounds)
        {
            float scale = 0.825f;
            float glyphWidth = HomeGeometry.WIDTH * scale;
            float glyphHeight = HomeGeometry.HEIGHT * scale;

            float offsetX = (branchBounds.width / 2) - (glyphWidth / 2);
            float offsetY = (branchBounds.height / 2) - (glyphHeight / 2);

            painter.fillColor = BrExColors.Branch.GetHomeGlyphFillColor();
            HomeGeometry.Draw(painter, offsetX, offsetY, scale);
            painter.Fill();
        }

        static SubBranchContainerDrawInfo[] TranslateSubBranchContainers(
            SubBranchContainerDrawInfo[] subBranchContainers,
            BrExRectangle relativeTo)
        {
            if (subBranchContainers == null || subBranchContainers.Length == 0)
                return subBranchContainers;

            List<SubBranchContainerDrawInfo> translatedSubBranchContainers = new List<SubBranchContainerDrawInfo>();
            foreach (var subBranchContainer in subBranchContainers)
            {
                translatedSubBranchContainers.Add(new SubBranchContainerDrawInfo()
                {
                    Bounds = new BrExRectangle(
                        subBranchContainer.Bounds.X - relativeTo.X,
                        subBranchContainer.Bounds.Y - relativeTo.Y,
                        subBranchContainer.Bounds.Width,
                        subBranchContainer.Bounds.Height),
                    CaptionBounds = subBranchContainer.CaptionBounds,
                    Depth = subBranchContainer.Depth,
                    IniChangeset = subBranchContainer.IniChangeset,
                    EndChangeset = subBranchContainer.EndChangeset,
                    Tag = subBranchContainer.Tag,
                    Visual = subBranchContainer.Visual,
                    Hidden = subBranchContainer.Hidden
                });
            }

            return translatedSubBranchContainers.ToArray();
        }

        bool IsMultiBranchSelected()
        {
            BranchExplorerViewer brExView = this.GetFirstAncestorOfType<BranchExplorerViewer>();
            if (brExView == null)
                return false;

            return brExView.Selection.GetSelectedBranches().Count > 1;
        }

        // internal for testing purposes
        internal void OnPointerEnter(PointerEnterEvent evt)
        {
            if (evt.pressedButtons != 0)
                return;

            mIsHovered = true;
            MarkDirtyRepaint();
        }

        // internal for testing purposes
        internal void OnPointerLeave(PointerLeaveEvent evt)
        {
            mIsHovered = false;
            MarkDirtyRepaint();
        }

        static Color BrightenColor(Color color, float amount)
        {
            return new Color(
                Mathf.Min(1f, color.r + amount),
                Mathf.Min(1f, color.g + amount),
                Mathf.Min(1f, color.b + amount),
                Mathf.Clamp01(color.a + amount * 0.5f));
        }

        bool mIsHovered;
        List<Vector2> mHitTestPolygon;
        readonly ColorProvider mColorProvider;

        const float HoverBrightenAmount = 0.08f;
    }
}
