#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Editor
{
    /// <summary>PB-P2-009D 的第一版正式屍群內容規格。</summary>
    public static class HordeContentV1Specification
    {
        public const string StandardProfileId = "HORDE-STANDARD-01";
        public const string LegacyVisualSourcePath =
            "Assets/ScriptObj/CardScriptObj/NewCard/ZombieCard.asset";

        private static readonly HordeZombieContentSpec[] zombies =
        {
            new HordeZombieContentSpec(
                "ZMB-001",
                "遊蕩殭屍",
                "動作緩慢但數量眾多，是早期屍群的主要壓力。",
                CardRarity.Common,
                health: 1,
                attack: 1,
                budgetCost: 1,
                baseWeight: 60,
                minWorldThreatTier: 1,
                maxWorldThreatTier: 0,
                maxCountPerHorde: 6),
            new HordeZombieContentSpec(
                "ZMB-002",
                "毀壞殭屍",
                "殘破的身體仍相當耐打，會在世界威脅升高後混入屍群。",
                CardRarity.Common,
                health: 2,
                attack: 1,
                budgetCost: 2,
                baseWeight: 30,
                minWorldThreatTier: 2,
                maxWorldThreatTier: 0,
                maxCountPerHorde: 3),
            new HordeZombieContentSpec(
                "ZMB-003",
                "狂暴殭屍",
                "速度與攻擊性明顯更高，只會在較高世界威脅下出現。",
                CardRarity.Uncommon,
                health: 3,
                attack: 2,
                budgetCost: 4,
                baseWeight: 10,
                minWorldThreatTier: 3,
                maxWorldThreatTier: 0,
                maxCountPerHorde: 2)
        };

        private static readonly HordeProfileContentSpec[] profiles =
        {
            new HordeProfileContentSpec(
                StandardProfileId,
                "標準基地屍群",
                "EVT-014 使用的首版基地屍群。預算同時購買數量與體質，並限制單次 UI 壓力。",
                HordeCompositionStyle.Mixed,
                baseBudget: 3,
                worldThreatBudgetPerTier: 2,
                baseAttentionBudgetPerPoint: 1f,
                regionPressureBudgetMultiplier: 4f,
                minimumBudget: 3,
                maximumBudget: 10,
                zombieIds: new[] { "ZMB-001", "ZMB-002", "ZMB-003" })
        };

        public static IReadOnlyList<HordeZombieContentSpec> Zombies => zombies;
        public static IReadOnlyList<HordeProfileContentSpec> Profiles => profiles;
    }

    public sealed class HordeZombieContentSpec
    {
        public HordeZombieContentSpec(
            string zombieId,
            string displayName,
            string description,
            CardRarity rarity,
            int health,
            int attack,
            int budgetCost,
            int baseWeight,
            int minWorldThreatTier,
            int maxWorldThreatTier,
            int maxCountPerHorde)
        {
            ZombieId = zombieId;
            DisplayName = displayName;
            Description = description;
            Rarity = rarity;
            Health = health;
            Attack = attack;
            BudgetCost = budgetCost;
            BaseWeight = baseWeight;
            MinWorldThreatTier = minWorldThreatTier;
            MaxWorldThreatTier = maxWorldThreatTier;
            MaxCountPerHorde = maxCountPerHorde;
        }

        public string ZombieId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public CardRarity Rarity { get; }
        public int Health { get; }
        public int Attack { get; }
        public int BudgetCost { get; }
        public int BaseWeight { get; }
        public int MinWorldThreatTier { get; }
        public int MaxWorldThreatTier { get; }
        public int MaxCountPerHorde { get; }
    }

    public sealed class HordeProfileContentSpec
    {
        public HordeProfileContentSpec(
            string profileId,
            string displayName,
            string description,
            HordeCompositionStyle compositionStyle,
            int baseBudget,
            int worldThreatBudgetPerTier,
            float baseAttentionBudgetPerPoint,
            float regionPressureBudgetMultiplier,
            int minimumBudget,
            int maximumBudget,
            IReadOnlyList<string> zombieIds)
        {
            ProfileId = profileId;
            DisplayName = displayName;
            Description = description;
            CompositionStyle = compositionStyle;
            BaseBudget = baseBudget;
            WorldThreatBudgetPerTier = worldThreatBudgetPerTier;
            BaseAttentionBudgetPerPoint = baseAttentionBudgetPerPoint;
            RegionPressureBudgetMultiplier = regionPressureBudgetMultiplier;
            MinimumBudget = minimumBudget;
            MaximumBudget = maximumBudget;
            ZombieIds = zombieIds ?? Array.Empty<string>();
        }

        public string ProfileId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public HordeCompositionStyle CompositionStyle { get; }
        public int BaseBudget { get; }
        public int WorldThreatBudgetPerTier { get; }
        public float BaseAttentionBudgetPerPoint { get; }
        public float RegionPressureBudgetMultiplier { get; }
        public int MinimumBudget { get; }
        public int MaximumBudget { get; }
        public IReadOnlyList<string> ZombieIds { get; }
    }
}
#endif
