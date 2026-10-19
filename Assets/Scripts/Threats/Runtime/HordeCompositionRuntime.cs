using System;
using System.Collections.Generic;
using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class HordeCompositionRuntime
    {
        public const int MaxGeneratedZombiesPerHorde = 256;

        public HordeCompositionResult LastResult { get; private set; }

        public HordeCompositionResult Generate(
            HordeProfileData profile,
            HordeBudgetSnapshot budget,
            Random random = null)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (budget == null)
            {
                throw new ArgumentNullException(nameof(budget));
            }

            random ??= new Random(unchecked(Environment.TickCount));

            int remainingBudget = Math.Max(0, budget.FinalBudget);
            int spentBudget = 0;
            int fixedCursor = 0;
            IReadOnlyList<ZombieArchetypeEntry> pool = profile.ZombiePool;
            int[] countsByIndex = new int[pool != null ? pool.Count : 0];
            List<HordeCompositionEntry> entries = new List<HordeCompositionEntry>();
            HordeCompositionStopReason stopReason = HordeCompositionStopReason.BudgetSpent;

            for (int safety = 0; safety < MaxGeneratedZombiesPerHorde; safety++)
            {
                List<Candidate> candidates = BuildCandidates(
                    pool,
                    countsByIndex,
                    budget.Input.WorldThreatTier,
                    remainingBudget,
                    profile.CompositionStyle);

                if (candidates.Count == 0)
                {
                    stopReason = HasEligibleZombieIgnoringBudget(
                            pool,
                            countsByIndex,
                            budget.Input.WorldThreatTier)
                        ? HordeCompositionStopReason.BudgetTooLow
                        : HordeCompositionStopReason.NoEligibleZombie;
                    break;
                }

                Candidate selected = profile.CompositionStyle == HordeCompositionStyle.FixedOrder
                    ? SelectFixedOrderCandidate(candidates, pool.Count, ref fixedCursor)
                    : SelectWeightedCandidate(candidates, random);

                if (selected == null)
                {
                    stopReason = HordeCompositionStopReason.NoEligibleZombie;
                    break;
                }

                countsByIndex[selected.Index]++;
                remainingBudget -= selected.Entry.BudgetCost;
                spentBudget += selected.Entry.BudgetCost;
                entries.Add(new HordeCompositionEntry(
                    selected.Entry.ZombieCard,
                    selected.Entry,
                    selected.Entry.BudgetCost));

                if (remainingBudget <= 0)
                {
                    stopReason = HordeCompositionStopReason.BudgetSpent;
                    break;
                }

                if (safety == MaxGeneratedZombiesPerHorde - 1)
                {
                    stopReason = HordeCompositionStopReason.SafetyLimitReached;
                }
            }

            LastResult = new HordeCompositionResult(
                profile,
                budget,
                entries,
                spentBudget,
                Math.Max(0, remainingBudget),
                stopReason);
            return LastResult;
        }

        private static List<Candidate> BuildCandidates(
            IReadOnlyList<ZombieArchetypeEntry> pool,
            int[] countsByIndex,
            int worldThreatTier,
            int remainingBudget,
            HordeCompositionStyle style)
        {
            List<Candidate> candidates = new List<Candidate>();
            if (pool == null)
            {
                return candidates;
            }

            for (int index = 0; index < pool.Count; index++)
            {
                ZombieArchetypeEntry entry = pool[index];
                if (!IsEligible(entry, countsByIndex[index], worldThreatTier)
                    || entry.BudgetCost > remainingBudget)
                {
                    continue;
                }

                double weight = GetEffectiveWeight(entry, style);
                if (weight <= 0d)
                {
                    continue;
                }

                candidates.Add(new Candidate(index, entry, weight));
            }

            return candidates;
        }

        private static bool HasEligibleZombieIgnoringBudget(
            IReadOnlyList<ZombieArchetypeEntry> pool,
            int[] countsByIndex,
            int worldThreatTier)
        {
            if (pool == null)
            {
                return false;
            }

            for (int index = 0; index < pool.Count; index++)
            {
                if (IsEligible(pool[index], countsByIndex[index], worldThreatTier))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEligible(
            ZombieArchetypeEntry entry,
            int currentCount,
            int worldThreatTier)
        {
            return entry != null
                && entry.IsConfigured
                && entry.IsAllowedAtWorldThreatTier(worldThreatTier)
                && entry.IsWithinCountLimit(currentCount);
        }

        private static double GetEffectiveWeight(
            ZombieArchetypeEntry entry,
            HordeCompositionStyle style)
        {
            switch (style)
            {
                case HordeCompositionStyle.Quantity:
                    return entry.BaseWeight / (double)entry.BudgetCost;
                case HordeCompositionStyle.Elite:
                    return entry.BaseWeight * (double)entry.BudgetCost;
                default:
                    return entry.BaseWeight;
            }
        }

        private static Candidate SelectFixedOrderCandidate(
            IReadOnlyList<Candidate> candidates,
            int poolCount,
            ref int fixedCursor)
        {
            if (candidates == null || candidates.Count == 0 || poolCount <= 0)
            {
                return null;
            }

            for (int offset = 0; offset < poolCount; offset++)
            {
                int index = (fixedCursor + offset) % poolCount;
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    Candidate candidate = candidates[candidateIndex];
                    if (candidate.Index != index)
                    {
                        continue;
                    }

                    fixedCursor = (index + 1) % poolCount;
                    return candidate;
                }
            }

            return null;
        }

        private static Candidate SelectWeightedCandidate(
            IReadOnlyList<Candidate> candidates,
            Random random)
        {
            double totalWeight = 0d;
            for (int index = 0; index < candidates.Count; index++)
            {
                totalWeight += candidates[index].Weight;
            }

            if (totalWeight <= 0d)
            {
                return null;
            }

            double roll = random.NextDouble() * totalWeight;
            double cumulative = 0d;
            Candidate fallback = null;
            for (int index = 0; index < candidates.Count; index++)
            {
                Candidate candidate = candidates[index];
                fallback = candidate;
                cumulative += candidate.Weight;
                if (roll < cumulative)
                {
                    return candidate;
                }
            }

            return fallback;
        }

        private sealed class Candidate
        {
            public Candidate(int index, ZombieArchetypeEntry entry, double weight)
            {
                Index = index;
                Entry = entry;
                Weight = weight;
            }

            public int Index { get; }
            public ZombieArchetypeEntry Entry { get; }
            public double Weight { get; }
        }
    }
}
