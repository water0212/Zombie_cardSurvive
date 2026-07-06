using System;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public enum DeckVacancyReason
    {
        Exhausted,
        BuildingCompleted,
        BuildingCancelled
    }

    [Serializable]
    public class DeckVacancyRecord
    {
        public DeckVacancyRecord(CardRuntime sourceCard, DeckSlotType slotType, DeckVacancyReason reason)
        {
            SourceCard = sourceCard;
            SlotType = slotType;
            Reason = reason;
        }

        public CardRuntime SourceCard { get; }
        public DeckSlotType SlotType { get; }
        public DeckVacancyReason Reason { get; }
    }
}
