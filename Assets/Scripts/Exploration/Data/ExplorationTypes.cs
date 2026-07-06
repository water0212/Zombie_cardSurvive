using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Exploration.Data
{
    public enum ExplorationTier
    {
        Basic,
        Advanced,
        HighRisk
    }

    public enum DangerLevel
    {
        Low,
        Medium,
        High,
        Extreme
    }

    public enum RewardTag
    {
        Food,
        Resource,
        Weapon,
        Tech,
        SurvivorClue,
        MixedSurvival
    }

    public enum CardPackType
    {
        FoodPack,
        SmallResourcePack,
        LargeResourcePack,
        TechPack,
        WeaponPack,
        SurvivorCluePack,
        MixedSurvivalPack
    }

    public enum RegionRiskType
    {
        AddZombieCardToDiscard,
        LoseMorale,
        GainInjury,
        LoseFood,
        AddWoundCard,
        TriggerBadEvent
    }

    [Serializable]
    public class CardPackEntry
    {
        [SerializeField] private CardBase card;
        [SerializeField] private int weight = 1;
        [SerializeField] private bool isZombiePollution;

        public CardBase Card => card;
        public int Weight => Mathf.Max(0, weight);
        public bool IsZombiePollution => isZombiePollution || card is ZombieCardData;
    }

    [Serializable]
    public class CardPackDropEntry
    {
        [SerializeField] private CardPackData cardPack;
        [SerializeField] private int weight = 1;
        [SerializeField] private int minPackCount = 1;
        [SerializeField] private int maxPackCount = 1;

        public CardPackData CardPack => cardPack;
        public int Weight => Mathf.Max(0, weight);
        public int MinPackCount => Mathf.Max(1, minPackCount);
        public int MaxPackCount => Mathf.Max(MinPackCount, maxPackCount);
    }

    [Serializable]
    public class RegionRiskEntry
    {
        [SerializeField] private RegionRiskType riskType = RegionRiskType.AddZombieCardToDiscard;
        [Range(0, 100)]
        [SerializeField] private int chancePercent = 10;
        [SerializeField] private int amount = 1;
        [SerializeField] private ZombieCardData zombieCard;
        [TextArea]
        [SerializeField] private string description;

        public RegionRiskType RiskType => riskType;
        public int ChancePercent => Mathf.Clamp(chancePercent, 0, 100);
        public int Amount => Mathf.Max(0, amount);
        public ZombieCardData ZombieCard => zombieCard;
        public string Description => description;
    }

    public static class ExplorationTextUtility
    {
        public static string FormatRewards(IReadOnlyList<RewardTag> rewards)
        {
            if (rewards == null || rewards.Count == 0)
            {
                return "-";
            }

            List<string> labels = new List<string>();
            foreach (RewardTag reward in rewards)
            {
                labels.Add(GetRewardLabel(reward));
            }

            return string.Join("、", labels);
        }

        public static string GetRewardLabel(RewardTag reward)
        {
            switch (reward)
            {
                case RewardTag.Food:
                    return "食物";
                case RewardTag.Resource:
                    return "資源";
                case RewardTag.Weapon:
                    return "武器";
                case RewardTag.Tech:
                    return "科技";
                case RewardTag.SurvivorClue:
                    return "倖存者線索";
                case RewardTag.MixedSurvival:
                    return "生存物資";
                default:
                    return reward.ToString();
            }
        }

        public static string GetDangerLabel(DangerLevel danger)
        {
            switch (danger)
            {
                case DangerLevel.Low:
                    return "低";
                case DangerLevel.Medium:
                    return "中";
                case DangerLevel.High:
                    return "高";
                case DangerLevel.Extreme:
                    return "極高";
                default:
                    return danger.ToString();
            }
        }
    }
}
