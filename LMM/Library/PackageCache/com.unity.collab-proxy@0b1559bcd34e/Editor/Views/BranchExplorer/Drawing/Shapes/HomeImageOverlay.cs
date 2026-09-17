using Unity.PlasticSCM.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class HomeImageOverlay : VisualElement
    {
        internal HomeImageOverlay(
            float width,
            float height)
        {
            style.width = width;
            style.height = height;
            style.backgroundColor = UnityStyles.Colors.BranchExplorer.ControlBackgroundColor;

            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;

            style.borderBottomLeftRadius = width / 2;
            style.borderBottomRightRadius = width / 2;
            style.borderTopLeftRadius = height / 2;
            style.borderTopRightRadius = height / 2;
            generateVisualContent += OnGenerateVisualContent;
        }

        internal void Dispose()
        {
            generateVisualContent -= OnGenerateVisualContent;
        }

        void OnGenerateVisualContent(MeshGenerationContext obj)
        {
#if UNITY_2022_1_OR_NEWER
            Painter2D painter = obj.painter2D;
#else
            Painter2D painter = new Painter2D();
#endif

            painter.fillColor = UnityStyles.Colors.ImageForeground;
            float scale = 0.65f;
            float glyphWidth = HomeGeometry.WIDTH * scale;
            float glyphHeight = HomeGeometry.HEIGHT * scale;
            float offsetX = (localBound.width - glyphWidth) * 0.5f;
            float offsetY = (localBound.height - glyphHeight) * 0.5f;

            HomeGeometry.Draw(painter, offsetX, offsetY - 0.5f, scale);
            painter.Fill();
        }
    }
}
