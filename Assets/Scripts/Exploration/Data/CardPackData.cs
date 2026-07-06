using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Exploration.Data
{
    [CreateAssetMenu(fileName = "CardPack", menuName = "Zombie Card Survive/Exploration/Card Pack")]
    public class CardPackData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string packId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite artwork;
        [SerializeField] private CardPackType packType = CardPackType.MixedSurvivalPack;
        [SerializeField] private int cardsToGenerate = 2;
        [SerializeField] private List<CardPackEntry> cardPool = new List<CardPackEntry>();

        public string PackId => packId;
        public string DisplayName => displayName;
        public Sprite Artwork => artwork;
        public CardPackType PackType => packType;
        public int CardsToGenerate => Mathf.Max(0, cardsToGenerate);
        public IReadOnlyList<CardPackEntry> CardPool => cardPool;
        public string Id => packId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(packId))
            {
                packId = AutoIdUtility.CreateId("pack");
            }
        }

        public void RegenerateId()
        {
            packId = AutoIdUtility.CreateId("pack");
        }

        public List<CardBase> GenerateCards()
        {
            List<CardBase> cards = new List<CardBase>();
            int drawCount = CardsToGenerate;
            for (int i = 0; i < drawCount; i++)
            {
                CardBase card = SelectWeightedCard();
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }

        private CardBase SelectWeightedCard()
        {
            int totalWeight = 0;
            foreach (CardPackEntry entry in cardPool)
            {
                if (entry != null && entry.Card != null)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (CardPackEntry entry in cardPool)
            {
                if (entry == null || entry.Card == null)
                {
                    continue;
                }

                cumulative += entry.Weight;
                if (roll < cumulative)
                {
                    return entry.Card;
                }
            }

            return null;
        }

        public bool IsZombiePollution(CardBase card)
        {
            if (card == null)
            {
                return false;
            }

            foreach (CardPackEntry entry in cardPool)
            {
                if (entry != null && ReferenceEquals(entry.Card, card))
                {
                    return entry.IsZombiePollution;
                }
            }

            return card is ZombieCardData;
        }
    }
}
