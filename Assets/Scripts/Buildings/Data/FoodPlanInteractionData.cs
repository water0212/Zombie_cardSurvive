using UnityEngine;

namespace ZombieCardSurvive.Buildings.Data
{
    [CreateAssetMenu(fileName = "FoodPlanInteraction", menuName = "Zombie Card Survive/Buildings/Food Plan Interaction")]
    public class FoodPlanInteractionData : BuildingInteractionData
    {
        [TextArea]
        [SerializeField] private string description;

        public string Description => description;
    }
}
