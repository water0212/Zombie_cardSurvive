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
            IEnumerable<CardInventoryEntry> candidateEntries,
            IEnumerable<DeckSlotType> allowedSlotTypes = null,
            IEnumerable<CardInventoryEntry> freeCandidateEntries = null)
        {
            Reason = reason;
            ReplacementCount = Math.Max(0, replacementCount);
            ReplaceableCards = CopyRuntimeCards(replaceableCards);
            CandidateEntries = CopyEntries(candidateEntries);
            CandidateCards = CopyEntryCardData(CandidateEntries);
            AllowedSlotTypes = CopySlotTypes(allowedSlotTypes);
            FreeCandidateEntries = CopyEntries(freeCandidateEntries);
            FreeCandidateCards = CopyEntryCardData(FreeCandidateEntries);
        }

        public DeckReplacementReason Reason { get; }
        public int ReplacementCount { get; }
        public IReadOnlyList<CardRuntime> ReplaceableCards { get; }
        public IReadOnlyList<CardInventoryEntry> CandidateEntries { get; }
        public IReadOnlyList<CardBase> CandidateCards { get; }
        public IReadOnlyList<DeckSlotType> AllowedSlotTypes { get; }
        public IReadOnlyList<CardInventoryEntry> FreeCandidateEntries { get; }
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

        public bool IsFreeCandidate(CardInventoryEntry candidate)
        {
            return candidate != null && (candidate.IsReusable || ContainsEntryReference(FreeCandidateEntries, candidate));
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

        private static List<CardInventoryEntry> CopyEntries(IEnumerable<CardInventoryEntry> source)
        {
            List<CardInventoryEntry> result = new List<CardInventoryEntry>();
            if (source == null)
            {
                return result;
            }

            foreach (CardInventoryEntry entry in source)
            {
                if (entry != null && entry.Data != null)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static List<CardBase> CopyEntryCardData(IEnumerable<CardInventoryEntry> source)
        {
            List<CardBase> result = new List<CardBase>();
            if (source == null)
            {
                return result;
            }

            foreach (CardInventoryEntry entry in source)
            {
                if (entry != null && entry.Data != null)
                {
                    result.Add(entry.Data);
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

        private static bool ContainsEntryReference(IReadOnlyList<CardInventoryEntry> entries, CardInventoryEntry target)
        {
            foreach (CardInventoryEntry entry in entries)
            {
                if (ReferenceEquals(entry, target))
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
