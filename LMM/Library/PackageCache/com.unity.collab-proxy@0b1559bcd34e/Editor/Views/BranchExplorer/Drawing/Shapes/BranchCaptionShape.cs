using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using PlasticGui;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class BranchCaptionShape : BrExShape
    {
        internal BranchDrawInfo BranchDraw => VirtualShape.DrawInfo as BranchDrawInfo;

        internal BranchCaptionShape(
            VirtualShape virtualShape,
            AsyncTaskLoader taskLoader) : base(virtualShape)
        {
            mTaskLoader = taskLoader;

            mCaptionContainer = new VisualElement();
            mCaptionContainer.style.position = Position.Absolute;
            mCaptionContainer.style.backgroundColor = BrExColors.Branch.GetCaptionBackgroundBrush();
            mCaptionContainer.style.borderTopLeftRadius = CornerRadius;
            mCaptionContainer.style.borderTopRightRadius = CornerRadius;
            mCaptionContainer.style.borderBottomLeftRadius = CornerRadius;
            mCaptionContainer.style.borderBottomRightRadius = CornerRadius;
            mCaptionContainer.style.paddingLeft = TextPaddingX;
            mCaptionContainer.style.paddingRight = TextPaddingX;
            mCaptionContainer.style.paddingTop = TextPaddingY;
            mCaptionContainer.style.paddingBottom = TextPaddingY;
            mCaptionContainer.style.maxWidth = BranchDraw.Bounds.Width + 2 * TextPaddingX;
            mCaptionContainer.style.maxHeight = MaxHeight;

            mCaptionLabel = new Label();
            mCaptionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            mCaptionLabel.style.color = UnityStyles.Colors.Label;
            mCaptionLabel.style.marginBottom = 0;
            mCaptionLabel.style.marginTop = 0;
            mCaptionLabel.style.paddingBottom = 0;
            mCaptionLabel.style.paddingTop = 0;
            mCaptionLabel.style.whiteSpace = WhiteSpace.Normal;
            mCaptionLabel.style.textOverflow = TextOverflow.Ellipsis;
            mCaptionLabel.style.overflow = Overflow.Hidden;
            mCaptionLabel.RegisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);

            mDescriptionLabel = new Label();
            mDescriptionLabel.style.fontSize = 10;
            mDescriptionLabel.style.color = UnityStyles.Colors.SecondaryLabel;
            mDescriptionLabel.style.marginBottom = 0;
            mDescriptionLabel.style.marginTop = 0;
            mDescriptionLabel.style.paddingBottom = 0;
            mDescriptionLabel.style.paddingTop = 0;
            mDescriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            mDescriptionLabel.style.textOverflow = TextOverflow.Ellipsis;
            mDescriptionLabel.style.overflow = Overflow.Hidden;
            mDescriptionLabel.RegisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);

            mCaptionLabel.text = GetBranchName(BranchDraw, DisplayOptions);
            mDescriptionLabel.text = GetBranchDescription(BranchDraw, DisplayOptions, mTaskLoader);

            mCaptionContainer.Add(mCaptionLabel);
            mCaptionContainer.Add(mDescriptionLabel);
            Add(mCaptionContainer);
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return mCaptionContainer.localBound.Contains(localPoint);
        }

        internal override void Dispose()
        {
            base.Dispose();

            mCaptionLabel.UnregisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
            mDescriptionLabel.UnregisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
        }

        internal override void Redraw()
        {
            mCaptionLabel.text = GetBranchName(BranchDraw , DisplayOptions);
            mDescriptionLabel.text = GetBranchDescription(BranchDraw, DisplayOptions, mTaskLoader);
            UpdateLayout();
            base.Redraw();
        }

        internal override bool HasContextMenu() => true;

        internal override void OnScrollChanged()
        {
            UpdateContainerLayout();
        }

        protected override void GenerateVisualContent(Painter2D painter)
        {
            if (!DisplayOptions.DisplayTaskInfoOnBranches)
                return;

            mTaskLoader.RequestTaskInfo(new List<BranchRequest>()
            {
                new BranchRequest(BranchDraw.Id, BranchDraw.Caption),
            });
        }

        protected internal override VirtualShape GetVirtualShapeForSelection()
        {
            // return the branch shape so selection highlights the whole branch
            VirtualCanvas canvas = GetFirstAncestorOfType<VirtualCanvas>();

            foreach (BrExShape brExShape in canvas.GetShapes())
            {
                if (brExShape is BranchShape branchShape &&
                    branchShape.BranchDrawInfo.Guid == BranchDraw.Guid)
                    return brExShape.VirtualShape;
            }

            return null;
        }

        void OnLabelGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateLayout();
        }

        void UpdateLayout()
        {
            if (HasDescription())
            {
                mCaptionLabel.style.maxHeight = MaxHeight * 0.7f;
                mDescriptionLabel.style.maxHeight = MaxHeight - mCaptionLabel.resolvedStyle.height;
            }
            else
            {
                mCaptionLabel.style.maxHeight = MaxHeight;
                mDescriptionLabel.style.maxHeight = 0;
            }

            UpdateContainerLayout();
        }

        void UpdateContainerLayout()
        {
            VirtualCanvas canvas = this.GetFirstAncestorOfType<VirtualCanvas>();
            if (canvas == null)
                return;

            float translatedScrollX = (canvas.ScrollOffset.x / canvas.ZoomLevel) - BranchDraw.Bounds.X;

            float captionX = translatedScrollX > 0 ?
                translatedScrollX + BrExDrawProperties.BranchCaptionOffsetX :
                0;

            float containerHeight = mCaptionContainer.resolvedStyle.height;
            if (float.IsNaN(containerHeight) || containerHeight == 0)
            {
                // estimate height from labels
                containerHeight = mCaptionLabel.resolvedStyle.height +
                                  (HasDescription() ? mDescriptionLabel.resolvedStyle.height : 0) +
                                  2 * TextPaddingY;
            }

            float captionY = -containerHeight - VerticalMargin;

            mCaptionContainer.style.translate = new Translate(
                captionX,
                captionY);
        }

        bool HasDescription()
        {
            return !string.IsNullOrEmpty(mDescriptionLabel.text);
        }

        static string GetBranchName(
            BranchDrawInfo branch,
            DisplayOptions displayOptions)
        {
            return displayOptions.DisplayFullBranchNames ?
                branch.Caption :
                ((BrExBranch)branch.Tag).Name;
        }

        static string GetBranchDescription(
            BranchDrawInfo branch,
            DisplayOptions displayOptions,
            AsyncTaskLoader taskLoader)
        {
            if (!displayOptions.DisplayTaskInfoOnBranches)
                return string.Empty;

            return CommentFormatter.GetFormattedComment(
                taskLoader.GetTaskInfoForBranch(branch.Caption));
        }

        readonly VisualElement mCaptionContainer;
        readonly Label mCaptionLabel;
        readonly Label mDescriptionLabel;
        readonly AsyncTaskLoader mTaskLoader;

        const float TextPaddingX = 12;
        const float TextPaddingY = 3;
        const float VerticalMargin = 5;
        const float CornerRadius = 6;
        const float MaxHeight = 80;
    }
}
