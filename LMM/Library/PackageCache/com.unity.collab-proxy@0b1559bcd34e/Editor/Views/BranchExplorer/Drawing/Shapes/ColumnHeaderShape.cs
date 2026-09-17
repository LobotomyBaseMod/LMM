using Codice.Client.BaseCommands.BranchExplorer;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class ColumnHeaderShape : BrExShape
    {
        internal ColumnDrawInfo ColumnDraw { get { return VirtualShape.DrawInfo as ColumnDrawInfo; } }

        internal ColumnHeaderShape(VirtualShape virtualShape) : base(virtualShape)
        {
            mHeaderBackground = new VisualElement();
            mHeaderBackground.style.position = Position.Absolute;
            mHeaderBackground.style.width = ColumnDraw.Bounds.Width;
            mHeaderBackground.style.height = BrExDrawProperties.DateHeaderHeight;
            mHeaderBackground.style.backgroundColor =
                UnityStyles.Colors.BranchExplorer.ColumnHeaderBackgroundColor;

            mCaptionLabel = new Label();
            mCaptionLabel.style.position = Position.Absolute;
            mCaptionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            mCaptionLabel.style.fontSize = 11;
            mCaptionLabel.style.color = UnityStyles.Colors.SecondaryLabel;
            mCaptionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            mCaptionLabel.style.marginBottom = 0;
            mCaptionLabel.style.marginTop = 0;
            mCaptionLabel.style.paddingBottom = 0;
            mCaptionLabel.style.paddingTop = 0;
            mCaptionLabel.style.paddingLeft = TextPaddingX;
            mCaptionLabel.style.paddingRight = TextPaddingX;
            mCaptionLabel.style.height = BrExDrawProperties.DateHeaderHeight;

            mCaptionLabel.text = ColumnDraw.Caption;

            Add(mHeaderBackground);
            Add(mCaptionLabel);
        }

        internal override void Redraw()
        {
            UpdateLayout();
            base.Redraw();
        }

        internal override void OnScrollChanged()
        {
            UpdateLayout();
        }

        void UpdateLayout()
        {
            VirtualCanvas canvas = this.GetFirstAncestorOfType<VirtualCanvas>();
            CanvasScrollView scrollView = this.GetFirstAncestorOfType<CanvasScrollView>();
            if (canvas == null || scrollView == null)
                return;

            float scrollY = canvas.ScrollOffset.y / canvas.ZoomLevel;

            mHeaderBackground.style.translate = new Translate(0, scrollY);

            float columnLeft = ColumnDraw.Bounds.X;
            float columnRight = columnLeft + ColumnDraw.Bounds.Width;

            float viewportLeft = canvas.ScrollOffset.x / canvas.ZoomLevel;
            float viewportRight = viewportLeft + scrollView.Viewport.contentRect.width / canvas.ZoomLevel;

            float labelWidth = mCaptionLabel.resolvedStyle.width;
            if (float.IsNaN(labelWidth) || labelWidth == 0)
                labelWidth = mCaptionLabel.text.Length * 7 + 2 * TextPaddingX;

            // Center label in visible portion of column (intersection of column and viewport)
            float visibleLeft = Mathf.Max(columnLeft, viewportLeft);
            float visibleRight = Mathf.Min(columnRight, viewportRight);
            float labelLeft = (visibleLeft + visibleRight - labelWidth) / 2;

            // Clamp to column bounds
            labelLeft = Mathf.Clamp(labelLeft, columnLeft, columnRight - labelWidth);

            float captionX = labelLeft - columnLeft;

            mCaptionLabel.style.translate = new Translate(captionX, scrollY);
        }

        readonly VisualElement mHeaderBackground;
        readonly Label mCaptionLabel;

        const float TextPaddingX = 8;
    }
}
