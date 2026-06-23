using System;
using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class DeckReplacementSession
    {
        private readonly CardController cardController;
        private readonly CardInventory inventory;
        private readonly DeckLayoutDefinition deckLayout;
        private readonly DeckReplacementRequest request;
        private readonly List<DeckReplacementOption> options = new List<DeckReplacementOption>();

        public DeckReplacementSession(
            CardController cardController,
            CardInventory inventory,
            DeckLayoutDefinition deckLayout,
            DeckReplacementRequest request)
        {
            this.cardController = cardController;
            this.inventory = inventory;
            this.deckLayout = deckLayout;
            this.request = request;
            RefreshOptions();
        }

        public event Action Changed;

        public DeckReplacementRequest Request => request;
        public IReadOnlyList<DeckReplacementOption> Options => options;
        public int CompletedReplacementCount { get; private set; }
        public int RemainingReplacementCount => Math.Max(0, request.ReplacementCount - CompletedReplacementCount);
        public bool IsComplete => RemainingReplacementCount <= 0;
        public bool HasOptions => options.Count > 0;

        public void RefreshOptions()
        {
            options.Clear();

            if (IsComplete)
            {
                NotifyChanged();
                return;
            }

            List<DeckReplacementOption> newOptions = DeckReplacementSystem.GetOptions(cardController, deckLayout, request);
            foreach (DeckReplacementOption option in newOptions)
            {
                if (option != null)
                {
                    options.Add(FilterInventoryCandidates(option));
                }
            }

            NotifyChanged();
        }

        public bool TryApply(CardRuntime target, CardBase replacement, out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            if (IsComplete)
            {
                return false;
            }

            bool replaced = DeckReplacementSystem.TryReplace(
                cardController,
                inventory,
                deckLayout,
                request,
                target,
                replacement,
                out replacementRuntime);

            if (!replaced)
            {
                return false;
            }

            CompletedReplacementCount++;
            RefreshOptions();
            return true;
        }

        public bool TryApplyFirstValid(out CardRuntime replacementRuntime)
        {
            replacementRuntime = null;

            foreach (DeckReplacementOption option in options)
            {
                if (option == null || !option.HasCandidates)
                {
                    continue;
                }

                return TryApply(option.TargetCard, option.CandidateCards[0], out replacementRuntime);
            }

            return false;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private DeckReplacementOption FilterInventoryCandidates(DeckReplacementOption option)
        {
            if (inventory == null || option == null)
            {
                return option;
            }

            List<CardBase> candidates = new List<CardBase>();
            foreach (CardBase candidate in option.CandidateCards)
            {
                if (inventory.Contains(candidate) || request.IsFreeCandidate(candidate))
                {
                    candidates.Add(candidate);
                }
            }

            return new DeckReplacementOption(option.TargetCard, candidates);
        }
    }
}
