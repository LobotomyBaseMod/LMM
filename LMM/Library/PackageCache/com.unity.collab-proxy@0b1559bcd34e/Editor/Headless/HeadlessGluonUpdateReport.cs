using System.Collections.Generic;

using Codice.CM.Common;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui.Gluon;
using Unity.PlasticSCM.Editor.Gluon.UpdateReport;

namespace Unity.PlasticSCM.Editor.Headless
{
    internal class HeadlessGluonUpdateReport : IUpdateReport
    {
        void IUpdateReport.AppendReport(string updateReport) { }

        UpdateReportResult IUpdateReport.ShowUpdateReport(
            WorkspaceInfo wkInfo,
            List<ErrorMessage> errors)
        {
            return UpdateReportDialog.ShowUpdateReport(
                wkInfo, errors, null);
        }
    }
}
