using System;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class ResolvedForcedLossConsequenceRuntime
    {
        private ResolvedForcedLossConsequenceRuntime(
            int sourceEffectIndex,
            EventShortageConsequenceAmountMode amountMode,
            ResolvedGameEffectRuntime effect)
        {
            SourceEffectIndex = sourceEffectIndex;
            AmountMode = amountMode;
            Effect = effect;
        }

        public int SourceEffectIndex { get; }
        public EventShortageConsequenceAmountMode AmountMode { get; }
        public ResolvedGameEffectRuntime Effect { get; }

        public static ResolvedForcedLossConsequenceRuntime Create(
            EventForcedLossConsequenceData source,
            EventMagnitudeSnapshot magnitudeSnapshot)
        {
            return source == null
                ? null
                : new ResolvedForcedLossConsequenceRuntime(
                    source.SourceEffectIndex,
                    source.AmountMode,
                    ResolvedGameEffectRuntime.Create(source.ConsequenceEffect, magnitudeSnapshot));
        }

        public int ResolveAmount(int shortage)
        {
            if (Effect == null || shortage <= 0)
            {
                return 0;
            }

            long amount = Effect.FinalAmount;
            if (AmountMode == EventShortageConsequenceAmountMode.PerMissingUnit)
            {
                amount *= shortage;
            }

            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, amount));
        }
    }
}
