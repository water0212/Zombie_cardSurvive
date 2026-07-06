namespace ZombieCardSurvive.Buildings.Data
{
    public enum BuildingState
    {
        Vacant,
        Constructing,
        Active,
        Upgrading,
        CancelPending
    }

    public enum BuildingJobState
    {
        Queued,
        InProgress,
        CancelPending,
        Completed,
        Cancelled
    }
}
