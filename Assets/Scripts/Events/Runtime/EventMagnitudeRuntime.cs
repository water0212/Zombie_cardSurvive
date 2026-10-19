using System;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class EventMagnitudeRuntime
    {
        private EventMagnitudeSettingsSnapshot settings = EventMagnitudeSettingsSnapshot.Default;

        public int PeakSettlementTier { get; private set; } = 1;
        public EventMagnitudeSnapshot LastSnapshot { get; private set; }

        public void InitializeRun(EventMagnitudeSettingsData settingsData = null)
        {
            settings = settingsData != null
                ? settingsData.CreateSnapshot()
                : EventMagnitudeSettingsSnapshot.Default;
            PeakSettlementTier = 1;
            LastSnapshot = null;
        }

        public EventMagnitudeSnapshot Capture(EventMagnitudeInputSnapshot input)
        {
            int timeTier = 1 + (input.Day - 1) / settings.DaysPerTimeTier;
            int developmentPoints = input.AdditionalSurvivorCount
                + input.ActiveBuildingLevelSum
                + input.PermanentSlotUpgradeCount
                + input.OtherPermanentDevelopmentModifiers;
            int currentSettlementTier = 1
                + developmentPoints / settings.DevelopmentPointsPerSettlementTier;

            PeakSettlementTier = Math.Max(PeakSettlementTier, currentSettlementTier);
            int eventMagnitude = Math.Max(timeTier, PeakSettlementTier);
            int foodStorageCapacityTier = 1
                + input.FoodStorageCapacityBonus / settings.FoodStorageBonusPerTier;

            LastSnapshot = new EventMagnitudeSnapshot(
                input,
                timeTier,
                developmentPoints,
                currentSettlementTier,
                PeakSettlementTier,
                eventMagnitude,
                foodStorageCapacityTier);
            return LastSnapshot;
        }
    }
}
