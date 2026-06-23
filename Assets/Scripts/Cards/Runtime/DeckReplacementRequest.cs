using System;
using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class DeckReplacementRequest
    {
        public DeckReplacementRequest(
            DeckReplacementReason reason,
            int replacementCount,
            IEnumerable<CardRuntime> replaceableCards,
            IEnumerable<CardBase> candidateCards,
            IEnumerable<DeckSlotType> allowedSlotTypes = null,
            IEnumerable<CardBase> freeCandidateCards = null)
        {
            Reason = reason;
            ReplacementCount = Math.Max(0, replacementCount);
            ReplaceableCards = CopyRuntimeCards(replaceableCards);
            CandidateCards = CopyCardData(candidateCards);
            AllowedSlotTypes = CopySlotTypes(allowedSlotTypes);
            FreeCandidateCards = CopyCardData(freeCandidateCards);
        }

        public DeckReplacementReason Reason { get; }
        public int ReplacementCount { get; }
        public IReadOnlyList<CardRuntime> ReplaceableCards { get; }
        public IReadOnlyList<CardBase> CandidateCards { get; }
        public IReadOnlyList<DeckSlotType> AllowedSlotTypes { get; }
        public IReadOnlyList<CardBase> FreeCandidateCards { get; }

        public bool AllowsTarget(CardRuntime target)
        {
            if (target == null)
            {
                return false;
            }

            if (ReplaceableCards.Count > 0 && !ContainsReference(ReplaceableCards, target))
            {
                return false;
            }

            return AllowedSlotTypes.Count == 0 || ContainsSlotType(AllowedSlotTypes, target.AssignedSlotType);
        }

        public bool IsFreeCandidate(CardBase candidate)
        {
            return candidate != null && ContainsCardReference(FreeCandidateCards, candidate);
        }

        private static List<CardRuntime> CopyRuntimeCards(IEnumerable<CardRuntime> source)
        {
            List<CardRuntime> result = new List<CardRuntime>();
            if (source == null)
            {
                return result;
            }

            foreach (CardRuntime card in source)
            {
                if (card != null)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        private static List<CardBase> CopyCardData(IEnumerable<CardBase> source)
        {
            List<CardBase> result = new List<CardBase>();
            if (source == null)
            {
                return result;
            }

            foreach (CardBase card in source)
            {
                if (card != null)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        private static List<DeckSlotType> CopySlotTypes(IEnumerable<DeckSlotType> source)
        {
            List<DeckSlotType> result = new List<DeckSlotType>();
            if (source == null)
            {
                return result;
            }

            foreach (DeckSlotType slotType in source)
            {
                if (!result.Contains(slotType))
                {
                    result.Add(slotType);
                }
            }

            return result;
        }

        private static bool ContainsReference(IReadOnlyList<CardRuntime> cards, CardRuntime target)
        {
            foreach (CardRuntime card in cards)
            {
                if (ReferenceEquals(card, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCardReference(IReadOnlyList<CardBase> cards, CardBase target)
        {
            foreach (CardBase card in cards)
            {
                if (ReferenceEquals(card, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSlotType(IReadOnlyList<DeckSlotType> slotTypes, DeckSlotType target)
        {
            foreach (DeckSlotType slotType in slotTypes)
            {
                if (slotType == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
