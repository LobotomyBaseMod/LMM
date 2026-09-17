using CodiceApp;
using Codice.CM.Common;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using PlasticGui;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using PlasticGui.WorkspaceWindow.Configuration;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Options.ConditionalFormat
{
    internal class RuleData
    {
        internal Rule.Type Type { get { return mType; } }
        internal Rule.FormattedObject FormatTarget { get { return mFormatTarget; } }
        internal bool IsEnabled { get { return mIsEnabled; } }
        internal string Description { get { return mDescription; } set { mDescription = value; } }
        internal string Condition { get { return mCondition; } set { mCondition = value; } }
        internal RelatedBranchFlags Options { get { return mOptions; } set { mOptions = value; } }

        internal RuleData(Rule.Type type, Rule.FormattedObject formatTarget)
        {
            mType = type;
            mFormatTarget = formatTarget;

            mIsEnabled = true;
            mDescription = ConditionalFormatRuleText.ForDescription(mType, null);
            mCondition = DefaultConditionalFormatConditions
                .GetDefaultCondition(mType, mFormatTarget);
            mOptions = RelatedBranchFlags.None;
            mColor = null;
        }

        internal RuleData(Rule rule)
        {
            mType = rule.RuleType;
            mIsEnabled = rule.Enabled;
            mFormatTarget = rule.FormatTarget;
            mDescription = ConditionalFormatRuleText
                .ForDescription(mType, rule.Description);
            mCondition = rule.Condition ?? string.Empty;
            mColor = rule.Color;
            mOptions = rule.Options;
        }

        internal ColorRGB GetColor()
        {
            if (mType == Rule.Type.InclusionRule || mType == Rule.Type.ExclusionRule)
                return new ColorRGB(1, 1, 1, 1);

            return mColor ?? DefaultColorProvider.GetDefaultColor();
        }

        internal BrExLayoutFilter GetFilter(WorkspaceInfo wkInfo, RepositorySpec repSpec)
        {
            return new BrExLayoutFilter(
                PlasticGui.Plastic.API.FindGuids(repSpec, wkInfo, Condition, ObjectType.Branch),
                mOptions);
        }

        internal bool HasDefaultCondition()
        {
            return DefaultConditionalFormatConditions.IsDefaultCondition(mCondition);
        }

        readonly Rule.Type mType;
        readonly Rule.FormattedObject mFormatTarget;

        string mDescription;
        string mCondition;
        RelatedBranchFlags mOptions;

        readonly bool mIsEnabled;
        readonly ColorRGB mColor;
    }
}
