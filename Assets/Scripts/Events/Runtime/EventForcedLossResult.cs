using ZombieCardSurvive.Effects.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public readonly struct EventForcedLossResult
    {
        public EventForcedLossResult(
            int sourceEffectIndex,
            ValueEffectType valueEffectType,
            int requested,
            int consumed,
            int shortage)
        {
            SourceEffectIndex = sourceEffectIndex;
            ValueEffectType = valueEffectType;
            Requested = requested;
            Consumed = consumed;
            Shortage = shortage;
        }

        public int SourceEffectIndex { get; }
        public ValueEffectType ValueEffectType { get; }
        public int Requested { get; }
        public int Consumed { get; }
        public int Shortage { get; }
    }
}
