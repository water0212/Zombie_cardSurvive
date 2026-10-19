namespace ZombieCardSurvive.Events.Data
{
    public enum EventMoodCategory
    {
        Positive,
        Negative,
        Mixed
    }

    public enum EventContentCategory
    {
        Resource,
        Survivor,
        Effect,
        Zombie,
        Story
    }

    public enum EventImplementationStatus
    {
        Ready,
        RequiresSupportSystem
    }

    public enum EventConditionMatchMode
    {
        All,
        Any
    }

    public enum EventConditionType
    {
        Morale,
        Food,
        Resource,
        MaxEnergy,
        ActiveZombieCount,
        BuildingLevel,
        EventAppearanceCount,
        DaysSinceLastNegativeEvent,
        RunFlag,
        CustomValue,
        RunFlagValue,
        RunFlagDaysSinceSet
    }

    public enum EventValueComparison
    {
        Equal,
        NotEqual,
        AtLeast,
        AtMost,
        GreaterThan,
        LessThan
    }
}
