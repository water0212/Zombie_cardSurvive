using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;
namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "incomeResourceCard", menuName = "Zombie Card Survive/Cards/Test/Resource Card")]
    public class incomeResourceCard : CardBase
    {
        
            [SerializeField] private int resourceAmountTurn = 3;
            [SerializeField] private int resourceAmount = 3;
            public int ResourceAmountTurn => Mathf.Max(0, resourceAmountTurn);
            public int ResourceAmount => Mathf.Max(0, resourceAmount);

            public override void Resolve(CardPlayContext context)
            {
                ResourceSystem.AddProductionEffect("incomeResourceCard_" + GetInstanceID(), ResourceAmount, false, ResourceAmountTurn);
                base.Resolve(context);
            }

    }
    
}

