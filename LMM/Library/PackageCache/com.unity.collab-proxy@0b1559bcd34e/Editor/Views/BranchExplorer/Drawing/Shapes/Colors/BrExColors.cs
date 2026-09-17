using Codice.Client.BaseCommands.BranchExplorer;
using Codice.CM.Common;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors
{
    internal class BrExColors
    {
        internal static class Branch
        {
            internal static Color GetBackgroundColor(
                bool isSelected,
                bool isMultipleSelected,
                bool isCurrentSearchResult,
                bool isSearchResult)
            {
                Color baseColor = GetBaseBackgroundColor(
                    isSelected, isMultipleSelected, isCurrentSearchResult, isSearchResult);

                float alpha = GetBackgroundAlpha(
                    isSelected, isCurrentSearchResult, isSearchResult);

                return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            internal static Color GetBorderColor(
                bool isSelected,
                bool isMultipleSelected,
                bool isCurrentSearchResult)
            {
                if (isCurrentSearchResult)
                    return UnityStyles.Colors.BranchExplorer.BranchDefaultColor;

                if (isSelected)
                {
                    return isMultipleSelected
                        ? UnityStyles.Colors.BranchExplorer.MultipleSelectedObjectsColor
                        : UnityStyles.Colors.BranchExplorer.SingleSelectedObjectColor;
                }

                return UnityStyles.Colors.BranchExplorer.BranchDefaultColor;
            }

            internal static Color GetHomeGlyphFillColor()
            {
                return UnityStyles.Colors.ImageForeground;
            }

            static Color GetBaseBackgroundColor(
                bool isSelected,
                bool isMultipleSelected,
                bool isCurrentSearchResult,
                bool isSearchResult)
            {
                if (isCurrentSearchResult)
                    return UnityStyles.Colors.BranchExplorer.CurrentSearchResultColor;

                if (isSelected)
                {
                    return isMultipleSelected
                        ? UnityStyles.Colors.BranchExplorer.MultipleSelectedObjectsColor
                        : UnityStyles.Colors.BranchExplorer.SingleSelectedObjectColor;
                }

                if (isSearchResult)
                    return UnityStyles.Colors.BranchExplorer.SearchResultColor;

                return UnityStyles.Colors.BranchExplorer.BranchDefaultColor;
            }

            static float GetBackgroundAlpha(
                bool isSelected,
                bool isCurrentSearchResult,
                bool isSearchResult)
            {
                if (isCurrentSearchResult || isSearchResult)
                    return 1;

                if (isSelected)
                    return 0.39f;

                return 0.12f;
            }

            internal static Color GetCaptionBackgroundBrush()
            {
                return UnityStyles.Colors.BranchExplorer.CaptionBackgroundColor;
            }
        }

        internal static class Changeset
        {
            internal static Color GetFillColor(
                bool isSelected,
                bool isMultipleSelected,
                bool isCurrentSearchResult,
                bool isSearchResult)
            {
                if (isCurrentSearchResult)
                    return UnityStyles.Colors.BranchExplorer.CurrentSearchResultColor;

                if (isSelected)
                {
                    return isMultipleSelected
                        ? UnityStyles.Colors.BranchExplorer.MultipleSelectedObjectsColor
                        : UnityStyles.Colors.BranchExplorer.SingleSelectedObjectColor;
                }

                if (isSearchResult)
                    return UnityStyles.Colors.BranchExplorer.SearchResultColor;

                return UnityStyles.Colors.BranchExplorer.ChangesetDefaultColor;
            }
        }

        internal static class Label
        {
            internal static Color GetColor(
                bool isSelected,
                bool isMultipleSelected,
                bool isCurrentSearchResult,
                bool isSearchResult)
            {
                if (isCurrentSearchResult)
                    return UnityStyles.Colors.BranchExplorer.CurrentSearchResultColor;

                if (isSelected)
                {
                    return isMultipleSelected
                        ? UnityStyles.Colors.BranchExplorer.MultipleSelectedObjectsColor
                        : UnityStyles.Colors.BranchExplorer.SingleSelectedObjectColor;
                }

                if (isSearchResult)
                    return UnityStyles.Colors.BranchExplorer.SearchResultColor;

                return UnityStyles.Colors.BranchExplorer.LabelColor;
            }

            internal static Color GetCaptionBorderColor()
            {
                return UnityStyles.Colors.BranchExplorer.CaptionBorderColor;
            }
        }

        internal static class ParentLink
        {
            internal static Color GetLineColor()
            {
                return UnityStyles.Colors.BranchExplorer.ParentLinkColor;
            }
        }

        internal static class MergeLink
        {
            internal static Color GetLineColor(MergeType mergeType, bool isSelected)
            {
                switch (mergeType)
                {
                    case MergeType.Merge:
                        return isSelected ?
                            UnityStyles.Colors.BranchExplorer.MergeLinkSelectedColor :
                            UnityStyles.Colors.BranchExplorer.MergeLinkColor;
                    case MergeType.Cherrypicking:
                    case MergeType.IntervalCherrypick:
                        return isSelected ?
                            UnityStyles.Colors.BranchExplorer.CherryPickLinkSelectedColor :
                            UnityStyles.Colors.BranchExplorer.CherryPickLinkColor;
                    case MergeType.CherrypickSubtractive:
                    case MergeType.IntervalCherrypickSubtractive:
                        return isSelected ?
                            UnityStyles.Colors.BranchExplorer.SubstractiveLinkSelectedColor :
                            UnityStyles.Colors.BranchExplorer.SubstractiveLinkColor;
                }

                return Color.red;
            }
        }
    }
}
