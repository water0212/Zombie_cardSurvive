using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class DeckReplacementOption
    {
        public DeckReplacementOption(CardRuntime targetCard, List<CardBase> candidateCards)
        {
            TargetCard = targetCard;
            CandidateCards = candidateCards ?? new List<CardBase>();
        }

        public CardRuntime TargetCard { get; }
        public IReadOnlyList<CardBase> CandidateCards { get; }
        public DeckSlotType SlotType => TargetCard != null ? TargetCard.AssignedSlotType : DeckSlotType.Unrestricted;
        public bool HasCandidates => CandidateCards.Count > 0;
    }
}
