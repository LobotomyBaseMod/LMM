using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor
{
    internal static class RecommendToEnableManualCheckout
    {
        internal static bool IsAlreadyRecommended()
        {
            return BoolSetting.Load(
                UnityConstants.IS_MANUAL_CHECKOUT_ALREADY_RECOMMENDED_KEY_NAME,
                false);
        }

        internal static void SetAlreadyRecommended(bool value)
        {
            BoolSetting.Save(value,
                UnityConstants.IS_MANUAL_CHECKOUT_ALREADY_RECOMMENDED_KEY_NAME);
        }

        internal static void IfHasLockRulesFor(RepositorySpec repSpec)
        {
            if (UVCSAssetModificationProcessor.IsManualCheckoutEnabled ||
                IsAlreadyRecommended() ||
                !IsTimeToRecommend())
                return;

            LockRule lockRule = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(50);
            waiter.Execute(
                /*threadOperationDelegate*/
                delegate
                {
                    lockRule = PlasticGui.Plastic.API.GetLockRule(repSpec);
                },
                /*afterOperationDelegate*/
                delegate
                {
                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.LogException(
                            "RecommendToEnableManualCheckout",
                            waiter.Exception);
                        return;
                    }

                    if (lockRule == null || lockRule.Rules.Length == 0)
                        return;

                    if (GuiMessage.ShowQuestion(
                            PlasticLocalization.Name.EnableManualCheckout.GetString(),
                            PlasticLocalization.Name.RecommendToEnableManualCheckoutSinceLockRulesAreConfigured.GetString(),
                            PlasticLocalization.Name.EnableButton.GetString()))
                    {
                        UVCSAssetModificationProcessor.ToggleManualCheckoutPreference(repSpec);
                    }

                    SetAlreadyRecommended(true);
                });
        }

        static bool IsTimeToRecommend()
        {
            return ProjectLoadedCounter.Get() >= MIN_NUMBER_PROJECT_LOAD_TO_RECOMMEND;
        }

        const int MIN_NUMBER_PROJECT_LOAD_TO_RECOMMEND = 2;
    }
}
