using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class HordeCompositionEntry
    {
        public HordeCompositionEntry(
            ZombieCardData zombieCard,
            ZombieArchetypeEntry sourceEntry,
            int budgetCost)
        {
            ZombieCard = zombieCard;
            SourceEntry = sourceEntry;
            BudgetCost = budgetCost;
        }

        public ZombieCardData ZombieCard { get; }
        public ZombieArchetypeEntry SourceEntry { get; }
        public int BudgetCost { get; }
    }
}
