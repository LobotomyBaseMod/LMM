using System;
using System.Collections.Generic;
using Codice.CM.Common;
using UnityEngine.UIElements;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Options.ConditionalFormat
{
    internal class FiltersAndConditionalFormatPanel : VisualElement
    {
        internal FiltersAndConditionalFormatPanel(
            WorkspaceInfo wkInfo,
            Func<BranchExplorerOptionsWindow.IBranchExplorerView> getBrExView)
        {
            mWkInfo = wkInfo;
            mGetBrExView = getBrExView;
            mFormatRuleMenu = new FormatRuleMenu(this);

            CreateGUI();
        }

        internal void SetWorkspaceUIConfiguration(
            WorkspaceUIConfiguration workspaceUIConfiguration)
        {
            mConfig = workspaceUIConfiguration;
        }

        internal void LoadConfigRules()
        {
            mRulesContainer.Clear();

            foreach (Rule rule in mConfig.Rules)
            {
                RulePanel rulePanel = CreateRulePanel(rule);
                rulePanel.CreateView(mRulesContainer);
                mRulesContainer.Add(rulePanel);
            }
        }

        internal void CreateNewRule(Rule.Type type, Rule.FormattedObject formatTarget)
        {
            RulePanel newRule = CreateRulePanel(type, formatTarget);
            newRule.CreateView(mRulesContainer);
            mRulesContainer.Add(newRule);
            SaveAndRefresh();
        }

        internal void Dispose()
        {
            List<RulePanel> rulePanels = GetRulePanels();
            foreach (RulePanel rulePanel in rulePanels)
                rulePanel.Dispose();
        }

        void OnAddInclusionClicked()
        {
            CreateNewRule(Rule.Type.InclusionRule, Rule.FormattedObject.Branch);
        }

        void OnAddExclusionClicked()
        {
            CreateNewRule(Rule.Type.ExclusionRule, Rule.FormattedObject.Branch);
        }

        void OnAddFormatClicked()
        {
            mFormatRuleMenu.Popup();
        }

        RulePanel CreateRulePanel(
            Rule.Type type,
            Rule.FormattedObject formatTarget)
        {
            return new RulePanel(
                type,
                formatTarget,
                SaveAndRefresh,
                SaveAndRedraw);
        }

        RulePanel CreateRulePanel(Rule rule)
        {
            return new RulePanel(
                rule,
                SaveAndRefresh,
                SaveAndRedraw);
        }

        void SaveAndRefresh()
        {
            SaveFilterRules();
            mConfig.Save(mWkInfo);
            mGetBrExView()?.Refresh();
        }

        void SaveAndRedraw()
        {
            SaveFilterRules();
            mConfig.Save(mWkInfo);
            mGetBrExView()?.Redraw();
        }

        void SaveFilterRules()
        {
            List<Rule> rules = new List<Rule>();
            List<RulePanel> rulePanels = GetRulePanels();

            foreach (RulePanel rulePanel in rulePanels)
            {
                Rule rule = rulePanel.GetRule();
                rules.Add(rule);
            }

            mConfig.Rules = rules;
        }

        List<RulePanel> GetRulePanels()
        {
            List<RulePanel> result = new List<RulePanel>();
            foreach (VisualElement child in mRulesContainer.Children())
            {
                if (child is RulePanel rulePanel)
                    result.Add(rulePanel);
            }
            return result;
        }

        void CreateGUI()
        {
            style.flexGrow = 1;
            style.paddingLeft = 8;
            style.paddingTop = 8;
            style.paddingRight = 8;
            style.paddingBottom = 8;

            VisualElement buttonsArea = CreateButtonsArea();
            Add(buttonsArea);

            VisualElement rulesArea = CreateRulesArea();
            Add(rulesArea);
        }

        VisualElement CreateButtonsArea()
        {
            VisualElement buttonsArea = new VisualElement();
            buttonsArea.style.flexDirection = FlexDirection.Row;
            buttonsArea.style.alignItems = Align.Center;
            buttonsArea.style.marginBottom = 8;

            VisualElement centerButtons = new VisualElement();
            centerButtons.style.flexGrow = 1;
            centerButtons.style.flexDirection = FlexDirection.Row;
            centerButtons.style.justifyContent = Justify.Center;

            Button addInclusionButton = ControlBuilder.Button.CreateButton(
                PlasticLocalization.Name.AddInclusion.GetString(),
                OnAddInclusionClicked);
            addInclusionButton.style.marginRight = 4;
            centerButtons.Add(addInclusionButton);

            Button addExclusionButton = ControlBuilder.Button.CreateButton(
                PlasticLocalization.Name.AddExclusion.GetString(),
                OnAddExclusionClicked);
            addExclusionButton.style.marginRight = 4;
            centerButtons.Add(addExclusionButton);

            Button addFormatButton = ControlBuilder.Button.CreateDropDownButton(
                PlasticLocalization.Name.AddFormat.GetString(),
                OnAddFormatClicked);
            centerButtons.Add(addFormatButton);

            buttonsArea.Add(centerButtons);

            return buttonsArea;
        }

        VisualElement CreateRulesArea()
        {
            VisualElement rulesArea = new VisualElement();
            rulesArea.style.flexGrow = 1;

            ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.backgroundColor = new StyleColor(
                UnityStyles.Colors.TreeViewBackground);

            mRulesContainer = new VisualElement();
            scrollView.Add(mRulesContainer);

            rulesArea.Add(scrollView);

            return rulesArea;
        }

        VisualElement mRulesContainer;
        WorkspaceUIConfiguration mConfig;

        readonly FormatRuleMenu mFormatRuleMenu;
        readonly WorkspaceInfo mWkInfo;
        readonly Func<BranchExplorerOptionsWindow.IBranchExplorerView> mGetBrExView;
    }
}
