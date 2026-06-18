using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "FoodCard", menuName = "Zombie Card Survive/Cards/Food Card")]
    public class FoodCardData : CardBase
    {
        [SerializeField] private int foodAmount = 1;

        public int FoodAmount => Mathf.Max(0, foodAmount);

        public override void Resolve(CardPlayContext context)
        {
            FoodSystem.AddFood(FoodAmount);
            base.Resolve(context);
        }
    }
}
