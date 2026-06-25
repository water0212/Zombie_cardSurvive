using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "Card", menuName = "Zombie Card Survive/Cards/Card")]
    public class CardBase : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField] private int energyCost = 1;
        [SerializeField] private Sprite artwork;
        [SerializeField] private Sprite background;
        [TextArea]
        [SerializeField] private string description;
        [Header("Rules")]
        [SerializeField] private CardType cardType = CardType.Special;
        [SerializeField] private CardPlayMode playMode = CardPlayMode.Playable;
        [SerializeField] private List<DeckSlotType> allowedSlots = new List<DeckSlotType>();
        [SerializeField] private bool isAdditiveCard;
        [SerializeField] private bool drawReplacementOnAutoResolve;
        [SerializeField] private bool hasLimitedUses;
        [SerializeField] private int maxUsesPerRun = 1;

        [Header("Effects")]
        [SerializeField] private List<GameEffectData> costs = new List<GameEffectData>();
        [SerializeField] private List<GameEffectData> effects = new List<GameEffectData>();

        public string CardId => cardId;
        public string DisplayName => displayName;
        public int EnergyCost => Mathf.Max(0, energyCost);
        public Sprite Artwork => artwork;
        public Sprite Background => background;
        public string Description => description;
        public CardType CardType => cardType;
        public CardType EffectiveCardType => ResolveEffectiveCardType();
        public CardPlayMode PlayMode => playMode;
        public CardPlayMode EffectivePlayMode => ResolveEffectivePlayMode();
        public IReadOnlyList<DeckSlotType> AllowedSlots => allowedSlots;
        public bool IsAdditiveCard => isAdditiveCard;
        public bool EffectiveIsAdditiveCard => isAdditiveCard || this is ZombieCardData;
        public bool DrawReplacementOnAutoResolve => drawReplacementOnAutoResolve;
        public bool EffectiveDrawReplacementOnAutoResolve => drawReplacementOnAutoResolve || this is ZombieCardData;
        public bool HasLimitedUses => hasLimitedUses;
        public int MaxUsesPerRun => Mathf.Max(1, maxUsesPerRun);
        public IReadOnlyList<GameEffectData> Costs => costs;
        public IReadOnlyList<GameEffectData> Effects => effects;
        public string Id => cardId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                cardId = AutoIdUtility.CreateId("card");
            }
        }

        public void RegenerateId()
        {
            cardId = AutoIdUtility.CreateId("card");
        }

        public CardRuntime CreateRuntimeCard()
        {
            return new CardRuntime(this);
        }

        public bool CanUseInSlot(DeckSlotType slotType)
        {
            return allowedSlots == null || allowedSlots.Count == 0 || allowedSlots.Contains(slotType);
        }

        private CardType ResolveEffectiveCardType()
        {
            if (this is FoodCardData)
            {
                return ZombieCardSurvive.Cards.Data.CardType.Food;
            }

            if (this is ResourceCardData)
            {
                return ZombieCardSurvive.Cards.Data.CardType.Resource;
            }

            if (this is CombatCardData)
            {
                return ZombieCardSurvive.Cards.Data.CardType.Combat;
            }

            if (this is ZombieCardData)
            {
                return ZombieCardSurvive.Cards.Data.CardType.Zombie;
            }

            if (this is EnergyCard)
            {
                return ZombieCardSurvive.Cards.Data.CardType.Special;
            }

            return cardType;
        }

        private CardPlayMode ResolveEffectivePlayMode()
        {
            if (this is ZombieCardData)
            {
                return CardPlayMode.AutoResolveOnDraw;
            }

            if (EffectiveCardType == ZombieCardSurvive.Cards.Data.CardType.Wound)
            {
                return CardPlayMode.Unplayable;
            }

            return playMode;
        }

        public bool CanPayCosts(CardPlayContext context)
        {
            GameEffectContext effectContext = GameEffectContext.ForCard(context);
            int requiredFood = 0;
            int requiredResource = 0;
            int requiredZombieThreat = 0;

            foreach (GameEffectData cost in costs)
            {
                if (cost != null && !cost.CanApply(effectContext))
                {
                    return false;
                }

                AddRequiredValues(cost, ref requiredFood, ref requiredResource, ref requiredZombieThreat);
            }

            return context.Food >= requiredFood
                && context.Resource >= requiredResource
                && context.ZombieThreatCount >= requiredZombieThreat;
        }

        public bool PayCosts(CardPlayContext context)
        {
            if (!CanPayCosts(context))
            {
                return false;
            }

            GameEffectContext effectContext = GameEffectContext.ForCard(context);
            foreach (GameEffectData cost in costs)
            {
                if (cost != null && !cost.TryApply(effectContext))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddRequiredValues(GameEffectData cost, ref int requiredFood, ref int requiredResource, ref int requiredZombieThreat)
        {
            if (cost == null || cost.Category != GameEffectCategory.Value)
            {
                return;
            }

            int amount = Mathf.Max(0, cost.Amount);
            switch (cost.ValueEffectType)
            {
                case ValueEffectType.SpendFood:
                    requiredFood += amount;
                    break;
                case ValueEffectType.SpendResource:
                    requiredResource += amount;
                    break;
                case ValueEffectType.RemoveZombieThreat:
                    requiredZombieThreat += amount;
                    break;
            }
        }

        public virtual void Resolve(CardPlayContext context)
        {
            GameEffectContext effectContext = GameEffectContext.ForCard(context);
            foreach (GameEffectData effect in effects)
            {
                effect?.Apply(effectContext);
            }
        }
    }
}
