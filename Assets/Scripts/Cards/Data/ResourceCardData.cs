using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "ResourceCard", menuName = "Zombie Card Survive/Cards/Resource Card")]
    public class ResourceCardData : CardBase
    {
        [SerializeField] private int resourceAmount = 1;

        public int ResourceAmount => Mathf.Max(0, resourceAmount);

        public override void Resolve(CardPlayContext context)
        {
            ResourceSystem.AddResource(ResourceAmount);
            base.Resolve(context);
        }
    }
}
