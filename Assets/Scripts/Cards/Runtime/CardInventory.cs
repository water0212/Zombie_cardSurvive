using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public class CardInventory : MonoBehaviour
    {
        [SerializeField] private List<CardBase> startingCards = new List<CardBase>();

        private readonly List<CardBase> cards = new List<CardBase>();

        public IReadOnlyList<CardBase> Cards => cards;
        public int Count => cards.Count;

        private void Awake()
        {
            ResetInventory();
        }

        [ContextMenu("Reset Inventory")]
        public void ResetInventory()
        {
            cards.Clear();

            foreach (CardBase card in startingCards)
            {
                if (card != null)
                {
                    cards.Add(card);
                }
            }
        }

        public void AddCard(CardBase card)
        {
            if (card != null)
            {
                cards.Add(card);
            }
        }

        public bool RemoveCard(CardBase card)
        {
            return card != null && cards.Remove(card);
        }

        public bool Contains(CardBase card)
        {
            return card != null && cards.Contains(card);
        }
    }
}
