using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "ZombieCard", menuName = "Zombie Card Survive/Cards/Zombie Card")]
    public class ZombieCardData : CardBase
    {
        [SerializeField] private int health = 1;
        [SerializeField] private int attack = 1;

        public int Health => Mathf.Max(1, health);
        public int Attack => Mathf.Max(0, attack);

        public override void Resolve(CardPlayContext context)
        {
            CombatSystem.AddZombieThreat(1);
            context.Controller.DrawCards(1);
            base.Resolve(context);
        }
    }
}
