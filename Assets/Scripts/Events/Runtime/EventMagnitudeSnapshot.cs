using System;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class EventMagnitudeSnapshot
    {
        public EventMagnitudeSnapshot(
            EventMagnitudeInputSnapshot input,
            int timeTier,
            int developmentPoints,
            int currentSettlementTier,
            int peakSettlementTier,
            int eventMagnitude,
            int foodStorageCapacityTier)
        {
            Input = input;
            TimeTier = Math.Max(1, timeTier);
            DevelopmentPoints = Math.Max(0, developmentPoints);
            CurrentSettlementTier = Math.Max(1, currentSettlementTier);
            PeakSettlementTier = Math.Max(1, peakSettlementTier);
            EventMagnitude = Math.Max(1, eventMagnitude);
            FoodStorageCapacityTier = Math.Max(1, foodStorageCapacityTier);
        }

        public EventMagnitudeInputSnapshot Input { get; }
        public int Day => Input.Day;
        public int TimeTier { get; }
        public int DevelopmentPoints { get; }
        public int CurrentSettlementTier { get; }
        public int PeakSettlementTier { get; }
        public int EventMagnitude { get; }
        public int FoodStorageCapacityTier { get; }

        public int GetScaleTier(EventScaleSource source)
        {
            switch (source)
            {
                case EventScaleSource.TimeTier:
                    return TimeTier;
                case EventScaleSource.SettlementTier:
                    return CurrentSettlementTier;
                case EventScaleSource.EventMagnitude:
                    return EventMagnitude;
                case EventScaleSource.SurvivorCount:
                    return Math.Max(1, Input.SurvivorCount);
                case EventScaleSource.MaxEnergy:
                    return Math.Max(1, Input.MaxEnergy);
                case EventScaleSource.FoodStorageCapacity:
                    return FoodStorageCapacityTier;
                case EventScaleSource.WorldThreat:
                    return Math.Max(1, Input.WorldThreatTier);
                case EventScaleSource.HordeBudget:
                    return Math.Max(1, Input.HordeBudgetTier);
                default:
                    return 1;
            }
        }
    }
}
