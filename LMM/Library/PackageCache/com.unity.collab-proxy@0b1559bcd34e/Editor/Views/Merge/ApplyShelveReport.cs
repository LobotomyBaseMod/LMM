using System.Collections.Generic;

using UnityEditor;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Merge;
using Unity.PlasticSCM.Editor.UI.Errors;

namespace Unity.PlasticSCM.Editor.Views.Merge
{
    internal class ApplyShelveReport : IApplyShelveReport
    {
        internal ApplyShelveReport(EditorWindow window)
        {
            mWindow = window;
        }

        void IApplyShelveReport.Show(WorkspaceInfo wkInfo, List<ItemError> errors)
        {
            ErrorsDialog.ShowDialog(
                PlasticLocalization.Name.ApplyShelveReportTitle.GetString(),
                PlasticLocalization.Name.ApplyShelveReportExplanation.GetString(),
                GetErrorMessagesFromItemErrors(errors),
                mWindow);
        }

        static List<ErrorMessage> GetErrorMessagesFromItemErrors(List<ItemError> errors)
        {
            List<ErrorMessage> result = new List<ErrorMessage>(errors.Count);

            foreach (ItemError error in errors)
            {
                result.Add(new ErrorMessage(error.Path, error.Error.Message));
            }

            return result;
        }

        readonly EditorWindow mWindow;
    }
}
