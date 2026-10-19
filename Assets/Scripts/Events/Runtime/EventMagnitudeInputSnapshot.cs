using System;

namespace ZombieCardSurvive.Events.Runtime
{
    public readonly struct EventMagnitudeInputSnapshot
    {
        public EventMagnitudeInputSnapshot(
            int day,
            int survivorCount,
            int additionalSurvivorCount,
            int activeBuildingLevelSum,
            int permanentSlotUpgradeCount,
            int otherPermanentDevelopmentModifiers,
            int maxEnergy,
            int foodStorageCapacity,
            int foodStorageCapacityBonus,
            int worldThreatTier = 1,
            int hordeBudgetTier = 1)
        {
            Day = Math.Max(1, day);
            SurvivorCount = Math.Max(1, survivorCount);
            AdditionalSurvivorCount = Math.Max(0, additionalSurvivorCount);
            ActiveBuildingLevelSum = Math.Max(0, activeBuildingLevelSum);
            PermanentSlotUpgradeCount = Math.Max(0, permanentSlotUpgradeCount);
            OtherPermanentDevelopmentModifiers = Math.Max(0, otherPermanentDevelopmentModifiers);
            MaxEnergy = Math.Max(1, maxEnergy);
            FoodStorageCapacity = Math.Max(0, foodStorageCapacity);
            FoodStorageCapacityBonus = Math.Max(0, foodStorageCapacityBonus);
            WorldThreatTier = Math.Max(1, worldThreatTier);
            HordeBudgetTier = Math.Max(1, hordeBudgetTier);
        }

        public int Day { get; }
        public int SurvivorCount { get; }
        public int AdditionalSurvivorCount { get; }
        public int ActiveBuildingLevelSum { get; }
        public int PermanentSlotUpgradeCount { get; }
        public int OtherPermanentDevelopmentModifiers { get; }
        public int MaxEnergy { get; }
        public int FoodStorageCapacity { get; }
        public int FoodStorageCapacityBonus { get; }
        public int WorldThreatTier { get; }
        public int HordeBudgetTier { get; }
    }
}
