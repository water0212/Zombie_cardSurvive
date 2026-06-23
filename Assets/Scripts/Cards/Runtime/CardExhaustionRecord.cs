using System;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    [Serializable]
    public class CardExhaustionRecord
    {
        public CardExhaustionRecord(CardRuntime card, DeckSlotType slotType)
        {
            Card = card;
            SlotType = slotType;
        }

        public CardRuntime Card { get; }
        public DeckSlotType SlotType { get; }
    }
}
