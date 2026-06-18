using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "CombatCard", menuName = "Zombie Card Survive/Cards/Combat Card")]
    public class CombatCardData : CardBase
    {
        [SerializeField] private int damage = 1;

        public int Damage => Mathf.Max(0, damage);

        public override void Resolve(CardPlayContext context)
        {
            CombatSystem.AddPendingDamage(Damage);
            base.Resolve(context);
        }
    }
}
