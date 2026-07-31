using UnityEngine;

namespace ZombieCardSurvive.Buildings.Data
{
    [CreateAssetMenu(fileName = "FoodStorageBonus", menuName = "Zombie Card Survive/Buildings/Food Storage Bonus")]
    public class FoodStorageInteractionData : BuildingInteractionData
    {
        [Min(0)]
        [SerializeField] private int foodCapacityBonus;

        public int FoodCapacityBonus => Mathf.Max(0, foodCapacityBonus);
    }
}
