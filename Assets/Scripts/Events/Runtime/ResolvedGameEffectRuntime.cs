using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class ResolvedGameEffectRuntime
    {
        public ResolvedGameEffectRuntime(
            GameEffectData source,
            int baseAmount,
            int finalAmount,
            int scaleTier,
            float multiplier,
            ResolvedHordeSpawnRuntime hordeSpawn = null)
        {
            Source = source;
            BaseAmount = baseAmount;
            FinalAmount = finalAmount;
            ScaleTier = scaleTier;
            Multiplier = multiplier;
            HordeSpawn = hordeSpawn;
        }

        public GameEffectData Source { get; }
        public int BaseAmount { get; }
        public int FinalAmount { get; }
        public int ScaleTier { get; }
        public float Multiplier { get; }
        public ResolvedHordeSpawnRuntime HordeSpawn { get; }
        public bool IsMoraleEffect => Source != null && Source.IsMoraleEffect;
        public int MoraleDelta => IsMoraleEffect ? Source.ResolveMoraleDelta(FinalAmount) : 0;
        public bool HasInvalidConfiguration => Source != null
            && Source.IsSpawnHorde
            && (HordeSpawn == null || !HordeSpawn.IsConfigurationValid);
        public string UnavailabilityReason => HordeSpawn != null
            ? HordeSpawn.UnavailabilityReason
            : string.Empty;

        public static ResolvedGameEffectRuntime Create(
            GameEffectData source,
            EventMagnitudeSnapshot magnitudeSnapshot)
        {
            int eventDay = magnitudeSnapshot != null ? magnitudeSnapshot.Day : 1;
            return Create(
                source,
                magnitudeSnapshot,
                null,
                runSeed: 0,
                eventDay: eventDay,
                eventId: string.Empty,
                choiceIndex: 0,
                effectIndex: 0);
        }

        public static ResolvedGameEffectRuntime Create(
            GameEffectData source,
            EventMagnitudeSnapshot magnitudeSnapshot,
            ThreatSnapshot threatSnapshot,
            int runSeed,
            int eventDay,
            string eventId,
            int choiceIndex,
            int effectIndex)
        {
            if (source == null)
            {
                return null;
            }

            if (source.IsSpawnHorde)
            {
                int seed = EventHordeSeedUtility.CreateSeed(
                    runSeed,
                    eventDay,
                    eventId,
                    choiceIndex,
                    effectIndex);
                ResolvedHordeSpawnRuntime hordeSpawn = ResolvedHordeSpawnRuntime.Create(
                    source,
                    threatSnapshot,
                    seed);
                return new ResolvedGameEffectRuntime(
                    source,
                    source.EventBonusBudget,
                    hordeSpawn.SpawnCount,
                    1,
                    1f,
                    hordeSpawn);
            }

            if (source.IsNextRoundEffect)
            {
                return new ResolvedGameEffectRuntime(
                    source,
                    source.Amount,
                    source.Amount,
                    1,
                    1f);
            }

            int scaleTier = EventEffectScalingResolver.ResolveScaleTier(
                source.EventScaling,
                magnitudeSnapshot);
            float multiplier = EventEffectScalingResolver.ResolveMultiplier(
                source.EventScaling,
                magnitudeSnapshot);
            int finalAmount = EventEffectScalingResolver.ResolveAmount(
                source.Amount,
                source.EventScaling,
                magnitudeSnapshot);

            return new ResolvedGameEffectRuntime(
                source,
                source.Amount,
                finalAmount,
                scaleTier,
                multiplier);
        }

        public bool CanApply(GameEffectContext context)
        {
            return HordeSpawn != null
                ? HordeSpawn.CanExecute
                : Source == null || Source.CanApply(context, FinalAmount);
        }

        public bool CanApply(GameEffectContext context, int amount)
        {
            return HordeSpawn != null
                ? HordeSpawn.CanExecute
                : Source == null || Source.CanApply(context, amount);
        }

        public bool TryApply(GameEffectContext context)
        {
            return HordeSpawn != null
                ? HordeSpawn.TryExecute()
                : Source == null || Source.TryApply(context, FinalAmount);
        }

        public bool TryApply(GameEffectContext context, int amount)
        {
            return HordeSpawn != null
                ? HordeSpawn.TryExecute()
                : Source == null || Source.TryApply(context, amount);
        }

        public bool ShouldApply => Source == null || Source.ShouldApplyForCurrentState();
    }
}
