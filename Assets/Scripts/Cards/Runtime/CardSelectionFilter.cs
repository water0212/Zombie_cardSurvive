using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    public sealed class CardSelectionFilter
    {
        private readonly List<CardType> allowedCardTypes = new List<CardType>();
        private readonly List<CardRarity> allowedCardRarities = new List<CardRarity>();

        public CardSelectionFilter(
            bool useCardTypeFilter,
            IEnumerable<CardType> cardTypes,
            bool useCardRarityFilter,
            IEnumerable<CardRarity> cardRarities,
            bool useEnergyCostFilter,
            CardCostFilterMode energyCostFilterMode,
            int energyCost)
        {
            UseCardTypeFilter = useCardTypeFilter;
            CopyValues(cardTypes, allowedCardTypes);
            UseCardRarityFilter = useCardRarityFilter;
            CopyValues(cardRarities, allowedCardRarities);
            UseEnergyCostFilter = useEnergyCostFilter;
            EnergyCostFilterMode = energyCostFilterMode;
            EnergyCost = Mathf.Max(0, energyCost);
        }

        public bool UseCardTypeFilter { get; }
        public IReadOnlyList<CardType> AllowedCardTypes => allowedCardTypes;
        public bool UseCardRarityFilter { get; }
        public IReadOnlyList<CardRarity> AllowedCardRarities => allowedCardRarities;
        public bool UseEnergyCostFilter { get; }
        public CardCostFilterMode EnergyCostFilterMode { get; }
        public int EnergyCost { get; }

        public bool Allows(CardRuntime card)
        {
            return card != null && Allows(card.Data, card.EffectiveEnergyCost);
        }

        public bool Allows(CardBase cardData)
        {
            return cardData != null && Allows(cardData, cardData.EnergyCost);
        }

        private bool Allows(CardBase cardData, int energyCost)
        {
            if (cardData == null)
            {
                return false;
            }

            if (UseCardTypeFilter && !allowedCardTypes.Contains(cardData.EffectiveCardType))
            {
                return false;
            }

            if (UseCardRarityFilter && !allowedCardRarities.Contains(cardData.Rarity))
            {
                return false;
            }

            if (!UseEnergyCostFilter)
            {
                return true;
            }

            switch (EnergyCostFilterMode)
            {
                case CardCostFilterMode.GreaterThan:
                    return energyCost > EnergyCost;
                case CardCostFilterMode.LessThan:
                    return energyCost < EnergyCost;
                case CardCostFilterMode.Equal:
                default:
                    return energyCost == EnergyCost;
            }
        }

        private static void CopyValues<T>(IEnumerable<T> source, List<T> target)
        {
            if (source == null)
            {
                return;
            }

            foreach (T value in source)
            {
                if (!target.Contains(value))
                {
                    target.Add(value);
                }
            }
        }
    }
}
