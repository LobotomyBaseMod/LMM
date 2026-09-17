using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Client.BaseCommands.BranchExplorer;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class LabelShape : BrExShape
    {
        // internal for testing purposes
        internal bool IsHovered { get { return mIsHovered; } }

        internal LabelDrawInfo LabelDraw => VirtualShape.DrawInfo as LabelDrawInfo;

        internal LabelShape(VirtualShape virtualShape, ColorProvider colorProvider) : base(virtualShape)
        {
            mColorProvider = colorProvider;

            CreateHitTestOverlay();
            CreateSummaryCaptionContainer();
            CreateExpandedCaptionContainer();
            SetupHoverEffects();

            mHitTestOverlay.RegisterCallback<PointerMoveEvent>(OnOverlayPointerMove);
            mHitTestOverlay.RegisterCallback<PointerLeaveEvent>(OnOverlayPointerLeave);
            mSummaryCaptionContainer.RegisterCallback<GeometryChangedEvent>(OnSummaryContainerGeometryChanged);
            mExpandedCaptionLabel.RegisterCallback<GeometryChangedEvent>(OnExpandedLabelGeometryChanged);

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        internal override void Dispose()
        {
            mHitTestOverlay.UnregisterCallback<PointerMoveEvent>(OnOverlayPointerMove);
            mHitTestOverlay.UnregisterCallback<PointerLeaveEvent>(OnOverlayPointerLeave);
            mSummaryCaptionContainer.UnregisterCallback<GeometryChangedEvent>(OnSummaryContainerGeometryChanged);
            mExpandedCaptionLabel.UnregisterCallback<GeometryChangedEvent>(OnExpandedLabelGeometryChanged);
            UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            base.Dispose();
        }

        internal override bool HasContextMenu() => true;

        protected override void GenerateVisualContent(Painter2D painter)
        {
            if (!DisplayOptions.DisplayLabels)
                return;

            Color labelColor = mColorProvider.GetLabelColor(this, IsMultiLabelSelected());

            if (mIsHovered)
                labelColor = BrightenColor(labelColor, HoverBrightenAmount);

            painter.strokeColor = labelColor;
            painter.lineWidth = mIsHovered
                ? BrExLineWidths.Label.GetWidth() + HoverExtraLineWidth
                : BrExLineWidths.Label.GetWidth();

            painter.DrawCircle(
                new Vector2(BrExDrawProperties.LabelRadius, BrExDrawProperties.LabelRadius),
                BrExDrawProperties.LabelRadius);

            painter.Stroke();
        }

        void CreateHitTestOverlay()
        {
            // create a transparent overlay that covers both the label shape and caption area
            // this is needed because captions are positioned with negative Y (above the shape),
            // and pointer events only fire within an element's bounds
            // width matches the summary container for better usability
            mHitTestOverlay = new VisualElement();
            mHitTestOverlay.style.position = Position.Absolute;
            mHitTestOverlay.style.left = -CaptionExtraMargin / 2;
            mHitTestOverlay.style.width = BrExDrawProperties.LabelRadius * 2 + CaptionExtraMargin;
            mHitTestOverlay.pickingMode = PickingMode.Position;

            Add(mHitTestOverlay);
        }

        void CreateSummaryCaptionContainer()
        {
            mSummaryCaptionContainer = new VisualElement();
            mSummaryCaptionContainer.style.position = Position.Absolute;
            mSummaryCaptionContainer.style.width = BrExDrawProperties.LabelRadius * 2 + CaptionExtraMargin;
            mSummaryCaptionContainer.style.left = -CaptionExtraMargin / 2;
            mSummaryCaptionContainer.pickingMode = PickingMode.Ignore;

            mSummaryCaptionLabels = new List<Label>();
            string[] captions = GetLabelCaptionsArray(LabelDraw.Labels, MaxLabelCaptions);

            foreach (string caption in captions)
            {
                Label label = CreateSummaryCaptionLabel(caption);
                mSummaryCaptionLabels.Add(label);
                mSummaryCaptionContainer.Add(label);
            }

            Add(mSummaryCaptionContainer);
        }

        Label CreateSummaryCaptionLabel(string text)
        {
            Label label = new Label();
            label.style.color = GetCaptionForegroundColor(IsSelected);
            label.style.fontSize = 11;
            label.style.unityTextAlign = TextAnchor.UpperCenter;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.marginBottom = 0;
            label.style.marginTop = 0;
            label.style.paddingBottom = 0;
            label.style.paddingTop = 0;
            label.pickingMode = PickingMode.Ignore;
            label.text = text;
            return label;
        }

        void CreateExpandedCaptionContainer()
        {
            mExpandedCaptionContainer = new VisualElement();
            mExpandedCaptionContainer.style.position = Position.Absolute;
            mExpandedCaptionContainer.style.backgroundColor = UnityStyles.Colors.BranchExplorer.ControlBackgroundColor;
            mExpandedCaptionContainer.style.borderTopLeftRadius = CornerRadius;
            mExpandedCaptionContainer.style.borderTopRightRadius = CornerRadius;
            mExpandedCaptionContainer.style.borderBottomLeftRadius = CornerRadius;
            mExpandedCaptionContainer.style.borderBottomRightRadius = CornerRadius;
            mExpandedCaptionContainer.style.paddingLeft = ExpandedPadding;
            mExpandedCaptionContainer.style.paddingRight = ExpandedPadding;
            mExpandedCaptionContainer.style.paddingTop = ExpandedPadding;
            mExpandedCaptionContainer.style.paddingBottom = ExpandedPadding;
            mExpandedCaptionContainer.style.display = DisplayStyle.None;
            mExpandedCaptionContainer.style.borderTopWidth = ExpandedBorderWidth;
            mExpandedCaptionContainer.style.borderBottomWidth = ExpandedBorderWidth;
            mExpandedCaptionContainer.style.borderLeftWidth = ExpandedBorderWidth;
            mExpandedCaptionContainer.style.borderRightWidth = ExpandedBorderWidth;
            mExpandedCaptionContainer.style.borderTopColor = BrExColors.Label.GetCaptionBorderColor();
            mExpandedCaptionContainer.style.borderBottomColor = BrExColors.Label.GetCaptionBorderColor();
            mExpandedCaptionContainer.style.borderLeftColor = BrExColors.Label.GetCaptionBorderColor();
            mExpandedCaptionContainer.style.borderRightColor = BrExColors.Label.GetCaptionBorderColor();
            mExpandedCaptionContainer.pickingMode = PickingMode.Ignore;

            mExpandedCaptionLabel = new Label();
            mExpandedCaptionLabel.style.color = GetCaptionForegroundColor(true);
            mExpandedCaptionLabel.style.fontSize = 11;
            mExpandedCaptionLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            mExpandedCaptionLabel.style.whiteSpace = WhiteSpace.Normal;
            mExpandedCaptionLabel.style.marginBottom = 0;
            mExpandedCaptionLabel.style.marginTop = 0;
            mExpandedCaptionLabel.style.paddingBottom = 0;
            mExpandedCaptionLabel.style.paddingTop = 0;
            mExpandedCaptionLabel.pickingMode = PickingMode.Ignore;
            mExpandedCaptionLabel.text = GetAllLabelCaptions(LabelDraw.Labels);

            mExpandedCaptionContainer.Add(mExpandedCaptionLabel);
            Add(mExpandedCaptionContainer);
        }

        void OnSummaryContainerGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateSummaryCaptionPosition();
            UpdateCaptionHitArea();
        }

        void OnExpandedLabelGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateExpandedCaptionPosition();
        }

        void OnOverlayPointerMove(PointerMoveEvent evt)
        {
            bool isInsideCaptionArea = mCaptionHitAreaInOverlay.Contains(evt.localPosition);

            if (isInsideCaptionArea)
            {
                ShowExpandedCaption();
                return;
            }

            ShowSummaryCaption();
        }

        void OnOverlayPointerLeave(PointerLeaveEvent evt)
        {
            ShowSummaryCaption();
        }

        void ShowExpandedCaption()
        {
            if (mIsExpanded)
                return;

            if (!DisplayOptions.DisplayLabels)
                return;

            mIsExpanded = true;
            mSummaryCaptionContainer.style.display = DisplayStyle.None;
            mExpandedCaptionContainer.style.display = DisplayStyle.Flex;
            BringToFront();
        }

        void ShowSummaryCaption()
        {
            if (!mIsExpanded)
                return;

            mIsExpanded = false;
            mExpandedCaptionContainer.style.display = DisplayStyle.None;
            mSummaryCaptionContainer.style.display = DisplayStyle.Flex;

            VirtualCanvas canvas = parent as VirtualCanvas;
            canvas?.RestoreZOrder(this);
        }

        void UpdateCaptionHitArea()
        {
            float containerHeight = mSummaryCaptionContainer.resolvedStyle.height;
            if (float.IsNaN(containerHeight) || containerHeight <= 0)
                return;

            float captionTop = containerHeight + CaptionMargin;

            mHitTestOverlay.style.top = -captionTop;
            mHitTestOverlay.style.height = captionTop + BrExDrawProperties.LabelRadius * 2;

            mCaptionHitAreaInOverlay = new Rect(
                0,
                0,
                BrExDrawProperties.LabelRadius * 2 + CaptionExtraMargin,
                containerHeight);
        }

        void UpdateSummaryCaptionPosition()
        {
            float containerHeight = mSummaryCaptionContainer.resolvedStyle.height;
            if (float.IsNaN(containerHeight) || containerHeight <= 0)
                return;

            mSummaryCaptionContainer.style.top = -containerHeight - CaptionMargin;
        }

        void UpdateExpandedCaptionPosition()
        {
            float textHeight = mExpandedCaptionLabel.resolvedStyle.height;
            float textWidth = mExpandedCaptionLabel.resolvedStyle.width;

            if (float.IsNaN(textHeight) || textHeight <= 0)
                return;
            if (float.IsNaN(textWidth) || textWidth <= 0)
                return;

            float textY = -textHeight - CaptionMargin;
            float textX = (BrExDrawProperties.LabelRadius * 2 - textWidth) / 2;

            mExpandedCaptionContainer.style.left = textX - ExpandedPadding - ExpandedBorderWidth;
            mExpandedCaptionContainer.style.top = textY - ExpandedPadding - ExpandedBorderWidth;
        }

        internal override void Redraw()
        {
            UpdateSummaryCaptionLabels();
            mExpandedCaptionLabel.text = GetAllLabelCaptions(LabelDraw.Labels);

            bool displayLabels = DisplayOptions.DisplayLabels;
            mSummaryCaptionContainer.style.display =
                displayLabels ? DisplayStyle.Flex : DisplayStyle.None;
            mHitTestOverlay.style.display =
                displayLabels ? DisplayStyle.Flex : DisplayStyle.None;

            if (!displayLabels)
            {
                mExpandedCaptionContainer.style.display = DisplayStyle.None;
                mIsExpanded = false;
            }

            base.Redraw();
        }

        void UpdateSummaryCaptionLabels()
        {
            string[] captions = GetLabelCaptionsArray(LabelDraw.Labels, MaxLabelCaptions);
            Color captionColor = GetCaptionForegroundColor(IsSelected);

            // Remove excess labels if needed
            while (mSummaryCaptionLabels.Count > captions.Length)
            {
                Label labelToRemove = mSummaryCaptionLabels[mSummaryCaptionLabels.Count - 1];
                mSummaryCaptionContainer.Remove(labelToRemove);
                mSummaryCaptionLabels.RemoveAt(mSummaryCaptionLabels.Count - 1);
            }

            // Update existing labels and add new ones if needed
            for (int i = 0; i < captions.Length; i++)
            {
                if (i < mSummaryCaptionLabels.Count)
                {
                    mSummaryCaptionLabels[i].text = captions[i];
                    mSummaryCaptionLabels[i].style.color = captionColor;
                }
                else
                {
                    Label label = CreateSummaryCaptionLabel(captions[i]);
                    mSummaryCaptionLabels.Add(label);
                    mSummaryCaptionContainer.Add(label);
                }
            }
        }

        static string GetAllLabelCaptions(BrExLabel[] labels)
        {
            return GetLabelCaptions(labels, 10000);
        }

        static string GetLabelCaptions(BrExLabel[] labels, int maxCaptions)
        {
            return string.Join(Environment.NewLine, GetLabelCaptionsArray(labels, maxCaptions));
        }

        static string[] GetLabelCaptionsArray(BrExLabel[] labels, int maxCaptions)
        {
            IEnumerable<BrExLabel> sortedLabels = labels.Reverse();

            if (labels.Length <= maxCaptions)
                return sortedLabels.Select(x => x.Name).ToArray();

            List<string> result = sortedLabels.Take(maxCaptions - 1).Select(x => x.Name).ToList();
            result.Add(PlasticLocalization.GetString(
                PlasticLocalization.Name.PlusMore, labels.Length - maxCaptions + 1));

            return result.ToArray();
        }

        static Color GetCaptionForegroundColor(bool isSelected)
        {
            return isSelected ? UnityStyles.Colors.Label : UnityStyles.Colors.SecondaryLabel;
        }

        // internal for testing purposes
        internal void OnPointerEnter(PointerEnterEvent evt)
        {
            if (evt.pressedButtons != 0)
                return;

            mIsHovered = true;

            style.scale = new Scale(new Vector3(HoverScale, HoverScale, 1f));

            Color labelColor = mColorProvider.GetLabelColor(this, IsMultiLabelSelected());
            Color glowColor = new Color(
                labelColor.r, labelColor.g, labelColor.b, GlowAlpha);

            mGlowRing.style.borderLeftColor = glowColor;
            mGlowRing.style.borderRightColor = glowColor;
            mGlowRing.style.borderTopColor = glowColor;
            mGlowRing.style.borderBottomColor = glowColor;
            mGlowRing.style.opacity = 1;
            mGlowRing.style.scale = new Scale(Vector3.one);

            MarkDirtyRepaint();
        }

        // internal for testing purposes
        internal void OnPointerLeave(PointerLeaveEvent evt)
        {
            mIsHovered = false;

            style.scale = new Scale(Vector3.one);

            mGlowRing.style.opacity = 0;
            mGlowRing.style.scale = new Scale(
                new Vector3(GlowStartScale, GlowStartScale, 1f));

            MarkDirtyRepaint();

            if (!mIsExpanded)
            {
                VirtualCanvas canvas = parent as VirtualCanvas;
                canvas?.RestoreZOrder(this);
            }
        }

        void SetupHoverEffects()
        {
            style.overflow = Overflow.Visible;

            style.transitionProperty = new List<StylePropertyName>
            {
                new StylePropertyName("scale"),
            };
            style.transitionDuration = new List<TimeValue>
            {
                new TimeValue(HoverAnimationMs, TimeUnit.Millisecond),
            };
            style.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(EasingMode.EaseOutCubic),
            };
            style.transformOrigin = new TransformOrigin(
                Length.Percent(50), Length.Percent(50));

            float glowDiameter = BrExDrawProperties.LabelRadius * 2 + GlowExtent * 2;
            float glowRadius = glowDiameter / 2f;

            mGlowRing = new VisualElement();
            mGlowRing.style.position = Position.Absolute;
            mGlowRing.style.width = glowDiameter;
            mGlowRing.style.height = glowDiameter;
            mGlowRing.style.left = -GlowExtent;
            mGlowRing.style.top = -GlowExtent;
            mGlowRing.style.borderBottomLeftRadius = glowRadius;
            mGlowRing.style.borderBottomRightRadius = glowRadius;
            mGlowRing.style.borderTopLeftRadius = glowRadius;
            mGlowRing.style.borderTopRightRadius = glowRadius;
            mGlowRing.style.backgroundColor = Color.clear;
            mGlowRing.style.borderLeftWidth = GlowThickness;
            mGlowRing.style.borderRightWidth = GlowThickness;
            mGlowRing.style.borderTopWidth = GlowThickness;
            mGlowRing.style.borderBottomWidth = GlowThickness;
            mGlowRing.style.opacity = 0;
            mGlowRing.pickingMode = PickingMode.Ignore;

            mGlowRing.style.transitionProperty = new List<StylePropertyName>
            {
                new StylePropertyName("opacity"),
                new StylePropertyName("scale"),
            };
            mGlowRing.style.transitionDuration = new List<TimeValue>
            {
                new TimeValue(HoverAnimationMs, TimeUnit.Millisecond),
                new TimeValue(GlowExpandMs, TimeUnit.Millisecond),
            };
            mGlowRing.style.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(EasingMode.EaseOutCubic),
                new EasingFunction(EasingMode.EaseOutCubic),
            };
            mGlowRing.style.transformOrigin = new TransformOrigin(
                Length.Percent(50), Length.Percent(50));
            mGlowRing.style.scale = new Scale(
                new Vector3(GlowStartScale, GlowStartScale, 1f));

            Insert(0, mGlowRing);
        }

        static Color BrightenColor(Color color, float amount)
        {
            return new Color(
                Mathf.Min(1f, color.r + amount),
                Mathf.Min(1f, color.g + amount),
                Mathf.Min(1f, color.b + amount),
                Mathf.Clamp01(color.a + amount * 0.5f));
        }

        bool IsMultiLabelSelected()
        {
            return false;
        }

        bool mIsHovered;
        VisualElement mGlowRing;
        VisualElement mHitTestOverlay;
        VisualElement mSummaryCaptionContainer;
        List<Label> mSummaryCaptionLabels;
        VisualElement mExpandedCaptionContainer;
        Label mExpandedCaptionLabel;

        Rect mCaptionHitAreaInOverlay;
        bool mIsExpanded;
        readonly ColorProvider mColorProvider;

        const float CaptionMargin = 3;
        const float CaptionExtraMargin = 18;
        const float ExpandedPadding = 5;
        const float ExpandedBorderWidth = 1;
        const float CornerRadius = 4;
        const int MaxLabelCaptions = 3;
        const float HoverBrightenAmount = 0.1f;
        const float HoverExtraLineWidth = 0f;
        const int HoverAnimationMs = 200;
        const int GlowExpandMs = 300;
        const float HoverScale = 1.02f;
        const float GlowExtent = 6f;
        const float GlowThickness = 3f;
        const float GlowAlpha = 0.3f;
        const float GlowStartScale = 0.75f;
    }
}
