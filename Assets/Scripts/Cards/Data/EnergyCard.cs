using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;
namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "EnergyCard", menuName = "Zombie Card Survive/Cards/Test/Energy Card")]
    public class EnergyCard : CardBase
    {
        
            [SerializeField] private int energyAmount = 2;
            public int EnergyAmount => Mathf.Max(0, energyAmount);

            public override void Resolve(CardPlayContext context)
            {
                EnergySystem.AddEnergy(EnergyAmount);
                base.Resolve(context);
            }

    }
    
}

