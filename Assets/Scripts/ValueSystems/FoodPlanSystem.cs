using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class FoodPlanSystem
    {
        private static FoodPlanLevel currentPlan = FoodPlanLevel.Normal;
        private static FoodPlanLevel pendingPlan = FoodPlanLevel.Normal;
        private static bool hasPendingPlan;
        private static bool hasChangedThisTurn;

        private static int baseRoundConsumption = 5;
        private static int tightConsumptionDelta = -2;
        private static int generousConsumptionDelta = 2;
        private static int tightMoraleDelta = -5;
        private static int generousMoraleDelta = 5;
        private static int shortageMoralePenaltyPerFood = 5;

        public static FoodPlanLevel CurrentPlan => currentPlan;
        public static FoodPlanLevel PendingPlan => pendingPlan;
        public static bool HasPendingPlan => hasPendingPlan;
        public static bool HasChangedThisTurn => hasChangedThisTurn;
        public static int BaseRoundConsumption => baseRoundConsumption;
        public static int ShortageMoralePenaltyPerFood => shortageMoralePenaltyPerFood;

        public static void Reset(
            FoodPlanLevel startingPlan = FoodPlanLevel.Normal,
            int baseConsumption = 5,
            int tightFoodDelta = -2,
            int generousFoodDelta = 2,
            int tightMorale = -5,
            int generousMorale = 5,
            int shortagePenaltyPerFood = 5)
        {
            currentPlan = startingPlan;
            pendingPlan = startingPlan;
            hasPendingPlan = false;
            hasChangedThisTurn = false;

            baseRoundConsumption = Mathf.Max(0, baseConsumption);
            tightConsumptionDelta = tightFoodDelta;
            generousConsumptionDelta = generousFoodDelta;
            tightMoraleDelta = tightMorale;
            generousMoraleDelta = generousMorale;
            shortageMoralePenaltyPerFood = Mathf.Max(0, shortagePenaltyPerFood);
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

            pendingPlan = plan;
            hasPendingPlan = pendingPlan != currentPlan;
            hasChangedThisTurn = true;
            return true;
        }

        public static void ForceSetPendingPlan(FoodPlanLevel plan)
        {
            pendingPlan = plan;
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
            return Mathf.Max(0, baseRoundConsumption + GetConsumptionDelta(currentPlan));
        }

        public static int GetPendingRoundFoodConsumption()
        {
            return Mathf.Max(0, baseRoundConsumption + GetConsumptionDelta(pendingPlan));
        }

        public static int GetRoundMoraleDelta()
        {
            return GetMoraleDelta(currentPlan);
        }

        private static int GetConsumptionDelta(FoodPlanLevel plan)
        {
            switch (plan)
            {
                case FoodPlanLevel.Tight:
                    return tightConsumptionDelta;
                case FoodPlanLevel.Generous:
                    return generousConsumptionDelta;
                default:
                    return 0;
            }
        }

        private static int GetMoraleDelta(FoodPlanLevel plan)
        {
            switch (plan)
            {
                case FoodPlanLevel.Tight:
                    return tightMoraleDelta;
                case FoodPlanLevel.Generous:
                    return generousMoraleDelta;
                default:
                    return 0;
            }
        }
    }
}
