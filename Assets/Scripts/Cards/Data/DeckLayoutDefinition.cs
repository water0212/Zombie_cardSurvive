using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "DeckLayout", menuName = "Zombie Card Survive/Cards/Deck Layout")]
    public class DeckLayoutDefinition : ScriptableObject
    {
        [SerializeField] private int drawCountPerTurn = 5;
        [SerializeField] private List<DeckSlotDefinition> slots = new List<DeckSlotDefinition>();

        public int DrawCountPerTurn => Mathf.Max(1, drawCountPerTurn);
        public IReadOnlyList<DeckSlotDefinition> Slots => slots;

        public int RegularDeckSize
        {
            get
            {
                int total = 0;
                if (slots == null)
                {
                    return total;
                }

                foreach (DeckSlotDefinition slot in slots)
                {
                    if (slot != null)
                    {
                        total += slot.Count;
                    }
                }

                return total;
            }
        }

        public bool IsRegularDeckSizeValid => RegularDeckSize > 0 && RegularDeckSize % DrawCountPerTurn == 0;

        public CardBase GetDefaultCardForSlot(DeckSlotType slotType)
        {
            if (slots == null)
            {
                return null;
            }

            foreach (DeckSlotDefinition slot in slots)
            {
                if (slot != null && slot.SlotType == slotType && slot.DefaultCard != null)
                {
                    return slot.DefaultCard;
                }
            }

            return null;
        }

        public bool CanCardUseSlot(CardBase card, DeckSlotType slotType)
        {
            DeckSlotDefinition slot = GetSlotDefinition(slotType);
            return slot != null ? slot.CanAccept(card) : card != null && card.CanUseInSlot(slotType);
        }

        public DeckSlotDefinition GetSlotDefinition(DeckSlotType slotType)
        {
            if (slots == null)
            {
                return null;
            }

            foreach (DeckSlotDefinition slot in slots)
            {
                if (slot != null && slot.SlotType == slotType)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
