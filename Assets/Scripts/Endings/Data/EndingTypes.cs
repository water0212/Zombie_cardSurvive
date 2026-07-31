namespace ZombieCardSurvive.Endings.Data
{
    public enum EndingScenarioType
    {
        Special,
        Default
    }

    public enum EndingConditionMatchMode
    {
        All,
        Any
    }

    public enum EndingConditionType
    {
        RunFlag,
        KeyItemOwned,
        RegionVisited,
        CardAcquired,
        BuildingLevel,
        Day,
        ActiveZombieCount,
        Morale,
        Food,
        Resource,
        Custom
    }

    public enum EndingValueComparison
    {
        Equal,
        NotEqual,
        AtLeast,
        AtMost,
        GreaterThan,
        LessThan
    }

    public enum EndingOfferChoiceResult
    {
        None,
        Accept,
        Reject
    }

    public enum EndingEventPackOrderMode
    {
        ShuffleWithoutReplacement,
        ManualSequence
    }

    public enum EndingEventPackExhaustionMode
    {
        Stop,
        LoopInOrder,
        Reshuffle,
        RepeatLast
    }

    public enum EndingDeadlineMode
    {
        WhenActivePackExhausted,
        DaysAfterAcceptance,
        AfterEvent,
        None
    }

    public enum EndingFailureMode
    {
        UseNormalRunFailure,
        Immediate,
        Custom
    }
}
