using System.Collections.Generic;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    public class RoundEndReport
    {
        public int RoundIndex { get; set; }
        public int TurnIndex { get; set; }
        public int FoodBefore { get; set; }
        public int TemporaryFoodBefore { get; set; }
        public int FoodAfterConsumption { get; set; }
        public int FoodConsumption { get; set; }
        public int TemporaryFoodConsumed { get; set; }
        public int PermanentFoodConsumed { get; set; }
        public int TemporaryFoodDiscarded { get; set; }
        public int FoodShortage { get; set; }
        public int FoodTrimmedByStorage { get; set; }
        public int StorageCapacity { get; set; }
        public int CombatValueBeforeRoundEnd { get; set; }
        public int CombatValueAfterDecay { get; set; }
        public int MaxCombatValue { get; set; }
        public int CombatOvercapLossPercent { get; set; }
        public int CombatOvercapLoss { get; set; }
        public int ZombiesBeforeRoundEnd { get; set; }
        public int ZombiesKilled { get; set; }
        public int ZombiesSurvived { get; set; }
        public int EligibleZombieEffects { get; set; }
        public int DeferredZombieEffects { get; set; }
        public int ZombieMoraleDamage { get; set; }
        public int ZombiePermanentMaximumMoraleDamage { get; set; }
        public int ZombieNextRoundEffectsScheduled { get; set; }
        public int NextRoundFoodDemandScheduled { get; set; }
        public string MoraleProfileId { get; set; }
        public MoraleCollapseStage MoraleStageBefore { get; set; }
        public MoraleCollapseStage MoraleStageAfter { get; set; }
        public int MoraleStageResourceLossRequested { get; set; }
        public int MoraleStageResourceLossApplied { get; set; }
        public int MoraleBefore { get; set; }
        public int MoraleAfter { get; set; }
        public int ShortageMoraleLoss { get; set; }
        public int FoodPlanMoraleDelta { get; set; }
        public int HighMoraleDrift { get; set; }
        public int EncouragingMealTotalRequirement { get; set; }
        public int EncouragingMealExtraFoodCost { get; set; }
        public int EncouragingMealExtraFoodPaid { get; set; }
        public int EncouragingMealMoraleGain { get; set; }
        public bool EncouragingMealAttempted { get; set; }
        public bool EncouragingMealSucceeded { get; set; }
        public bool PendingFoodPlanApplied { get; set; }
        public string FoodPlanBefore { get; set; }
        public string FoodPlanAfter { get; set; }
        public MoraleBatchResult MoraleSettlement { get; set; }

        public string BuildSummary()
        {
            return $"Round {RoundIndex} End\n"
                + $"Combat: {CombatValueBeforeRoundEnd} -> {CombatValueAfterDecay}/{MaxCombatValue}, overcap loss {CombatOvercapLoss} ({CombatOvercapLossPercent}%)\n"
                + $"Zombies: {ZombiesBeforeRoundEnd}, killed {ZombiesKilled}, survived {ZombiesSurvived}, eligible {EligibleZombieEffects}, deferred {DeferredZombieEffects}, morale damage {ZombieMoraleDamage}, permanent max damage {ZombiePermanentMaximumMoraleDamage}\n"
                + $"Zombie next-round effects: scheduled {ZombieNextRoundEffectsScheduled}, next-round food demand +{NextRoundFoodDemandScheduled}\n"
                + $"Morale profile: {MoraleProfileId ?? "none"}, stage {MoraleStageBefore} -> {MoraleStageAfter}, resource loss {MoraleStageResourceLossApplied}/{MoraleStageResourceLossRequested}\n"
                + $"Food: permanent {FoodBefore}, temporary {TemporaryFoodBefore}, demand {FoodConsumption}\n"
                + $"Food paid: temporary {TemporaryFoodConsumed}, permanent {PermanentFoodConsumed}, discarded {TemporaryFoodDiscarded}, remaining {FoodAfterConsumption}, shortage {FoodShortage}, trim {FoodTrimmedByStorage}/{StorageCapacity}\n"
                + $"Morale: {MoraleBefore} -> {MoraleAfter}, shortage loss {ShortageMoraleLoss}, meal {EncouragingMealMoraleGain}, drift {HighMoraleDrift}\n"
                + $"Morale sources: {BuildMoraleSourceSummary()}\n"
                + $"Encouraging meal: total {EncouragingMealTotalRequirement}, extra {EncouragingMealExtraFoodPaid}/{EncouragingMealExtraFoodCost}, success {EncouragingMealSucceeded}\n"
                + $"Food Plan: {FoodPlanBefore} -> {FoodPlanAfter}";
        }

        private string BuildMoraleSourceSummary()
        {
            if (MoraleSettlement == null || MoraleSettlement.Contributions.Count == 0)
            {
                return "none";
            }

            List<string> entries = new List<string>(MoraleSettlement.Contributions.Count);
            foreach (MoraleBatchContribution contribution in MoraleSettlement.Contributions)
            {
                entries.Add(
                    $"{contribution.Source} morale {contribution.MoraleDelta:+#;-#;0}, max {contribution.MaximumMoraleDelta:+#;-#;0}");
            }

            return string.Join(", ", entries);
        }
    }
}
