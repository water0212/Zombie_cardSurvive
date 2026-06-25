using System;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    public class RoundEndResolver : MonoBehaviour
    {
        [Header("Morale")]
        [SerializeField] private int startingMorale = 70;
        [SerializeField] private int defaultMorale = 70;
        [SerializeField] private int maxMorale = 100;
        [SerializeField] private int highMoraleDecayPerRound = 5;

        [Header("Food")]
        [SerializeField] private int foodStorageCapacity = 50;
        [SerializeField] private int baseRoundFoodConsumption = 5;
        [SerializeField] private int shortageMoralePenaltyPerFood = 5;

        [Header("Food Plan")]
        [SerializeField] private FoodPlanLevel startingFoodPlan = FoodPlanLevel.Normal;
        [SerializeField] private int tightPlanFoodDelta = -2;
        [SerializeField] private int generousPlanFoodDelta = 2;
        [SerializeField] private int tightPlanMoraleDelta = -5;
        [SerializeField] private int generousPlanMoraleDelta = 5;

        [Header("Combat")]
        [SerializeField] private int maxCombatValue = 9;
        [Range(0, 100)]
        [SerializeField] private int combatOvercapLossPercent = 50;

        public event Action<RoundEndReport> RoundEndResolved;

        public RoundEndReport LastReport { get; private set; }
        public int FoodStorageCapacity => foodStorageCapacity;
        public int BaseRoundFoodConsumption => baseRoundFoodConsumption;

        public void PrepareRun()
        {
            FoodSystem.SetStorageCapacity(foodStorageCapacity);
            MoraleSystem.Reset(startingMorale, defaultMorale, maxMorale, highMoraleDecayPerRound);
            FoodPlanSystem.Reset(
                startingFoodPlan,
                baseRoundFoodConsumption,
                tightPlanFoodDelta,
                generousPlanFoodDelta,
                tightPlanMoraleDelta,
                generousPlanMoraleDelta,
                shortageMoralePenaltyPerFood);
            CombatSystem.Configure(maxCombatValue, combatOvercapLossPercent);
        }

        public RoundEndReport ResolveRoundEnd(CardController cardController, int roundIndex, int turnIndex)
        {
            RoundEndReport report = new RoundEndReport
            {
                RoundIndex = roundIndex,
                TurnIndex = turnIndex,
                FoodBefore = FoodSystem.GetFood(),
                StorageCapacity = FoodSystem.StorageCapacity,
                MoraleBefore = MoraleSystem.GetMorale(),
                FoodPlanBefore = FoodPlanSystem.CurrentPlan.ToString()
            };

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
            report.ZombieMoraleDamage = CombatSystem.GetSurvivingZombieAttackPower();
            if (report.ZombieMoraleDamage > 0)
            {
                MoraleSystem.ReduceMorale(report.ZombieMoraleDamage);
            }

            report.CombatOvercapLoss = CombatSystem.ApplyOvercapDecay();
            report.CombatValueAfterDecay = CombatSystem.PendingDamage;

            report.FoodConsumption = FoodPlanSystem.GetRoundFoodConsumption();
            report.FoodShortage = FoodSystem.ConsumeFoodAllowShortage(report.FoodConsumption);
            report.FoodAfterConsumption = FoodSystem.GetFood();

            report.ShortageMoraleLoss = report.FoodShortage * FoodPlanSystem.ShortageMoralePenaltyPerFood;
            if (report.ShortageMoraleLoss > 0)
            {
                MoraleSystem.ReduceMorale(report.ShortageMoraleLoss);
            }

            report.FoodPlanMoraleDelta = FoodPlanSystem.GetRoundMoraleDelta();
            MoraleSystem.ChangeMorale(report.FoodPlanMoraleDelta);

            report.HighMoraleDrift = MoraleSystem.ApplyRoundEndDrift();
            report.FoodTrimmedByStorage = FoodSystem.TrimToStorageCapacity();
            report.PendingFoodPlanApplied = FoodPlanSystem.ApplyPendingPlan();
            report.FoodPlanAfter = FoodPlanSystem.CurrentPlan.ToString();
            report.MoraleAfter = MoraleSystem.GetMorale();

            LastReport = report;
            RoundEndResolved?.Invoke(report);
            Debug.Log(report.BuildSummary());
            return report;
        }
    }
}
