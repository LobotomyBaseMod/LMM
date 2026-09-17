using System.Collections.Generic;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.CM.Common;
using CodiceApp.Gravatar;
using PlasticGui;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class ChangesetShape : BrExShape
    {
        // internal for testing purposes
        internal bool IsHovered { get { return mIsHovered; } }

        internal ChangesetDrawInfo ChangesetDraw { get { return VirtualShape.DrawInfo as ChangesetDrawInfo; } }

        internal ChangesetShape(
            VirtualShape virtualShape,
            ColorProvider colorProvider,
            AsyncUserNameResolver userNameResolver) : base(virtualShape)
        {
            mColorProvider = colorProvider;
            mUserNameResolver = userNameResolver;

            if (DisplayOptions.ChangesetColorMode.HasFlag(ChangesetColorMode.ByUser))
            {
                UpdateAvatar();
            }

            if (ChangesetDraw.IsWorkspaceChangeset && ChangesetDraw.Branch.IsWorkspaceBranch)
            {
                CreateHomeImage();
            }

            SetupHoverEffects();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        void CreateHomeImage()
        {
            mHomeImage = new HomeImageOverlay(
                HomeGeometry.WIDTH,
                HomeGeometry.HEIGHT);

            UpdateHomeImageBorder();

            mHomeImage.style.position = Position.Absolute;
            mHomeImage.style.right = -3;
            mHomeImage.style.bottom = -3;

            Add(mHomeImage);
        }

        internal override void Dispose()
        {
            base.Dispose();

            mIsHovered = false;
            mHomeImage?.Dispose();
            UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        internal override void Redraw()
        {
            base.Redraw();

            UpdateHomeImageBorder();
            UpdateGlowColor();

            if (!DisplayOptions.ChangesetColorMode.HasFlag(ChangesetColorMode.ByUser))
            {
                HideAvatarControls();
                return;
            }

            UpdateAvatar();
        }

        void UpdateHomeImageBorder()
        {
            if (mHomeImage == null)
                return;

            Color borderColor = mColorProvider.GetChangesetColor(
                this, IsMultiChangesetSelected());

            mHomeImage.style.borderLeftColor = borderColor;
            mHomeImage.style.borderRightColor = borderColor;
            mHomeImage.style.borderTopColor = borderColor;
            mHomeImage.style.borderBottomColor = borderColor;
        }

        internal override bool HasContextMenu() => true;

        public override bool ContainsPoint(Vector2 localPoint)
        {
            Vector2 center = new Vector2(
                BrExDrawProperties.ChangesetRadius,
                BrExDrawProperties.ChangesetRadius);

            float strokeWidth = BrExLineWidths.Changeset.GetOuterBorderWidth(
                ChangesetDraw.IsCheckoutChangeset);

            return Vector2.Distance(localPoint, center) <= BrExDrawProperties.ChangesetRadius + strokeWidth;
        }

        protected override void GenerateVisualContent(Painter2D painter)
        {
            Color changesetColor = mColorProvider.GetChangesetColor(
                this, IsMultiChangesetSelected());

            if (DrawWithOpacity())
            {
                //context.DrawGeometry(ThemeBrushes.Name.ControlBrush.GetBrush(), null, DefiningGeometry);
                //opacity = context.PushOpacity(0.3);
            }

            // outer fill
            painter.fillColor = UnityStyles.Colors.BranchExplorer.ControlBackgroundColor;
            painter.DrawCircle(
                new Vector2(BrExDrawProperties.ChangesetRadius, BrExDrawProperties.ChangesetRadius),
                BrExDrawProperties.ChangesetRadius);
            painter.Fill();

            // outer border
            painter.strokeColor = changesetColor;
            painter.lineWidth = BrExLineWidths.Changeset.GetOuterBorderWidth(ChangesetDraw.IsCheckoutChangeset);

            painter.StrokeCircle(
                new Vector2(BrExDrawProperties.ChangesetRadius, BrExDrawProperties.ChangesetRadius),
                BrExDrawProperties.ChangesetRadius,
                BrExDashes.GetChangesetDashPattern(ChangesetDraw.IsCheckoutChangeset));

            if (ChangesetDraw.IsHead && !ChangesetDraw.IsCheckoutChangeset)
            {
                // head border
                painter.strokeColor = changesetColor;
                painter.lineWidth = BrExLineWidths.Changeset.GetHeadBorderWidth();

                painter.DrawCircle(
                    new Vector2(BrExDrawProperties.ChangesetRadius, BrExDrawProperties.ChangesetRadius),
                    BrExDrawProperties.ChangesetHeadRadius);

                painter.Stroke();
            }

            if (!ChangesetDraw.IsCheckoutChangeset)
            {
                // changeset fill
                painter.fillColor = changesetColor;

                painter.DrawCircle(
                    new Vector2(BrExDrawProperties.ChangesetRadius, BrExDrawProperties.ChangesetRadius),
                    BrExDrawProperties.ChangesetFillRadius);

                painter.Fill();

                if (mIsHovered)
                {
                    painter.fillColor = new Color(1f, 1f, 1f, HoverHighlightAlpha);

                    painter.DrawCircle(
                        new Vector2(BrExDrawProperties.ChangesetRadius, BrExDrawProperties.ChangesetRadius),
                        BrExDrawProperties.ChangesetFillRadius);

                    painter.Fill();
                }
            }
        }

        bool IsMultiChangesetSelected()
        {
            BranchExplorerViewer brExView = this.GetFirstAncestorOfType<BranchExplorerViewer>();
            if (brExView == null)
                return false;

            return brExView.Selection.GetSelectedChangesets().Count > 1;
        }

        bool DrawWithOpacity()
        {
            if (IsSearchResult || IsSelected)
                return false;

            if (DisplayOptions.UserFilter == null)
                return false;

            // draw with opacity if the user is filtered
            return !DisplayOptions.UserFilter.Contains(((BrExChangeset)ChangesetDraw.Tag).Owner);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (!DisplayOptions.ChangesetColorMode.HasFlag(ChangesetColorMode.ByUser))
                return;

            UpdateAvatar();
        }

        void UpdateAvatar()
        {
            SEID owner = ((BrExChangeset)ChangesetDraw.Tag).Owner;

            if (owner == null)
            {
                HideAvatarControls();
                return;
            }

            string resolvedUserName = mUserNameResolver.GetResolvedUserName(owner);

            if (string.IsNullOrEmpty(resolvedUserName))
            {
                mUserNameResolver.RequestUserName(owner);
                HideAvatarControls();
                return;
            }

            VirtualCanvas canvas = GetFirstAncestorOfType<VirtualCanvas>();

            if (canvas == null)
                return;

            GetAvatar.ForEmail(resolvedUserName, canvas.RedrawChangesetShapes);

            if (AvatarImages.HasDownloadedGravatar(resolvedUserName))
            {
                CreateAvatarImage();
                UpdateAvatarImage(AvatarImages.GetAvatar(resolvedUserName));
                HideAvatarLabel();
                return;
            }

            CreateAvatarLabel();
            UpdateAvatarLabel(resolvedUserName);
            HideAvatarImage();
        }

        void CreateAvatarLabel()
        {
            if (mAvatarLabel != null)
                return;

            int radius = BrExDrawProperties.ChangesetFillRadius;

            mAvatarLabel = new Label();
            mAvatarLabel.style.position = Position.Absolute;
            mAvatarLabel.style.paddingLeft = 1;
            mAvatarLabel.style.paddingRight = 1;
            mAvatarLabel.style.borderBottomLeftRadius = radius;
            mAvatarLabel.style.borderBottomRightRadius = radius;
            mAvatarLabel.style.borderTopLeftRadius = radius;
            mAvatarLabel.style.borderTopRightRadius = radius;
            mAvatarLabel.style.color = Color.white;
            mAvatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            int offset = BrExDrawProperties.ChangesetRadius - radius;
            mAvatarLabel.style.left = offset;
            mAvatarLabel.style.top = offset;

            mAvatarLabel.style.width = radius * 2;
            mAvatarLabel.style.height = radius * 2;

            Add(mAvatarLabel);

            mHomeImage?.BringToFront();
        }

        void CreateAvatarImage()
        {
            if (mGravatarImage != null)
                return;

            int radius = BrExDrawProperties.ChangesetFillRadius;
            int offset = BrExDrawProperties.ChangesetRadius - radius;

            mGravatarImage = new Image();

            mGravatarImage.style.borderBottomLeftRadius = radius;
            mGravatarImage.style.borderBottomRightRadius = radius;
            mGravatarImage.style.borderTopLeftRadius = radius;
            mGravatarImage.style.borderTopRightRadius = radius;
            mGravatarImage.style.overflow = Overflow.Hidden;

            mGravatarImage.style.left = offset;
            mGravatarImage.style.top = offset;

            mGravatarImage.style.width = radius * 2;
            mGravatarImage.style.height = radius * 2;

            Add(mGravatarImage);

            mHomeImage?.BringToFront();
        }

        void UpdateAvatarLabel(string resolvedUserName)
        {
            if (mAvatarLabel == null)
                return;

            mAvatarLabel.visible = true;

            schedule.Execute(() =>
            {
                // check the element is still valid and attached
                if (mAvatarLabel != null && panel != null)
                {
                    mAvatarLabel.text = GetAvatarInitial.ForUserName(resolvedUserName);
                    mAvatarLabel.style.backgroundColor = AvatarColor.FromUserName(resolvedUserName);
                }
            });
        }

        void UpdateAvatarImage(Texture2D avatarImage)
        {
            if (mGravatarImage == null)
                return;

            mGravatarImage.visible = true;

            schedule.Execute(() =>
            {
                // check the element is still valid and attached
                if (mGravatarImage != null && panel != null)
                    mGravatarImage.image = avatarImage;
            });
        }

        void HideAvatarControls()
        {
            HideAvatarLabel();
            HideAvatarImage();
        }

        void HideAvatarLabel()
        {
            if (mAvatarLabel != null)
                mAvatarLabel.visible = false;
        }

        void HideAvatarImage()
        {
            if (mGravatarImage != null)
                mGravatarImage.visible = false;
        }

        // internal for testing purposes
        internal void OnPointerEnter(PointerEnterEvent evt)
        {
            // don't expand while dragging
            if (evt.pressedButtons != 0)
                return;

            mIsHovered = true;

            style.scale = new Scale(new Vector3(HoverScale, HoverScale, 1f));

            UpdateGlowColor();
            mGlowRing.style.opacity = 1;
            mGlowRing.style.scale = new Scale(Vector3.one);

            MarkDirtyRepaint();

            VirtualCanvas canvas = GetFirstAncestorOfType<VirtualCanvas>();
            ChangesetCommentShape commentShape =
                ChangesetCommentShape.FindForChangeset(canvas, ChangesetDraw);
            commentShape?.Expand();
        }

        void UpdateGlowColor()
        {
            Color changesetColor = mColorProvider.GetChangesetColor(
                this, IsMultiChangesetSelected());
            Color glowColor = new Color(
                changesetColor.r, changesetColor.g, changesetColor.b, GlowAlpha);

            mGlowRing.style.borderLeftColor = glowColor;
            mGlowRing.style.borderRightColor = glowColor;
            mGlowRing.style.borderTopColor = glowColor;
            mGlowRing.style.borderBottomColor = glowColor;
        }

        // internal for testing purposes
        internal void OnPointerLeave(PointerLeaveEvent evt)
        {
            mIsHovered = false;

            style.scale = new Scale(Vector3.one);

            // Fade out and shrink glow ring
            mGlowRing.style.opacity = 0;
            mGlowRing.style.scale = new Scale(
                new Vector3(GlowStartScale, GlowStartScale, 1f));

            MarkDirtyRepaint();

            VirtualCanvas canvas = GetFirstAncestorOfType<VirtualCanvas>();

            if (canvas == null)
                return;

            // use the comment shape's delayed collapse so that if the mouse
            // moves from the changeset directly onto the comment, the
            // comment's Expand() cancels this scheduled collapse.
            ChangesetCommentShape commentShape =
                ChangesetCommentShape.FindForChangeset(canvas, ChangesetDraw);
            commentShape?.ScheduleCollapse();
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

            float glowDiameter = BrExDrawProperties.ChangesetRadius * 2 + GlowExtent * 2;
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

        bool mIsHovered;
        VisualElement mGlowRing;
        Label mAvatarLabel;
        Image mGravatarImage;
        HomeImageOverlay mHomeImage;
        readonly AsyncUserNameResolver mUserNameResolver;

        readonly ColorProvider mColorProvider;

        const int HoverAnimationMs = 200;
        const int GlowExpandMs = 350;
        const float HoverScale = 1.15f;
        const float GlowExtent = 8f;
        const float GlowThickness = 4f;
        const float GlowAlpha = 0.35f;
        const float GlowStartScale = 0.7f;
        const float HoverHighlightAlpha = 0.12f;
    }
}
