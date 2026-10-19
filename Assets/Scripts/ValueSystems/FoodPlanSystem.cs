using System;
using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class FoodPlanSystem
    {
        public const int DefaultBaseRoundFoodDemand = 5;
        public const int DefaultShortageMoralePenaltyPerFood = 3;
        public const int DefaultShortageMoraleLossCap = 15;
        public const float DefaultEncouragingDemandMultiplier = 1.2f;
        public const int DefaultEncouragingMoraleGain = 5;

        private static FoodPlanLevel currentPlan = FoodPlanLevel.Normal;
        private static FoodPlanLevel pendingPlan = FoodPlanLevel.Normal;
        private static bool hasPendingPlan;
        private static bool hasChangedThisTurn;

        private static int baseRoundFoodDemand = DefaultBaseRoundFoodDemand;
        private static int shortageMoralePenaltyPerFood = DefaultShortageMoralePenaltyPerFood;
        private static int shortageMoraleLossCap = DefaultShortageMoraleLossCap;
        private static float encouragingDemandMultiplier = DefaultEncouragingDemandMultiplier;
        private static int encouragingMoraleGain = DefaultEncouragingMoraleGain;
        private static int temporaryRoundConsumptionModifier;

        public static FoodPlanLevel CurrentPlan => currentPlan;
        public static FoodPlanLevel PendingPlan => pendingPlan;
        public static bool HasPendingPlan => hasPendingPlan;
        public static bool HasChangedThisTurn => hasChangedThisTurn;
        public static int BaseRoundFoodDemand => baseRoundFoodDemand;
        public static int BaseRoundConsumption => BaseRoundFoodDemand;
        public static int TemporaryRoundConsumptionModifier => temporaryRoundConsumptionModifier;
        public static int ShortageMoralePenaltyPerFood => shortageMoralePenaltyPerFood;
        public static int ShortageMoraleLossCap => shortageMoraleLossCap;
        public static float EncouragingDemandMultiplier => encouragingDemandMultiplier;
        public static int EncouragingMoraleGain => encouragingMoraleGain;
        public static bool IsEncouragingMealActive => currentPlan == FoodPlanLevel.Generous;

        public static void Reset(
            FoodPlanLevel startingPlan = FoodPlanLevel.Normal,
            int baseConsumption = DefaultBaseRoundFoodDemand,
            int shortagePenaltyPerFood = DefaultShortageMoralePenaltyPerFood,
            int shortageLossCap = DefaultShortageMoraleLossCap,
            float encouragingMultiplier = DefaultEncouragingDemandMultiplier,
            int encouragingGain = DefaultEncouragingMoraleGain)
        {
            currentPlan = NormalizePlan(startingPlan);
            pendingPlan = currentPlan;
            hasPendingPlan = false;
            hasChangedThisTurn = false;

            baseRoundFoodDemand = Mathf.Max(0, baseConsumption);
            shortageMoralePenaltyPerFood = Mathf.Max(0, shortagePenaltyPerFood);
            shortageMoraleLossCap = Mathf.Max(0, shortageLossCap);
            encouragingDemandMultiplier = Mathf.Max(1f, encouragingMultiplier);
            encouragingMoraleGain = Mathf.Max(0, encouragingGain);
            temporaryRoundConsumptionModifier = 0;
        }

        public static void BeginTurn()
        {
            hasChangedThisTurn = false;
        }

        public static bool TrySetPendingPlan(FoodPlanLevel plan)
        {
            if (hasChangedThisTurn)
            {
                return false;
            }
            if (NormalizePlan(plan) == currentPlan)
            {
                return false;
            }

            pendingPlan = NormalizePlan(plan);
            hasPendingPlan = pendingPlan != currentPlan;
            hasChangedThisTurn = true;
            return true;
        }

        public static void ForceSetPendingPlan(FoodPlanLevel plan)
        {
            pendingPlan = NormalizePlan(plan);
            hasPendingPlan = pendingPlan != currentPlan;
        }

        public static bool ApplyPendingPlan()
        {
            if (!hasPendingPlan)
            {
                return false;
            }

            currentPlan = pendingPlan;
            hasPendingPlan = false;
            return true;
        }

        public static int GetRoundFoodConsumption()
        {
            return Mathf.Max(0, baseRoundFoodDemand + temporaryRoundConsumptionModifier);
        }

        public static int GetPendingRoundFoodConsumption()
        {
            return Mathf.Max(0, baseRoundFoodDemand + temporaryRoundConsumptionModifier);
        }

        public static void AddTemporaryRoundConsumptionModifier(int amount)
        {
            temporaryRoundConsumptionModifier += amount;
        }

        public static void ClearTemporaryRoundConsumptionModifiers()
        {
            temporaryRoundConsumptionModifier = 0;
        }

        public static int GetShortageMoraleLoss(int shortage)
        {
            long requested = (long)Mathf.Max(0, shortage) * shortageMoralePenaltyPerFood;
            return (int)Math.Min(shortageMoraleLossCap, Math.Min(int.MaxValue, requested));
        }

        public static int GetEncouragingTotalFoodRequirement(int fixedDemand, int maxEnergy)
        {
            int demand = Mathf.Max(0, fixedDemand);
            int scaledDemand = Mathf.CeilToInt(demand * encouragingDemandMultiplier);
            long total = (long)scaledDemand + Mathf.Max(0, maxEnergy);
            return (int)Math.Min(int.MaxValue, total);
        }

        public static int GetEncouragingExtraFoodCost(int fixedDemand, int maxEnergy)
        {
            int demand = Mathf.Max(0, fixedDemand);
            return Mathf.Max(0, GetEncouragingTotalFoodRequirement(demand, maxEnergy) - demand);
        }

        public static string GetDisplayName(FoodPlanLevel plan)
        {
            return NormalizePlan(plan) == FoodPlanLevel.Generous ? "鼓舞餐點" : "標準供應";
        }

        private static FoodPlanLevel NormalizePlan(FoodPlanLevel plan)
        {
            return plan == FoodPlanLevel.Generous
                ? FoodPlanLevel.Generous
                : FoodPlanLevel.Normal;
        }
    }
}
