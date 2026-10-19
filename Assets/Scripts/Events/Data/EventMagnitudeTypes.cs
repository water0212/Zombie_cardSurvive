namespace ZombieCardSurvive.Events.Data
{
    public enum EventScaleSource
    {
        Fixed,
        TimeTier,
        SettlementTier,
        EventMagnitude,
        SurvivorCount,
        MaxEnergy,
        FoodStorageCapacity,
        WorldThreat,
        HordeBudget
    }

    public enum EventScaleRoundingMode
    {
        Floor,
        Round,
        Ceil
    }
}
