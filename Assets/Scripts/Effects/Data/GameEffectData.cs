using System;
using UnityEngine;
using UnityEngine.Serialization;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Threats.Data;

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
        [SerializeField] private EventStateEffectType eventStateEffectType;
        [SerializeField] private ThreatEffectType threatEffectType;
        [SerializeField] private CardBase cardData;
        [SerializeField] private RunFlagData runFlagData;
        [Tooltip("Fallback run flag key used when Run Flag Data is empty.")]
        [SerializeField] private string runFlagId;
        [SerializeField] private bool clampRunFlagToDefinition = true;
        [SerializeField] private int amount = 1;
        [Min(1)]
        [SerializeField] private int selectionCount = 1;
        [SerializeField] private CardSelectionFilterData cardSelectionFilter = new CardSelectionFilterData();
        [SerializeField] private string effectId;
        [SerializeField] private bool isPermanent = true;
        [SerializeField] private int remainingTurns;
        [Header("Event Scaling")]
        [SerializeField] private EventEffectScalingData eventScaling = new EventEffectScalingData();
        [SerializeField] private EventPaymentMode eventPaymentMode = EventPaymentMode.ChoiceCost;
        [Header("Horde")]
        [SerializeField] private HordeProfileData hordeProfile;
        [SerializeField] private int eventBonusBudget;
        [Header("Next Round Effect")]
        [FormerlySerializedAs("zombieConsequence")]
        [SerializeField] private NextRoundEffectData nextRoundEffect;
        [Header("Optional Building Condition")]
        [SerializeField] private string requiredBuildingId;
        [Min(0)]
        [SerializeField] private int requiredBuildingLevel;
        [SerializeField] private bool invertBuildingRequirement;

        public GameEffectCarrier Carrier => carrier;
        public GameEffectCategory Category => category;
        public ValueEffectType ValueEffectType => valueEffectType;
        public CardFlowEffectType CardFlowEffectType => cardFlowEffectType;
        public BuildingEffectType BuildingEffectType => buildingEffectType;
        public SurvivorEffectType SurvivorEffectType => survivorEffectType;
        public EventStateEffectType EventStateEffectType => eventStateEffectType;
        public ThreatEffectType ThreatEffectType => threatEffectType;
        public CardBase CardData => cardData;
        public RunFlagData RunFlagData => runFlagData;
        public string RunFlagId => runFlagData != null ? runFlagData.FlagId : runFlagId;
        public bool ClampRunFlagToDefinition => clampRunFlagToDefinition;
        public int Amount => amount;
        public int SelectionCount => Mathf.Max(1, selectionCount);
        public CardSelectionFilterData CardSelectionFilter => cardSelectionFilter;
        public string EffectId => effectId;
        public bool IsPermanent => isPermanent;
        public int RemainingTurns => Mathf.Max(0, remainingTurns);
        public EventEffectScalingData EventScaling => eventScaling;
        public EventPaymentMode EventPaymentMode => eventPaymentMode;
        public HordeProfileData HordeProfile => hordeProfile;
        public int EventBonusBudget => eventBonusBudget;
        public NextRoundEffectData NextRoundEffect => nextRoundEffect;
        public string RequiredBuildingId => requiredBuildingId;
        public int RequiredBuildingLevel => Mathf.Max(0, requiredBuildingLevel);
        public bool InvertBuildingRequirement => invertBuildingRequirement;

        public bool CanApply(GameEffectContext context)
        {
            return CanApply(context, amount);
        }

        /// <summary>檢查指定快照數值是否可套用；事件 Runtime 使用此多載。</summary>
        public bool CanApply(GameEffectContext context, int resolvedAmount)
        {
            if (!ShouldApplyForBuildingRequirement())
            {
                return true;
            }

            switch (category)
            {
                case GameEffectCategory.Value:
                    return CanApplyValueEffect(resolvedAmount);
                case GameEffectCategory.CardFlow:
                    return CanApplyCardFlowEffect(context);
                case GameEffectCategory.EventState:
                    return CanApplyEventStateEffect(context);
                case GameEffectCategory.Threat:
                    // Threat effects require a resolved event snapshot and are executed by
                    // ResolvedGameEffectRuntime rather than directly from the data asset.
                    return false;
                case GameEffectCategory.NextRoundEffect:
                    return nextRoundEffect != null && nextRoundEffect.HasExecutableEffects;
                default:
                    return true;
            }
        }

        public bool TryApply(GameEffectContext context)
        {
            return TryApply(context, amount);
        }

        /// <summary>套用指定快照數值；不會改寫 Asset 上的基礎 Amount。</summary>
        public bool TryApply(GameEffectContext context, int resolvedAmount)
        {
            if (!ShouldApplyForBuildingRequirement())
            {
                return true;
            }

            switch (category)
            {
                case GameEffectCategory.Value:
                    return TryApplyValueEffect(resolvedAmount);
                case GameEffectCategory.CardFlow:
                    return TryApplyCardFlowEffect(context, resolvedAmount);
                case GameEffectCategory.EventState:
                    return TryApplyEventStateEffect(context, resolvedAmount);
                case GameEffectCategory.Threat:
                    return false;
                case GameEffectCategory.NextRoundEffect:
                    return TryScheduleNextRoundEffect(context);
                default:
                    return true;
            }
        }

        public void Apply(GameEffectContext context)
        {
            TryApply(context);
        }

        public void Apply(GameEffectContext context, int resolvedAmount)
        {
            TryApply(context, resolvedAmount);
        }

        private bool CanApplyValueEffect(int resolvedAmount)
        {
            int value = Mathf.Max(0, resolvedAmount);
            switch (valueEffectType)
            {
                case ValueEffectType.SpendFood:
                    return IsForcedLoss || FoodSystem.AvailableFood >= value;
                case ValueEffectType.SpendResource:
                    return IsForcedLoss || ResourceSystem.AvailableResource >= value;
                case ValueEffectType.RemoveZombieThreat:
                    return CombatSystem.ZombieThreatCount >= value;
                default:
                    return true;
            }
        }

        private bool TryApplyValueEffect(int resolvedAmount)
        {
            switch (valueEffectType)
            {
                case ValueEffectType.AddFood:
                    FoodSystem.AddFood(resolvedAmount);
                    return true;
                case ValueEffectType.AddTemporaryFood:
                    FoodSystem.AddTemporaryFood(resolvedAmount);
                    return true;
                case ValueEffectType.AddRoundFoodConsumptionModifier:
                    FoodPlanSystem.AddTemporaryRoundConsumptionModifier(resolvedAmount);
                    return true;
                case ValueEffectType.SpendFood:
                    if (IsForcedLoss)
                    {
                        FoodSystem.ConsumePermanentFoodAllowShortage(resolvedAmount);
                        return true;
                    }

                    return FoodSystem.SpendFood(resolvedAmount);
                case ValueEffectType.AddFoodProduction:
                    FoodSystem.AddProductionEffect(effectId, resolvedAmount, isPermanent, RemainingTurns);
                    return true;
                case ValueEffectType.AddResource:
                    ResourceSystem.AddResource(resolvedAmount);
                    return true;
                case ValueEffectType.SpendResource:
                    if (IsForcedLoss)
                    {
                        ResourceSystem.ConsumeResourceAllowShortage(resolvedAmount);
                        return true;
                    }

                    return ResourceSystem.SpendResource(resolvedAmount);
                case ValueEffectType.AddResourceProduction:
                    ResourceSystem.AddProductionEffect(effectId, resolvedAmount, isPermanent, RemainingTurns);
                    return true;
                case ValueEffectType.AddZombieThreat:
                    CombatSystem.AddZombieThreat(resolvedAmount);
                    return true;
                case ValueEffectType.RemoveZombieThreat:
                    return CombatSystem.RemoveZombieThreat(resolvedAmount);
                case ValueEffectType.AddPendingDamage:
                    CombatSystem.AddPendingDamage(resolvedAmount);
                    return true;
                case ValueEffectType.ClearPendingDamage:
                    CombatSystem.ClearPendingDamage();
                    return true;
                case ValueEffectType.AddCurrentEnergy:
                    EnergySystem.AddEnergy(resolvedAmount);
                    return true;
                case ValueEffectType.IncreaseMaxEnergy:
                    EnergySystem.IncreaseMaxEnergy(resolvedAmount);
                    return true;
                case ValueEffectType.DecreaseMaxEnergy:
                    EnergySystem.DecreaseMaxEnergy(resolvedAmount);
                    return true;
                case ValueEffectType.RefillEnergy:
                    EnergySystem.RefillToMax();
                    return true;
                case ValueEffectType.AddMorale:
                    MoraleSystem.AddMorale(resolvedAmount);
                    return true;
                case ValueEffectType.ReduceMorale:
                    MoraleSystem.ReduceMorale(resolvedAmount);
                    return true;
                default:
                    return true;
            }
        }

        private static bool CanApplyCardFlowEffect(GameEffectContext context)
        {
            return context.CardController != null;
        }

        private bool TryApplyCardFlowEffect(GameEffectContext context, int resolvedAmount)
        {
            if (context.CardController == null)
            {
                return false;
            }

            switch (cardFlowEffectType)
            {
                case CardFlowEffectType.DrawCards:
                    context.CardController.DrawCards(resolvedAmount);
                    return true;
                case CardFlowEffectType.GrantReplacementChance:
                    context.CardController.GrantPlanningReplacement(resolvedAmount);
                    return true;
                case CardFlowEffectType.AddCardToDiscardPile:
                    bool addedAny = false;
                    for (int i = 0; i < Mathf.Max(0, resolvedAmount); i++)
                    {
                        addedAny |= context.CardController.AddCardToDiscardPile(cardData);
                    }

                    return addedAny;
                case CardFlowEffectType.ReduceOtherHandEnergyCosts:
                    context.CardController.ReduceOtherHandEnergyCosts(
                        cardSelectionFilter?.CreateRuntimeFilter(),
                        context.Card,
                        resolvedAmount);
                    return true;
                case CardFlowEffectType.ReduceDrawPileEnergyCosts:
                    context.CardController.ReduceDrawPileEnergyCosts(
                        cardSelectionFilter?.CreateRuntimeFilter(),
                        context.Card,
                        SelectionCount,
                        resolvedAmount);
                    return true;
                default:
                    return true;
            }
        }

        private bool CanApplyEventStateEffect(GameEffectContext context)
        {
            return context.RunFlags != null && !string.IsNullOrWhiteSpace(RunFlagId);
        }

        private bool TryApplyEventStateEffect(GameEffectContext context, int resolvedAmount)
        {
            RunFlagRuntimeStore runFlags = context.RunFlags;
            if (runFlags == null || string.IsNullOrWhiteSpace(RunFlagId))
            {
                return false;
            }

            int day = Mathf.Max(1, context.EventDay);
            switch (eventStateEffectType)
            {
                case EventStateEffectType.SetRunFlagValue:
                    if (runFlagData != null)
                    {
                        runFlags.SetValue(runFlagData, resolvedAmount, day, clampRunFlagToDefinition);
                    }
                    else
                    {
                        runFlags.SetValue(RunFlagId, resolvedAmount, day);
                    }

                    return true;
                case EventStateEffectType.AddRunFlagValue:
                    if (runFlagData != null)
                    {
                        runFlags.AddValue(runFlagData, resolvedAmount, day, clampRunFlagToDefinition);
                    }
                    else
                    {
                        runFlags.AddValue(RunFlagId, resolvedAmount, day);
                    }

                    return true;
                case EventStateEffectType.ClearRunFlag:
                    if (runFlagData != null)
                    {
                        runFlags.Clear(runFlagData, day);
                    }
                    else
                    {
                        runFlags.Clear(RunFlagId, day);
                    }

                    return true;
                default:
                    return true;
            }
        }

        private bool TryScheduleNextRoundEffect(GameEffectContext context)
        {
            if (nextRoundEffect == null || !nextRoundEffect.HasExecutableEffects)
            {
                return false;
            }

            NextRoundEffectSourceKind sourceKind;
            string sourceId;
            switch (context.Carrier)
            {
                case GameEffectCarrier.Card:
                    sourceKind = NextRoundEffectSourceKind.Card;
                    sourceId = context.Card != null ? context.Card.RuntimeId : "Card";
                    break;
                case GameEffectCarrier.Event:
                    sourceKind = NextRoundEffectSourceKind.Event;
                    sourceId = context.EventDay > 0 ? $"EventDay:{context.EventDay}" : "Event";
                    break;
                default:
                    sourceKind = NextRoundEffectSourceKind.Other;
                    sourceId = "Other";
                    break;
            }

            return NextRoundEffectSystem.TryScheduleForNextRound(
                nextRoundEffect,
                sourceKind,
                sourceId,
                out _);
        }

        private bool ShouldApplyForBuildingRequirement()
        {
            if (string.IsNullOrWhiteSpace(requiredBuildingId))
            {
                return true;
            }

            bool met = BuildingSystem.HasActiveBuildingLevel(requiredBuildingId, RequiredBuildingLevel);
            return invertBuildingRequirement ? !met : met;
        }

        public bool IsResourceSpendEffect => category == GameEffectCategory.Value
            && (valueEffectType == ValueEffectType.SpendFood
                || valueEffectType == ValueEffectType.SpendResource);

        public bool IsEventResourcePayment => carrier == GameEffectCarrier.Event
            && IsResourceSpendEffect;

        public bool IsChoiceCost => IsEventResourcePayment
            && eventPaymentMode == EventPaymentMode.ChoiceCost;

        public bool IsForcedLoss => IsEventResourcePayment
            && eventPaymentMode == EventPaymentMode.ForcedLoss;

        public bool IsMoraleEffect => category == GameEffectCategory.Value
            && (valueEffectType == ValueEffectType.AddMorale
                || valueEffectType == ValueEffectType.ReduceMorale);

        public int ResolveMoraleDelta(int resolvedAmount)
        {
            int magnitude = Mathf.Max(0, resolvedAmount);
            return valueEffectType == ValueEffectType.ReduceMorale ? -magnitude : magnitude;
        }

        public bool IsSpawnHorde => carrier == GameEffectCarrier.Event
            && category == GameEffectCategory.Threat
            && threatEffectType == ThreatEffectType.SpawnHorde;

        public bool IsNextRoundEffect => category == GameEffectCategory.NextRoundEffect;

        public bool ShouldApplyForCurrentState()
        {
            return ShouldApplyForBuildingRequirement();
        }
    }
}
