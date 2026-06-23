using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public static class DeckBuildSystem
    {
        public static DeckBuildResult Build(DeckLayoutDefinition layout, IReadOnlyList<CardBase> sourceCards)
        {
            List<CardRuntime> regularCards = new List<CardRuntime>();
            List<CardRuntime> additiveCards = new List<CardRuntime>();
            List<CardBase> availableCards = new List<CardBase>();

            if (sourceCards != null)
            {
                foreach (CardBase card in sourceCards)
                {
                    if (card == null)
                    {
                        continue;
                    }

                    if (card.EffectiveIsAdditiveCard)
                    {
                        CardRuntime additive = card.CreateRuntimeCard();
                        additive.SetZone(DeckZone.AdditivePending);
                        additive.SetAdditiveInstance(true);
                        additiveCards.Add(additive);
                    }
                    else
                    {
                        availableCards.Add(card);
                    }
                }
            }

            if (layout == null)
            {
                foreach (CardBase card in availableCards)
                {
                    CardRuntime runtime = card.CreateRuntimeCard();
                    runtime.SetZone(DeckZone.DrawPile);
                    regularCards.Add(runtime);
                }

                return new DeckBuildResult(regularCards, additiveCards);
            }

            if (!layout.IsRegularDeckSizeValid)
            {
                Debug.LogWarning($"Deck layout '{layout.name}' regular deck size {layout.RegularDeckSize} is not divisible by draw count {layout.DrawCountPerTurn}.");
            }

            foreach (DeckSlotDefinition slot in layout.Slots)
            {
                if (slot == null)
                {
                    continue;
                }

                for (int i = 0; i < slot.Count; i++)
                {
                    CardBase selected = TakeFirstMatchingCard(availableCards, slot);
                    if (selected == null)
                    {
                        selected = slot.DefaultCard;
                    }

                    if (selected == null)
                    {
                        Debug.LogWarning($"Deck layout '{layout.name}' has no card available for slot {slot.SlotType}.");
                        continue;
                    }

                    CardRuntime runtime = selected.CreateRuntimeCard();
                    runtime.SetAssignedSlotType(slot.SlotType);
                    runtime.SetZone(DeckZone.DrawPile);
                    regularCards.Add(runtime);
                }
            }

            return new DeckBuildResult(regularCards, additiveCards);
        }

        private static CardBase TakeFirstMatchingCard(List<CardBase> cards, DeckSlotDefinition slot)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardBase card = cards[i];
                if (slot.CanAccept(card))
                {
                    cards.RemoveAt(i);
                    return card;
                }
            }

            return null;
        }
    }
}
