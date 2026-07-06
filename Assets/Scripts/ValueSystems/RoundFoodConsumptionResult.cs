namespace ZombieCardSurvive.Systems
{
    public readonly struct RoundFoodConsumptionResult
    {
        public RoundFoodConsumptionResult(
            int requested,
            int permanentBefore,
            int permanentAfter,
            int temporaryBefore,
            int temporaryConsumed,
            int permanentConsumed,
            int shortage,
            int temporaryDiscarded)
        {
            Requested = requested;
            PermanentBefore = permanentBefore;
            PermanentAfter = permanentAfter;
            TemporaryBefore = temporaryBefore;
            TemporaryConsumed = temporaryConsumed;
            PermanentConsumed = permanentConsumed;
            Shortage = shortage;
            TemporaryDiscarded = temporaryDiscarded;
        }

        public int Requested { get; }
        public int PermanentBefore { get; }
        public int PermanentAfter { get; }
        public int TemporaryBefore { get; }
        public int TemporaryConsumed { get; }
        public int PermanentConsumed { get; }
        public int Shortage { get; }
        public int TemporaryDiscarded { get; }
    }
}
