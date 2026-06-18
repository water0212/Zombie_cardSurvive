namespace ZombieCardSurvive.Events.Data
{
    [System.Obsolete("Use ZombieCardSurvive.Effects.Data.ValueEffectType or CardFlowEffectType instead.")]
    public enum EventEffectType
    {
        AddFood,
        SpendFood,
        AddFoodProduction,
        AddResource,
        SpendResource,
        AddResourceProduction,
        AddZombieThreat,
        RemoveZombieThreat,
        AddPendingDamage,
        ClearPendingDamage,
        AddCurrentEnergy,
        IncreaseMaxEnergy,
        DecreaseMaxEnergy,
        RefillEnergy
    }
}
