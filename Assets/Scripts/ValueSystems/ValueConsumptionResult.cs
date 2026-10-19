using System;

namespace ZombieCardSurvive.Systems
{
    public readonly struct ValueConsumptionResult
    {
        public ValueConsumptionResult(int requested, int consumed)
        {
            Requested = Math.Max(0, requested);
            Consumed = Math.Max(0, Math.Min(Requested, consumed));
            Shortage = Requested - Consumed;
        }

        public int Requested { get; }
        public int Consumed { get; }
        public int Shortage { get; }
    }
}
