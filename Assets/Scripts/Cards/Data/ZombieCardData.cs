using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Systems;
using UnityEngine.Serialization;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "ZombieCard", menuName = "Zombie Card Survive/Cards/Zombie Card")]
    public class ZombieCardData : CardBase
    {
        [SerializeField] private int health = 1;
        [SerializeField] private int attack = 1;
        [Min(0)]
        [SerializeField] private int permanentMaximumMoraleDamage;
        [FormerlySerializedAs("survivalConsequence")]
        [SerializeField] private NextRoundEffectData survivalNextRoundEffect;

        public int Health => Mathf.Max(1, health);
        public int Attack => Mathf.Max(0, attack);
        public int PermanentMaximumMoraleDamage => Mathf.Max(0, permanentMaximumMoraleDamage);
        public NextRoundEffectData SurvivalNextRoundEffect => survivalNextRoundEffect;

        public override void Resolve(CardPlayContext context)
        {
            CombatSystem.SpawnZombieThreat(this);
            base.Resolve(context);
        }
    }
}
