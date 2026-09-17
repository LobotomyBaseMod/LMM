using UnityEditor;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Configuration;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Options.ConditionalFormat
{
    internal class FormatRuleMenu
    {
        internal FormatRuleMenu(FiltersAndConditionalFormatPanel view)
        {
            mView = view;
        }

        internal void Popup()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(
                new UnityEngine.GUIContent(PlasticLocalization.GetString(
                    PlasticLocalization.Name.BranchesWithPendingIntegrations)),
                false,
                () => CreateNewRule(Rule.Type.NonIntegrated, Rule.FormattedObject.Branch));

            menu.AddItem(
                new UnityEngine.GUIContent(PlasticLocalization.GetString(
                    PlasticLocalization.Name.ActiveBranch)),
                false,
                () => CreateNewRule(Rule.Type.CurrentBranchFormat, Rule.FormattedObject.Branch));

            menu.AddSeparator(string.Empty);

            menu.AddItem(
                new UnityEngine.GUIContent(PlasticLocalization.GetString(
                    PlasticLocalization.Name.CustomBranchQuery)),
                false,
                () => CreateNewRule(Rule.Type.ConditionalFormat, Rule.FormattedObject.Branch));

            menu.AddItem(
                new UnityEngine.GUIContent(PlasticLocalization.GetString(
                    PlasticLocalization.Name.CustomChangesetQuery)),
                false,
                () => CreateNewRule(Rule.Type.ConditionalFormat, Rule.FormattedObject.Changeset));

            menu.AddItem(
                new UnityEngine.GUIContent(PlasticLocalization.GetString(
                    PlasticLocalization.Name.CustomLabelQuery)),
                false,
                () => CreateNewRule(Rule.Type.ConditionalFormat, Rule.FormattedObject.Label));

            menu.ShowAsContext();
        }

        void CreateNewRule(Rule.Type type, Rule.FormattedObject targetFormat)
        {
            mView.CreateNewRule(type, targetFormat);
        }

        readonly FiltersAndConditionalFormatPanel mView;
    }
}
