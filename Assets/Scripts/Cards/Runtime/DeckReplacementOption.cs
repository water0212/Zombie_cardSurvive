using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class DeckReplacementOption
    {
        public DeckReplacementOption(CardRuntime targetCard, List<CardInventoryEntry> candidateEntries)
        {
            TargetCard = targetCard;
            CandidateEntries = candidateEntries ?? new List<CardInventoryEntry>();
            CandidateCards = CopyCardData(CandidateEntries);
        }

        public CardRuntime TargetCard { get; }
        public IReadOnlyList<CardInventoryEntry> CandidateEntries { get; }
        public IReadOnlyList<CardBase> CandidateCards { get; }
        public DeckSlotType SlotType => TargetCard != null ? TargetCard.AssignedSlotType : DeckSlotType.Unrestricted;
        public bool HasCandidates => CandidateEntries.Count > 0;

        private static List<CardBase> CopyCardData(IEnumerable<CardInventoryEntry> entries)
        {
            List<CardBase> result = new List<CardBase>();
            if (entries == null)
            {
                return result;
            }

            foreach (CardInventoryEntry entry in entries)
            {
                if (entry != null && entry.Data != null)
                {
                    result.Add(entry.Data);
                }
            }

            return result;
        }
    }
}
