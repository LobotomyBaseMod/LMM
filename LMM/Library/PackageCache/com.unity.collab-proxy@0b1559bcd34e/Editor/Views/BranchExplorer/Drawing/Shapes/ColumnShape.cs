using Codice.Client.BaseCommands.BranchExplorer;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.BranchExplorer.Virtualization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes
{
    internal class ColumnShape : BrExShape
    {
        internal ColumnDrawInfo ColumnDraw { get { return VirtualShape.DrawInfo as ColumnDrawInfo; } }

        internal ColumnShape(VirtualShape virtualShape) : base(virtualShape) { }

        protected override void GenerateVisualContent(Painter2D painter)
        {
            int columnIndex = (int)ColumnDraw.Tag;
            bool isTransparentColumn =
                ((ColumnDraw.TotalColumns - columnIndex) % 2) != 0;

            Color color = (isTransparentColumn) ?
                UnityStyles.Colors.BranchExplorer.ControlBackgroundColor :
                UnityStyles.Colors.BranchExplorer.ColumnBackgroundColor;

            painter.fillColor = color;

            painter.DrawRect(new Rect(0, 0,
                ColumnDraw.Bounds.Width, ColumnDraw.Bounds.Height));

            painter.Fill();
        }
    }
}
