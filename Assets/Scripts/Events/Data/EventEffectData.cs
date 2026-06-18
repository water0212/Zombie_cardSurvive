using System;
using UnityEngine;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Events.Data
{
#pragma warning disable 0618
    [Obsolete("Use ZombieCardSurvive.Effects.Data.GameEffectData instead.")]
    [Serializable]
    public class EventEffectData
    {
        [SerializeField] private EventEffectType effectType;
        [SerializeField] private int amount = 1;
        [SerializeField] private string effectId;
        [SerializeField] private bool isPermanent = true;
        [SerializeField] private int remainingTurns;

        public EventEffectType EffectType => effectType;
        public int Amount => amount;
        public string EffectId => effectId;
        public bool IsPermanent => isPermanent;
        public int RemainingTurns => Mathf.Max(0, remainingTurns);

        public void Apply()
        {
            switch (effectType)
            {
                case EventEffectType.AddFood:
                    FoodSystem.AddFood(amount);
                    break;
                case EventEffectType.SpendFood:
                    FoodSystem.SpendFood(amount);
                    break;
                case EventEffectType.AddFoodProduction:
                    FoodSystem.AddProductionEffect(effectId, amount, isPermanent, RemainingTurns);
                    break;
                case EventEffectType.AddResource:
                    ResourceSystem.AddResource(amount);
                    break;
                case EventEffectType.SpendResource:
                    ResourceSystem.SpendResource(amount);
                    break;
                case EventEffectType.AddResourceProduction:
                    ResourceSystem.AddProductionEffect(effectId, amount, isPermanent, RemainingTurns);
                    break;
                case EventEffectType.AddZombieThreat:
                    CombatSystem.AddZombieThreat(amount);
                    break;
                case EventEffectType.RemoveZombieThreat:
                    CombatSystem.RemoveZombieThreat(amount);
                    break;
                case EventEffectType.AddPendingDamage:
                    CombatSystem.AddPendingDamage(amount);
                    break;
                case EventEffectType.ClearPendingDamage:
                    CombatSystem.ClearPendingDamage();
                    break;
                case EventEffectType.AddCurrentEnergy:
                    EnergySystem.AddEnergy(amount);
                    break;
                case EventEffectType.IncreaseMaxEnergy:
                    EnergySystem.IncreaseMaxEnergy(amount);
                    break;
                case EventEffectType.DecreaseMaxEnergy:
                    EnergySystem.DecreaseMaxEnergy(amount);
                    break;
                case EventEffectType.RefillEnergy:
                    EnergySystem.RefillToMax();
                    break;
            }
        }
    }
#pragma warning restore 0618
}
