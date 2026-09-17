namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Drawing.Shapes.Colors
{
    internal static class BrExDashes
    {
        internal static float[] GetParentLinkDashPattern(bool isRelevant)
        {
            return isRelevant ?
                RelevantParentLink : null;
        }

        internal static float[] GetMergeLinkDashPattern(bool isPendingMergeLink)
        {
            return isPendingMergeLink ?
                PendingMergeLink : null;
        }

        internal static float[] GetChangesetDashPattern(bool isCheckoutChangeset)
        {
            return isCheckoutChangeset ?
                CheckoutChangeset : null;
        }

        static float[] RelevantParentLink = new float[] { 7, 2 };
        static float[] PendingMergeLink = new float[] { 4, 5 };
        static float[] CheckoutChangeset = new float[] { 9, 3 };
    }
}
