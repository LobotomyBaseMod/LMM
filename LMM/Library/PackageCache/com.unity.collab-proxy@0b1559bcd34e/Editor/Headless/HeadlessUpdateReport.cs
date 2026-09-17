using System.Collections;

using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Update;
using Unity.PlasticSCM.Editor.Developer.UpdateReport;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessUpdateReport : IUpdateReport
    {
        void IUpdateReport.Show(WorkspaceInfo wkInfo, IList reportLines)
        {
            UpdateReportDialog.ShowReportDialog(
                wkInfo,
                reportLines,
                null);
        }
    }
}
