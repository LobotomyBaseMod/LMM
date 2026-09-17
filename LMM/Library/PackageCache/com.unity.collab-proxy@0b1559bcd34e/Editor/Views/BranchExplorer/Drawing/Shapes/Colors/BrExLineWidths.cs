using Codice.Client.BaseCommands.BranchExplorer;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors
{
    internal static class BrExLineWidths
    {
        internal static class Branch
        {
            internal static float GetBorderWidth()
            {
                return BRANCH_BORDER_THICKNESS;
            }

            internal static float GetHomeGlyphBorderWidth()
            {
                return BRANCH_HOME_BORDER_THICKNESS;
            }

            const float BRANCH_BORDER_THICKNESS = 1.5f;
            const float BRANCH_HOME_BORDER_THICKNESS = 1;
        }

        internal static class Changeset
        {
            internal static float GetHeadBorderWidth()
            {
                return CSET_HEAD_BORDER_THICKNESS;
            }

            internal static float GetOuterBorderWidth(bool isCheckoutChangeset)
            {
                return isCheckoutChangeset ?
                    CSET_CHECKOUT_BORDER_THICKNESS :
                    CSET_BORDER_THICKNESS;
            }

            internal static float GetParentLinkThickness()
            {
                return PARENT_LINK_THICKNESS;
            }

            internal static float GetHomeGlyphBorderWidth()
            {
                return CSET_HOME_BORDER_THICKNESS;
            }

            const float CSET_BORDER_THICKNESS = 3f;
            const float CSET_CHECKOUT_BORDER_THICKNESS = 3f;
            const float CSET_HEAD_BORDER_THICKNESS = 1.5f;
            const float CSET_HOME_BORDER_THICKNESS = 1.6f;
            const float PARENT_LINK_THICKNESS = 2.2f;
        }

        internal static class Label
        {
            internal static float GetWidth()
            {
                return BrExDrawProperties.LabelThickness;
            }
        }

        internal static class MergeLink
        {
            internal static float GetWidth()
            {
                return MERGE_LINK_THICKNESS;
            }

            internal static float GetIntervalHighlightWidth()
            {
                return INTERVAL_MERGE_PARENT_LINK_HIGHLIGHT_THICKNESS;
            }

            const float MERGE_LINK_THICKNESS = 2.2f;
            const float INTERVAL_MERGE_PARENT_LINK_HIGHLIGHT_THICKNESS = 7;
        }
    }
}
