using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;
namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "incomeFoodCard", menuName = "Zombie Card Survive/Cards/Test/Food Card")]
    public class incomeFoodCard : CardBase
    {
        
            [SerializeField] private int foodAmountTurn = 3;
            [SerializeField] private int foodAmount = 3;
            public int FoodAmountTurn => Mathf.Max(0, foodAmountTurn);
            public int FoodAmount => Mathf.Max(0, foodAmount);

            public override void Resolve(CardPlayContext context)
            {
                FoodSystem.AddProductionEffect("incomeFoodCard_" + GetInstanceID(), FoodAmount, false, FoodAmountTurn);
                base.Resolve(context);
            }

    }
    
}

