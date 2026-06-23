using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Cards.Data
{
    [Serializable]
    public class DeckSlotDefinition
    {
        [SerializeField] private DeckSlotType slotType = DeckSlotType.Unrestricted;
        [SerializeField] private int count = 1;
        [SerializeField] private List<CardType> acceptedCardTypes = new List<CardType>();
        [SerializeField] private CardBase defaultCard;

        public DeckSlotType SlotType => slotType;
        public int Count => Mathf.Max(0, count);
        public IReadOnlyList<CardType> AcceptedCardTypes => acceptedCardTypes;
        public CardBase DefaultCard => defaultCard;

        public bool CanAccept(CardBase card)
        {
            if (card == null)
            {
                return false;
            }

            if (!card.CanUseInSlot(slotType))
            {
                return false;
            }

            return acceptedCardTypes == null
                || acceptedCardTypes.Count == 0
                || acceptedCardTypes.Contains(card.EffectiveCardType);
        }
    }
}
