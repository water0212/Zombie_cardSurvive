using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class HordeCompositionResult
    {
        public HordeCompositionResult(
            HordeProfileData profile,
            HordeBudgetSnapshot budget,
            List<HordeCompositionEntry> entries,
            int spentBudget,
            int remainingBudget,
            HordeCompositionStopReason stopReason)
        {
            Profile = profile;
            Budget = budget;
            Entries = new ReadOnlyCollection<HordeCompositionEntry>(
                entries ?? new List<HordeCompositionEntry>());
            SpentBudget = spentBudget;
            RemainingBudget = remainingBudget;
            StopReason = stopReason;
        }

        public HordeProfileData Profile { get; }
        public HordeBudgetSnapshot Budget { get; }
        public IReadOnlyList<HordeCompositionEntry> Entries { get; }
        public int SpentBudget { get; }
        public int RemainingBudget { get; }
        public HordeCompositionStopReason StopReason { get; }
        public int SpawnCount => Entries.Count;
    }
}
