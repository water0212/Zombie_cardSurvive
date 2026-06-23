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

                List<CardBase> candidates = GetValidCandidates(deckLayout, target.AssignedSlotType, request.CandidateCards);
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
            CardBase replacement,
            out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            if (cardController == null || request == null || target == null || replacement == null)
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

            if (request.CandidateCards.Count > 0 && !ContainsReference(request.CandidateCards, replacement))
            {
                return false;
            }

            bool consumeFromInventory = inventory != null && !request.IsFreeCandidate(replacement);
            if (consumeFromInventory && !inventory.RemoveCard(replacement))
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
                    inventory.AddCard(replacement);
                }

                return false;
            }

            if (inventory != null && target.Data != null && !target.IsExhausted)
            {
                inventory.AddCard(target.Data);
            }

            return true;
        }

        private static List<CardBase> GetValidCandidates(
            DeckLayoutDefinition deckLayout,
            DeckSlotType slotType,
            IReadOnlyList<CardBase> candidateCards)
        {
            List<CardBase> validCandidates = new List<CardBase>();
            foreach (CardBase candidate in candidateCards)
            {
                if (IsValidCandidate(deckLayout, slotType, candidate))
                {
                    validCandidates.Add(candidate);
                }
            }

            return validCandidates;
        }

        private static bool IsValidCandidate(DeckLayoutDefinition deckLayout, DeckSlotType slotType, CardBase candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (deckLayout == null)
            {
                return candidate.CanUseInSlot(slotType);
            }

            return deckLayout.CanCardUseSlot(candidate, slotType);
        }

        private static bool ContainsReference(IReadOnlyList<CardBase> cards, CardBase target)
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
    }
}
