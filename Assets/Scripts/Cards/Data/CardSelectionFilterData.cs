using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.Data
{
    [Serializable]
    public sealed class CardSelectionFilterData
    {
        [SerializeField] private bool useCardTypeFilter;
        [SerializeField] private List<CardType> allowedCardTypes = new List<CardType>();

        [SerializeField] private bool useCardRarityFilter;
        [SerializeField] private List<CardRarity> allowedCardRarities = new List<CardRarity>
        {
            CardRarity.Common,
            CardRarity.Uncommon,
            CardRarity.Rare
        };

        [SerializeField] private bool useEnergyCostFilter;
        [SerializeField] private CardCostFilterMode energyCostFilterMode = CardCostFilterMode.Equal;
        [Min(0)]
        [SerializeField] private int energyCost;

        public bool UseCardTypeFilter => useCardTypeFilter;
        public IReadOnlyList<CardType> AllowedCardTypes => allowedCardTypes;
        public bool UseCardRarityFilter => useCardRarityFilter;
        public IReadOnlyList<CardRarity> AllowedCardRarities => allowedCardRarities;
        public bool UseEnergyCostFilter => useEnergyCostFilter;
        public CardCostFilterMode EnergyCostFilterMode => energyCostFilterMode;
        public int EnergyCost => Mathf.Max(0, energyCost);

        public CardSelectionFilter CreateRuntimeFilter()
        {
            return new CardSelectionFilter(
                useCardTypeFilter,
                allowedCardTypes,
                useCardRarityFilter,
                allowedCardRarities,
                useEnergyCostFilter,
                energyCostFilterMode,
                EnergyCost);
        }
    }
}
