using System;
using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Events.Runtime
{
    /// <summary>
    /// 事件顯示時鎖定的屍群結果。預覽階段只保存結果，玩家確認選項後才生成場上威脅。
    /// </summary>
    public sealed class ResolvedHordeSpawnRuntime
    {
        private readonly List<ZombieCardData> sourceCards;

        private ResolvedHordeSpawnRuntime(
            HordeBudgetSnapshot budgetSnapshot,
            HordeCompositionResult compositionResult,
            int seed,
            bool isConfigurationValid,
            string unavailabilityReason,
            List<ZombieCardData> sourceCards)
        {
            BudgetSnapshot = budgetSnapshot;
            CompositionResult = compositionResult;
            Seed = seed;
            IsConfigurationValid = isConfigurationValid;
            UnavailabilityReason = unavailabilityReason ?? string.Empty;
            this.sourceCards = sourceCards ?? new List<ZombieCardData>();
            SpawnedThreats = Array.Empty<ZombieThreatRuntime>();
        }

        public HordeBudgetSnapshot BudgetSnapshot { get; }
        public HordeCompositionResult CompositionResult { get; }
        public int Seed { get; }
        public bool IsConfigurationValid { get; }
        public string UnavailabilityReason { get; }
        public bool HasExecuted { get; private set; }
        public int SpawnCount => CompositionResult != null ? CompositionResult.SpawnCount : 0;
        public HordeCompositionStopReason? StopReason => CompositionResult?.StopReason;
        public IReadOnlyList<ZombieThreatRuntime> SpawnedThreats { get; private set; }

        public bool CanExecute => IsConfigurationValid
            && !HasExecuted
            && sourceCards.Count > 0
            && CombatSystem.CanSpawnZombieThreats(sourceCards);

        public static ResolvedHordeSpawnRuntime Create(
            GameEffectData source,
            ThreatSnapshot threatSnapshot,
            int seed)
        {
            if (source == null || !source.IsSpawnHorde)
            {
                return Invalid(seed, "The effect is not configured as Event Threat / SpawnHorde.");
            }

            if (source.HordeProfile == null)
            {
                return Invalid(seed, "SpawnHorde requires a HordeProfileData asset.");
            }

            HordeBudgetSnapshot budget = new HordeBudgetRuntime().Capture(
                source.HordeProfile,
                HordeBudgetInputSnapshot.FromThreat(
                    threatSnapshot,
                    regionPressure: null,
                    eventBonusBudget: source.EventBonusBudget));
            HordeCompositionResult composition = new HordeCompositionRuntime().Generate(
                source.HordeProfile,
                budget,
                new Random(seed));

            List<ZombieCardData> cards = new List<ZombieCardData>(composition.SpawnCount);
            for (int index = 0; index < composition.Entries.Count; index++)
            {
                ZombieCardData zombieCard = composition.Entries[index]?.ZombieCard;
                if (zombieCard == null)
                {
                    return new ResolvedHordeSpawnRuntime(
                        budget,
                        composition,
                        seed,
                        false,
                        $"Horde composition entry {index} has no ZombieCardData.",
                        cards);
                }

                cards.Add(zombieCard);
            }

            string unavailableReason = composition.SpawnCount > 0
                ? string.Empty
                : GetEmptyCompositionReason(composition.StopReason);
            return new ResolvedHordeSpawnRuntime(
                budget,
                composition,
                seed,
                true,
                unavailableReason,
                cards);
        }

        public bool TryExecute()
        {
            if (!CanExecute)
            {
                return false;
            }

            IReadOnlyList<ZombieThreatRuntime> spawned = CombatSystem.SpawnZombieThreats(sourceCards);
            if (spawned == null || spawned.Count != sourceCards.Count)
            {
                return false;
            }

            SpawnedThreats = spawned;
            HasExecuted = true;
            return true;
        }

        private static ResolvedHordeSpawnRuntime Invalid(int seed, string reason)
        {
            return new ResolvedHordeSpawnRuntime(
                null,
                null,
                seed,
                false,
                reason,
                new List<ZombieCardData>());
        }

        private static string GetEmptyCompositionReason(HordeCompositionStopReason stopReason)
        {
            switch (stopReason)
            {
                case HordeCompositionStopReason.BudgetTooLow:
                    return "The locked horde budget cannot afford any eligible zombie.";
                case HordeCompositionStopReason.NoEligibleZombie:
                    return "The HordeProfile has no eligible zombie for the locked world-threat tier.";
                default:
                    return "The locked horde composition contains no zombies.";
            }
        }
    }
}
