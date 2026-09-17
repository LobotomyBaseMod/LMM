using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.ExplorerTree;
using Codice.CM.Common;
using PlasticGui.WorkspaceWindow.Configuration;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors
{
    internal class ColorProvider
    {
        internal ColorProvider(
            RepositorySpec repSpec,
            WorkspaceInfo wkInfo,
            BrExTree explTree)
        {
            mRepSpec = repSpec;
            mWkInfo = wkInfo;
            mExplorerTree = explTree;
        }

        internal void SetRulesConfiguration(List<Rule> rules)
        {
            if (mExplorerTree == null)
                return;

            FillRulesCache(rules);
        }

        void FillRulesCache(List<Rule> rulesToApply)
        {
            mRuleColors.Clear();

            foreach (Rule rule in rulesToApply)
            {
                if (!rule.Enabled)
                    continue;

                switch (rule.RuleType)
                {
                    case Rule.Type.NonIntegrated:
                        ProcessNonIntegratedRule(rule);
                        break;
                    case Rule.Type.ConditionalFormat:
                        ProcessConditionalFormatRule(rule);
                        break;
                    case Rule.Type.CurrentBranchFormat:
                        ProcessCurrentBranchFormatRule(rule);
                        break;
                    default:
                        break;
                }
            }
        }

        internal Color GetBranchColor(BranchShape brShape, bool isMultipleSelected)
        {
            Color result = Color.red;
            if (brShape.IsSearchResult
                || brShape.IsSelected
                || !mRuleColors.TryGetValue(brShape.BranchDrawInfo.BranchId, out result))
            {
                return BrExColors.Branch.GetBackgroundColor(
                    brShape.IsSelected,
                    isMultipleSelected,
                    brShape.IsCurrentSearchResult,
                    brShape.IsSearchResult);
            }

            return result;
        }

        internal Color GetChangesetColor(ChangesetShape csShape, bool isMultipleSelected)
        {
            Color result;

            if (!csShape.IsSearchResult && !csShape.IsSelected)
            {
                if (mRuleColors.TryGetValue(csShape.ChangesetDraw.Id, out result))
                    return result;
            }

            return BrExColors.Changeset.GetFillColor(
                csShape.IsSelected,
                isMultipleSelected,
                csShape.IsCurrentSearchResult,
                csShape.IsSearchResult);
        }

        internal Color GetLabelColor(LabelShape lbShape, bool isMultipleSelected)
        {
            Color result;
            if (lbShape.IsSearchResult
                || lbShape.IsSelected
                || !TryGetLabelColor(lbShape.LabelDraw.Labels.Select(x => x.Guid), out result))
            {
                return BrExColors.Label.GetColor(
                    lbShape.IsSelected,
                    isMultipleSelected,
                    lbShape.IsCurrentSearchResult,
                    lbShape.IsSearchResult);
            }

            return result;
        }

        void ProcessConditionalFormatRule(Rule rule)
        {
            HashSet<Guid> guids = RuleObjectGuids.GetForRule(rule, mRepSpec, mWkInfo);

            foreach (Guid guid in guids)
                mRuleColors[guid] = GetRuleColor(rule);
        }

        void ProcessCurrentBranchFormatRule(Rule rule)
        {
            BranchInfo brInfo = PlasticGui.Plastic.API.GetWorkingBranch(mWkInfo);

            if (brInfo == null)
                return;

            mRuleColors[brInfo.GUID] = GetRuleColor(rule);
        }

        void ProcessNonIntegratedRule(Rule rule)
        {
            foreach (BrExBranch br in mExplorerTree.GetBranches())
            {
                if (br.ParentId == Guid.Empty)
                    continue;

                long lastOutChangeset = GetLastLinkChangeset(br);
                long lastBranchChangeset = GetLastBranchChangeset(br);

                if (lastOutChangeset < lastBranchChangeset)
                    mRuleColors[br.Guid] = GetRuleColor(rule);
            }
        }

        long GetLastLinkChangeset(BrExBranch br)
        {
            long result = -1;

            if (br.OutgoingLinks == null)
                return result;

            foreach (BrExLink link in br.OutgoingLinks)
            {
                BrExChangeset cs = link.DestinationChangeset;

                if (cs != null && cs.Id > result)
                    result = cs.Id;
            }

            return result;
        }

        long GetLastBranchChangeset(BrExBranch br)
        {
            long result = -1;

            if (br.Changesets == null)
                return result;

            foreach (BrExChangeset cs in br.Changesets)
            {
                if (cs.Id > result)
                    result = cs.Id;
            }

            return result;
        }

        Color GetRuleColor(Rule rule)
        {
            if (rule.FormatTarget == Rule.FormattedObject.Branch)
            {
                return new Color(
                    rule.Color.R,
                    rule.Color.G,
                    rule.Color.B,
                    0.63f);
            }

            return new Color(rule.Color.R, rule.Color.G, rule.Color.B, rule.Color.A);
        }

        bool TryGetLabelColor(IEnumerable<Guid> guids, out Color result)
        {
            foreach (Guid guid in guids)
            {
                if (mRuleColors.TryGetValue(guid, out result))
                    return true;
            }
            result = new Color();
            return false;
        }

        readonly RepositorySpec mRepSpec;
        readonly WorkspaceInfo mWkInfo;
        readonly BrExTree mExplorerTree;
        readonly Dictionary<Guid, Color> mRuleColors = new Dictionary<Guid, Color>();
    }
}
