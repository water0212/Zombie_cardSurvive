using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Buildings.Data
{
    [CreateAssetMenu(fileName = "CardUpgradeInteraction", menuName = "Zombie Card Survive/Buildings/Card Upgrade Interaction")]
    public class CardUpgradeInteractionData : BuildingInteractionData
    {
        [SerializeField] private CardBase sourceCard;
        [SerializeField] private CardBase upgradedCard;
        [SerializeField] private ResourceCost cost;
        [TextArea]
        [SerializeField] private string description;

        public CardBase SourceCard => sourceCard;
        public CardBase UpgradedCard => upgradedCard;
        public ResourceCost Cost => cost;
        public string Description => description;
    }
}
