using System;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    public class RoundEndResolver : MonoBehaviour
    {
        private const int MoraleSettlementConfigVersion = 2;

        [SerializeField, HideInInspector] private int moraleSettlementConfigVersion;

        [Header("Morale")]
        [SerializeField] private int defaultMorale = 70;
        [SerializeField] private int highMoraleDecayPerRound =
            MoraleSystem.DefaultHighMoraleDecayPerRound;

        [Header("Food")]
        [SerializeField] private int foodStorageCapacity = 50;
        [SerializeField] private int baseRoundFoodConsumption =
            FoodPlanSystem.DefaultBaseRoundFoodDemand;
        [SerializeField] private int shortageMoralePenaltyPerFood =
            FoodPlanSystem.DefaultShortageMoralePenaltyPerFood;
        [SerializeField] private int shortageMoraleLossCap =
            FoodPlanSystem.DefaultShortageMoraleLossCap;

        [Header("Food Plan")]
        [SerializeField] private FoodPlanLevel startingFoodPlan = FoodPlanLevel.Normal;
        [SerializeField, Min(1f)] private float encouragingDemandMultiplier =
            FoodPlanSystem.DefaultEncouragingDemandMultiplier;
        [SerializeField, Min(0)] private int encouragingMoraleGain =
            FoodPlanSystem.DefaultEncouragingMoraleGain;

        [Header("Combat")]
        [SerializeField] private int maxCombatValue = 9;
        [Range(0, 100)]
        [SerializeField] private int combatOvercapLossPercent = 50;

        public event Action<RoundEndReport> RoundEndResolved;

        public RoundEndReport LastReport { get; private set; }
        public int FoodStorageCapacity => foodStorageCapacity;
        public int BaseRoundFoodConsumption => baseRoundFoodConsumption;

        private void OnValidate()
        {
            MigrateMoraleSettlementConfig();
        }

        public void PrepareRun()
        {
            MigrateMoraleSettlementConfig();
            NextRoundEffectSystem.Reset();
            FoodSystem.SetStorageCapacity(foodStorageCapacity);
            MoraleSystem.Reset(
                MoraleSystem.InitialMorale,
                defaultMorale,
                MoraleSystem.AbsoluteMaximumMorale,
                highMoraleDecayPerRound);
            FoodPlanSystem.Reset(
                startingFoodPlan,
                baseRoundFoodConsumption,
                shortageMoralePenaltyPerFood,
                shortageMoraleLossCap,
                encouragingDemandMultiplier,
                encouragingMoraleGain);
            CombatSystem.Configure(maxCombatValue, combatOvercapLossPercent);
        }

        private void MigrateMoraleSettlementConfig()
        {
            if (moraleSettlementConfigVersion >= MoraleSettlementConfigVersion)
            {
                return;
            }

            // V2 unifies the tuning authority after older scenes serialized conflicting
            // meal/drift values. Later Inspector tuning remains intact once migrated.
            shortageMoralePenaltyPerFood = FoodPlanSystem.DefaultShortageMoralePenaltyPerFood;
            shortageMoraleLossCap = FoodPlanSystem.DefaultShortageMoraleLossCap;
            encouragingDemandMultiplier = FoodPlanSystem.DefaultEncouragingDemandMultiplier;
            encouragingMoraleGain = FoodPlanSystem.DefaultEncouragingMoraleGain;
            highMoraleDecayPerRound = MoraleSystem.DefaultHighMoraleDecayPerRound;
            moraleSettlementConfigVersion = MoraleSettlementConfigVersion;
        }

        public RoundEndReport ResolveRoundEnd(CardController cardController, int roundIndex, int turnIndex)
        {
            RoundEndReport report = new RoundEndReport
            {
                RoundIndex = roundIndex,
                TurnIndex = turnIndex,
                StorageCapacity = FoodSystem.StorageCapacity,
                MoraleBefore = MoraleSystem.GetMorale(),
                MoraleProfileId = MoralePyramidSystem.ActiveProfileId,
                MoraleStageBefore = MoraleSystem.CollapseStage,
                FoodPlanBefore = FoodPlanSystem.GetDisplayName(FoodPlanSystem.CurrentPlan)
            };

            ValueConsumptionResult stageResourceLoss =
                MoralePyramidSystem.ResolveRoundEndResourceLoss(roundIndex);
            report.MoraleStageResourceLossRequested = stageResourceLoss.Requested;
            report.MoraleStageResourceLossApplied = stageResourceLoss.Consumed;

            CombatSystem.BeginRoundEnd(roundIndex);

            if (cardController != null)
            {
                cardController.RevealRemainingAdditiveCardsForRound();
            }

            report.CombatValueBeforeRoundEnd = CombatSystem.PendingDamage;
            report.MaxCombatValue = CombatSystem.MaxCombatValue;
            report.CombatOvercapLossPercent = CombatSystem.OvercapLossPercent;
            report.ZombiesBeforeRoundEnd = CombatSystem.ZombieThreatCount;
            report.ZombiesKilled = CombatSystem.ResolveMarkedZombieKills();
            report.ZombiesSurvived = CombatSystem.ZombieThreatCount;

            MoraleBatchRequest moraleBatch = new MoraleBatchRequest("RoundEnd");
            foreach (ZombieThreatRuntime threat in CombatSystem.ZombieThreats)
            {
                if (threat == null)
                {
                    continue;
                }

                if (!threat.CanResolveConsequencesAtRoundEnd(roundIndex))
                {
                    report.DeferredZombieEffects++;
                    continue;
                }

                report.EligibleZombieEffects++;
                int moraleDamage = Mathf.Max(0, threat.Attack);
                int maximumMoraleDamage = Mathf.Max(0, threat.PermanentMaximumMoraleDamage);
                report.ZombieMoraleDamage += moraleDamage;
                report.ZombiePermanentMaximumMoraleDamage += maximumMoraleDamage;
                moraleBatch.Add(
                    $"Zombie[{threat.RuntimeId}:{threat.DisplayName}]",
                    -moraleDamage,
                    -maximumMoraleDamage);

                if (NextRoundEffectSystem.TryScheduleForRound(
                        threat.SourceCard != null ? threat.SourceCard.SurvivalNextRoundEffect : null,
                        NextRoundEffectSourceKind.Zombie,
                        threat.RuntimeId.ToString(),
                        roundIndex + 1,
                        out ScheduledNextRoundEffect scheduled))
                {
                    report.ZombieNextRoundEffectsScheduled++;
                    report.NextRoundFoodDemandScheduled = AddClamped(
                        report.NextRoundFoodDemandScheduled,
                        scheduled.FoodDemandModifier);
                }
            }

            report.CombatOvercapLoss = CombatSystem.ApplyOvercapDecay();
            report.CombatValueAfterDecay = CombatSystem.PendingDamage;

            report.FoodConsumption = FoodPlanSystem.GetRoundFoodConsumption();
            RoundFoodConsumptionResult foodResult = FoodSystem.ConsumeRoundFoodAllowShortage(report.FoodConsumption);
            FoodPlanSystem.ClearTemporaryRoundConsumptionModifiers();
            report.FoodBefore = foodResult.PermanentBefore;
            report.TemporaryFoodBefore = foodResult.TemporaryBefore;
            report.TemporaryFoodConsumed = foodResult.TemporaryConsumed;
            report.PermanentFoodConsumed = foodResult.PermanentConsumed;
            report.TemporaryFoodDiscarded = foodResult.TemporaryDiscarded;
            report.FoodShortage = foodResult.Shortage;
            report.FoodAfterConsumption = FoodSystem.GetFood();

            report.ShortageMoraleLoss = FoodPlanSystem.GetShortageMoraleLoss(report.FoodShortage);
            moraleBatch.Add("FoodShortage", -report.ShortageMoraleLoss);

            ResolveEncouragingMeal(report, moraleBatch);
            report.FoodPlanMoraleDelta = report.EncouragingMealMoraleGain;
            report.FoodAfterConsumption = FoodSystem.GetFood();

            int moraleBeforeDrift = PreviewMoraleAfter(
                moraleBatch.NetMoraleDelta,
                moraleBatch.NetMaximumMoraleDelta);
            if (moraleBeforeDrift > MoraleSystem.DefaultMorale)
            {
                report.HighMoraleDrift = -Mathf.Min(
                    MoraleSystem.HighMoraleDecayPerRound,
                    moraleBeforeDrift - MoraleSystem.DefaultMorale);
                moraleBatch.Add("HighMoraleDrift", report.HighMoraleDrift);
            }

            report.MoraleSettlement = MoraleSystem.CommitBatch(moraleBatch);
            report.MoraleStageAfter = MoraleSystem.CollapseStage;
            report.FoodTrimmedByStorage = FoodSystem.TrimToStorageCapacity();
            report.PendingFoodPlanApplied = !MoraleSystem.IsCollapsed && FoodPlanSystem.ApplyPendingPlan();
            report.FoodPlanAfter = FoodPlanSystem.GetDisplayName(FoodPlanSystem.CurrentPlan);
            report.MoraleAfter = MoraleSystem.GetMorale();

            LastReport = report;
            RoundEndResolved?.Invoke(report);
            Debug.Log(report.BuildSummary());
            return report;
        }

        private static void ResolveEncouragingMeal(
            RoundEndReport report,
            MoraleBatchRequest moraleBatch)
        {
            if (!FoodPlanSystem.IsEncouragingMealActive)
            {
                return;
            }

            report.EncouragingMealAttempted = true;
            report.EncouragingMealTotalRequirement =
                FoodPlanSystem.GetEncouragingTotalFoodRequirement(
                    report.FoodConsumption,
                    EnergySystem.MaxEnergy);
            report.EncouragingMealExtraFoodCost =
                FoodPlanSystem.GetEncouragingExtraFoodCost(
                    report.FoodConsumption,
                    EnergySystem.MaxEnergy);

            int moraleBeforeMeal = PreviewMoraleAfter(
                moraleBatch.NetMoraleDelta,
                moraleBatch.NetMaximumMoraleDelta);
            int maximumMoraleBeforeMeal = PreviewMaximumMoraleAfter(
                moraleBatch.NetMaximumMoraleDelta);
            if (report.FoodShortage > 0
                || moraleBeforeMeal >= maximumMoraleBeforeMeal
                || report.EncouragingMealExtraFoodCost <= 0
                || !FoodSystem.SpendFood(report.EncouragingMealExtraFoodCost))
            {
                return;
            }

            report.EncouragingMealSucceeded = true;
            report.EncouragingMealExtraFoodPaid = report.EncouragingMealExtraFoodCost;
            report.EncouragingMealMoraleGain = FoodPlanSystem.EncouragingMoraleGain;
            moraleBatch.Add("EncouragingMeal", report.EncouragingMealMoraleGain);
        }

        private static int PreviewMoraleAfter(
            int moraleDelta,
            int maximumMoraleDelta = 0)
        {
            int previewMaximum = PreviewMaximumMoraleAfter(maximumMoraleDelta);
            int clampedCurrent = Math.Min(MoraleSystem.CurrentMorale, previewMaximum);
            long value = (long)clampedCurrent + moraleDelta;
            return (int)Math.Max(0, Math.Min(previewMaximum, value));
        }

        private static int PreviewMaximumMoraleAfter(int maximumMoraleDelta)
        {
            long maximumValue = (long)MoraleSystem.MaximumMorale + maximumMoraleDelta;
            return (int)Math.Max(
                0,
                Math.Min(MoraleSystem.AbsoluteMaximumMorale, maximumValue));
        }

        private static int AddClamped(int left, int right)
        {
            long value = (long)left + right;
            return (int)Math.Max(0, Math.Min(int.MaxValue, value));
        }
    }
}
