using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public static class DeckReplacementSystem
    {
        public static List<DeckReplacementOption> GetOptions(
            CardController cardController,
            DeckLayoutDefinition deckLayout,
            DeckReplacementRequest request)
        {
            List<DeckReplacementOption> options = new List<DeckReplacementOption>();
            if (cardController == null || request == null || request.ReplacementCount <= 0)
            {
                return options;
            }

            foreach (CardRuntime target in request.ReplaceableCards)
            {
                if (!request.AllowsTarget(target))
                {
                    continue;
                }

                List<CardInventoryEntry> candidates = GetValidCandidates(deckLayout, target.AssignedSlotType, request.CandidateEntries);
                options.Add(new DeckReplacementOption(target, candidates));
            }

            return options;
        }

        public static bool TryReplace(
            CardController cardController,
            CardInventory inventory,
            DeckLayoutDefinition deckLayout,
            DeckReplacementRequest request,
            CardRuntime target,
            CardInventoryEntry replacement,
            out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            if (cardController == null || request == null || target == null || replacement == null || replacement.Data == null)
            {
                return false;
            }

            if (!request.AllowsTarget(target))
            {
                return false;
            }

            if (!IsValidCandidate(deckLayout, target.AssignedSlotType, replacement))
            {
                return false;
            }

            if (request.CandidateEntries.Count > 0 && !ContainsReference(request.CandidateEntries, replacement))
            {
                return false;
            }

            bool consumeFromInventory = inventory != null && !request.IsFreeCandidate(replacement);
            if (consumeFromInventory && !inventory.RemoveEntry(replacement))
            {
                return false;
            }

            bool replaced = target.IsExhausted
                ? cardController.TryRefillExhaustedSlot(target, replacement, out replacementRuntime)
                : cardController.TryReplaceRuntimeCard(target, replacement, out replacementRuntime);

            if (!replaced)
            {
                if (consumeFromInventory)
                {
                    inventory.AddEntry(replacement);
                }

                return false;
            }

            if (inventory != null && target.Data != null && !target.IsExhausted)
            {
                inventory.AddRuntimeCard(target);
            }

            return true;
        }

        private static List<CardInventoryEntry> GetValidCandidates(
            DeckLayoutDefinition deckLayout,
            DeckSlotType slotType,
            IReadOnlyList<CardInventoryEntry> candidateEntries)
        {
            List<CardInventoryEntry> validCandidates = new List<CardInventoryEntry>();
            foreach (CardInventoryEntry candidate in candidateEntries)
            {
                if (IsValidCandidate(deckLayout, slotType, candidate))
                {
                    validCandidates.Add(candidate);
                }
            }

            return validCandidates;
        }

        private static bool IsValidCandidate(DeckLayoutDefinition deckLayout, DeckSlotType slotType, CardInventoryEntry candidate)
        {
            if (candidate == null || candidate.Data == null || !candidate.HasRemainingUses)
            {
                return false;
            }

            if (deckLayout == null)
            {
                return candidate.Data.CanUseInSlot(slotType);
            }

            return deckLayout.CanCardUseSlot(candidate.Data, slotType);
        }

        private static bool ContainsReference(IReadOnlyList<CardInventoryEntry> entries, CardInventoryEntry target)
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
    }
}
