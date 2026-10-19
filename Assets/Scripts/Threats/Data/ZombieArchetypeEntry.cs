using System;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Threats.Data
{
    [Serializable]
    public sealed class ZombieArchetypeEntry
    {
        [SerializeField] private ZombieCardData zombieCard;
        [Min(1)]
        [SerializeField] private int budgetCost = 1;
        [Min(0)]
        [SerializeField] private int baseWeight = 10;
        [Min(1)]
        [SerializeField] private int minWorldThreatTier = 1;
        [Min(0)]
        [Tooltip("0 means no maximum world-threat tier.")]
        [SerializeField] private int maxWorldThreatTier;
        [Min(0)]
        [Tooltip("0 means this zombie can appear any number of times in one horde.")]
        [SerializeField] private int maxCountPerHorde;

        public ZombieCardData ZombieCard => zombieCard;
        public int BudgetCost => Mathf.Max(1, budgetCost);
        public int BaseWeight => Mathf.Max(0, baseWeight);
        public int MinWorldThreatTier => Mathf.Max(1, minWorldThreatTier);
        public int MaxWorldThreatTier => Mathf.Max(0, maxWorldThreatTier);
        public int MaxCountPerHorde => Mathf.Max(0, maxCountPerHorde);
        public bool HasCountLimit => MaxCountPerHorde > 0;

        public bool IsConfigured => zombieCard != null && BaseWeight > 0;

        public bool IsAllowedAtWorldThreatTier(int worldThreatTier)
        {
            int tier = Mathf.Max(1, worldThreatTier);
            if (tier < MinWorldThreatTier)
            {
                return false;
            }

            return MaxWorldThreatTier <= 0 || tier <= MaxWorldThreatTier;
        }

        public bool IsWithinCountLimit(int currentCount)
        {
            return !HasCountLimit || currentCount < MaxCountPerHorde;
        }
    }
}
