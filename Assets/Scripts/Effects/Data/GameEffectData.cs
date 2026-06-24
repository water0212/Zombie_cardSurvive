using System;
using UnityEngine;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Effects.Data
{
    [Serializable]
    public class GameEffectData
    {
        [SerializeField] private GameEffectCarrier carrier = GameEffectCarrier.Card;
        [SerializeField] private GameEffectCategory category = GameEffectCategory.Value;
        [SerializeField] private ValueEffectType valueEffectType;
        [SerializeField] private CardFlowEffectType cardFlowEffectType;
        [SerializeField] private BuildingEffectType buildingEffectType;
        [SerializeField] private SurvivorEffectType survivorEffectType;
        [SerializeField] private int amount = 1;
        [SerializeField] private string effectId;
        [SerializeField] private bool isPermanent = true;
        [SerializeField] private int remainingTurns;

        public GameEffectCarrier Carrier => carrier;
        public GameEffectCategory Category => category;
        public ValueEffectType ValueEffectType => valueEffectType;
        public CardFlowEffectType CardFlowEffectType => cardFlowEffectType;
        public BuildingEffectType BuildingEffectType => buildingEffectType;
        public SurvivorEffectType SurvivorEffectType => survivorEffectType;
        public int Amount => amount;
        public string EffectId => effectId;
        public bool IsPermanent => isPermanent;
        public int RemainingTurns => Mathf.Max(0, remainingTurns);

        public bool CanApply(GameEffectContext context)
        {
            switch (category)
            {
                case GameEffectCategory.Value:
                    return CanApplyValueEffect();
                case GameEffectCategory.CardFlow:
                    return CanApplyCardFlowEffect(context);
                default:
                    return true;
            }
        }

        public bool TryApply(GameEffectContext context)
        {
            switch (category)
            {
                case GameEffectCategory.Value:
                    return TryApplyValueEffect();
                case GameEffectCategory.CardFlow:
                    return TryApplyCardFlowEffect(context);
                default:
                    return true;
            }
        }

        public void Apply(GameEffectContext context)
        {
            TryApply(context);
        }

        private bool CanApplyValueEffect()
        {
            int value = Mathf.Max(0, amount);
            switch (valueEffectType)
            {
                case ValueEffectType.SpendFood:
                    return FoodSystem.GetFood() >= value;
                case ValueEffectType.SpendResource:
                    return ResourceSystem.GetResource() >= value;
                case ValueEffectType.RemoveZombieThreat:
                    return CombatSystem.ZombieThreatCount >= value;
                default:
                    return true;
            }
        }

        private bool TryApplyValueEffect()
        {
            switch (valueEffectType)
            {
                case ValueEffectType.AddFood:
                    FoodSystem.AddFood(amount);
                    return true;
                case ValueEffectType.SpendFood:
                    return FoodSystem.SpendFood(amount);
                case ValueEffectType.AddFoodProduction:
                    FoodSystem.AddProductionEffect(effectId, amount, isPermanent, RemainingTurns);
                    return true;
                case ValueEffectType.AddResource:
                    ResourceSystem.AddResource(amount);
                    return true;
                case ValueEffectType.SpendResource:
                    return ResourceSystem.SpendResource(amount);
                case ValueEffectType.AddResourceProduction:
                    ResourceSystem.AddProductionEffect(effectId, amount, isPermanent, RemainingTurns);
                    return true;
                case ValueEffectType.AddZombieThreat:
                    CombatSystem.AddZombieThreat(amount);
                    return true;
                case ValueEffectType.RemoveZombieThreat:
                    return CombatSystem.RemoveZombieThreat(amount);
                case ValueEffectType.AddPendingDamage:
                    CombatSystem.AddPendingDamage(amount);
                    return true;
                case ValueEffectType.ClearPendingDamage:
                    CombatSystem.ClearPendingDamage();
                    return true;
                case ValueEffectType.AddCurrentEnergy:
                    EnergySystem.AddEnergy(amount);
                    return true;
                case ValueEffectType.IncreaseMaxEnergy:
                    EnergySystem.IncreaseMaxEnergy(amount);
                    return true;
                case ValueEffectType.DecreaseMaxEnergy:
                    EnergySystem.DecreaseMaxEnergy(amount);
                    return true;
                case ValueEffectType.RefillEnergy:
                    EnergySystem.RefillToMax();
                    return true;
                case ValueEffectType.AddMorale:
                    MoraleSystem.AddMorale(amount);
                    return true;
                case ValueEffectType.ReduceMorale:
                    MoraleSystem.ReduceMorale(amount);
                    return true;
                default:
                    return true;
            }
        }

        private static bool CanApplyCardFlowEffect(GameEffectContext context)
        {
            return context.CardController != null;
        }

        private bool TryApplyCardFlowEffect(GameEffectContext context)
        {
            if (context.CardController == null)
            {
                return false;
            }

            switch (cardFlowEffectType)
            {
                case CardFlowEffectType.DrawCards:
                    context.CardController.DrawCards(amount);
                    return true;
                default:
                    return true;
            }
        }
    }
}
