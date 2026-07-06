using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "ConstructionCard", menuName = "Zombie Card Survive/Cards/Construction Card")]
    public class ConstructionCardData : CardBase
    {
        public override bool CanUseInSlot(DeckSlotType slotType)
        {
            return slotType == DeckSlotType.Unrestricted;
        }

        public override void Resolve(CardPlayContext context)
        {
            base.Resolve(context);
            if (!BuildingSystem.ApplyConstructionProgress(context.Card.RuntimeId))
            {
                Debug.LogWarning($"Construction card '{name}' has no active building job for runtime id '{context.Card.RuntimeId}'.");
            }
        }
    }
}
