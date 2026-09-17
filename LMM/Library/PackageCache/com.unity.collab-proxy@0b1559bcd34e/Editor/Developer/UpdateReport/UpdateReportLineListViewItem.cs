using UnityEditor.IMGUI.Controls;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace Unity.PlasticSCM.Editor.Developer.UpdateReport
{
    internal class UpdateReportLineListViewItem : TreeViewItem
    {
        internal ReportLine ReportLine { get; private set; }

        internal UpdateReportLineListViewItem(int id, ReportLine reportLine)
            : base(id, 0)
        {
            ReportLine = reportLine;

            displayName = reportLine.ItemPath;
        }
    }
}
