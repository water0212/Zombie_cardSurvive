using System;
using UnityEngine;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    /// <summary>只讀取 Runtime 快照；不提交付款、移除殭屍、揭露卡牌或觸發通知。</summary>
    public static class RoundEndPreviewService
    {
        public static RoundEndPreview Capture(int roundIndex)
        {
            RoundEndPreview preview = new RoundEndPreview
            {
                RoundIndex = roundIndex,
                MoraleBefore = MoraleSystem.CurrentMorale,
                MaximumMoraleBefore = MoraleSystem.MaximumMorale,
                StageBefore = MoraleSystem.CollapseStage,
                FoodBefore = FoodSystem.Food,
                ResourceBefore = ResourceSystem.GetResource(),
                CombatBefore = CombatSystem.PendingDamage
            };

            foreach (ZombieThreatRuntime threat in CombatSystem.ZombieThreats)
            {
                if (threat == null) continue;
                if (threat.IsMarkedForKill)
                {
                    preview.MarkedKillCount++;
                    continue;
                }
                if (!threat.CanResolveConsequencesAtRoundEnd(roundIndex))
                {
                    preview.DeferredZombieCount++;
                    continue;
                }

                preview.EligibleZombieCount++;
                preview.ZombieMoraleDamage = AddClamped(preview.ZombieMoraleDamage, Mathf.Max(0, threat.Attack));
                preview.PermanentMaximumMoraleDamage = AddClamped(
                    preview.PermanentMaximumMoraleDamage,
                    Mathf.Max(0, threat.PermanentMaximumMoraleDamage));
            }

            ValueConsumptionResult resource = MoralePyramidSystem.PreviewRoundEndResourceLoss(roundIndex);
            preview.ResourceLoss = resource.Consumed;
            preview.ResourceAfter = Math.Max(0, preview.ResourceBefore - resource.Consumed);

            preview.CombatAfter = CombatSystem.PreviewCombatAfterOvercapDecay();
            preview.CombatOvercapLoss = Math.Max(0, preview.CombatBefore - preview.CombatAfter);

            preview.FoodDemand = FoodPlanSystem.GetRoundFoodConsumption();
            RoundFoodConsumptionResult food = FoodSystem.PreviewRoundFoodConsumption(preview.FoodDemand);
            preview.FoodShortage = food.Shortage;
            preview.TemporaryFoodConsumed = food.TemporaryConsumed;
            preview.PermanentFoodConsumed = food.PermanentConsumed;
            preview.FoodAfter = food.PermanentAfter;
            preview.ShortageMoraleLoss = FoodPlanSystem.GetShortageMoraleLoss(food.Shortage);

            int moraleDelta = -preview.ZombieMoraleDamage - preview.ShortageMoraleLoss;
            int maximumDelta = -preview.PermanentMaximumMoraleDamage;
            ResolveEncouragingMeal(preview, ref moraleDelta, maximumDelta);

            preview.HighMoraleDrift = MoraleSystem.PreviewRoundEndDrift(moraleDelta, maximumDelta);
            moraleDelta = AddSignedClamped(moraleDelta, preview.HighMoraleDrift);
            MoraleChangeResult morale = MoraleSystem.PreviewChange("RoundEndPreview", moraleDelta, maximumDelta);
            preview.MoraleAfter = morale.MoraleAfter;
            preview.MaximumMoraleAfter = morale.MaximumMoraleAfter;
            preview.StageAfter = morale.StageAfter;
            return preview;
        }

        private static void ResolveEncouragingMeal(
            RoundEndPreview preview,
            ref int moraleDelta,
            int maximumDelta)
        {
            if (!FoodPlanSystem.IsEncouragingMealActive)
            {
                preview.EncouragingMealFailureReason = EncouragingMealFailureReason.NotSelected;
                return;
            }

            preview.EncouragingMealAttempted = true;
            preview.EncouragingMealExtraFoodCost = FoodPlanSystem.GetEncouragingExtraFoodCost(
                preview.FoodDemand,
                EnergySystem.MaxEnergy);
            MoraleChangeResult beforeMeal = MoraleSystem.PreviewChange("MealPreview", moraleDelta, maximumDelta);

            int availablePermanentAfterDemand = Math.Max(0, FoodSystem.AvailableFood - preview.PermanentFoodConsumed);
            preview.EncouragingMealFailureReason = FoodPlanSystem.EvaluateEncouragingMeal(
                preview.FoodShortage,
                beforeMeal.MoraleAfter,
                beforeMeal.MaximumMoraleAfter,
                preview.EncouragingMealExtraFoodCost,
                availablePermanentAfterDemand);
            if (preview.EncouragingMealFailureReason != EncouragingMealFailureReason.None)
            {
                return;
            }

            preview.EncouragingMealSucceeded = true;
            preview.EncouragingMealMoraleGain = FoodPlanSystem.EncouragingMoraleGain;
            preview.FoodAfter = Math.Max(0, preview.FoodAfter - preview.EncouragingMealExtraFoodCost);
            moraleDelta = AddSignedClamped(moraleDelta, preview.EncouragingMealMoraleGain);
        }

        private static int AddClamped(int left, int right)
        {
            return (int)Math.Min(int.MaxValue, (long)Math.Max(0, left) + Math.Max(0, right));
        }

        private static int AddSignedClamped(int left, int right)
        {
            long value = (long)left + right;
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value));
        }
    }
}
