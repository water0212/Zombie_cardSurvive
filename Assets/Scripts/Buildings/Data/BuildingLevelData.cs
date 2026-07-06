using System;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Buildings.Data
{
    [Serializable]
    public class BuildingLevelData
    {
        [Min(1)]
        [SerializeField] private int level = 1;
        [SerializeField] private Sprite artwork;
        [SerializeField] private Color color = Color.white;
        [TextArea]
        [SerializeField] private string rewardDescription;
        [SerializeField] private ResourceCost constructionCost;
        [Min(1)]
        [SerializeField] private int requiredConstructionProgress = 1;
        [Min(1)]
        [SerializeField] private int progressPerCardPlay = 1;
        [SerializeField] private ConstructionCardData constructionCard;
        [Min(1)]
        [SerializeField] private int constructionCardCount = 1;
        [Min(0)]
        [SerializeField] private int freeIntegrationCount = 1;
        [Min(0)]
        [SerializeField] private int manufacturingBonus;

        public int Level => Mathf.Max(1, level);
        public Sprite Artwork => artwork;
        public Color Color => color;
        public string RewardDescription => rewardDescription;
        public ResourceCost ConstructionCost => constructionCost;
        public int RequiredConstructionProgress => Mathf.Max(1, requiredConstructionProgress);
        public int ProgressPerCardPlay => Mathf.Max(1, progressPerCardPlay);
        public ConstructionCardData ConstructionCard => constructionCard;
        public int ConstructionCardCount => Mathf.Max(1, constructionCardCount);
        public int FreeIntegrationCount => Mathf.Clamp(freeIntegrationCount, 0, ConstructionCardCount);
        public int ManufacturingBonus => Mathf.Max(0, manufacturingBonus);
    }
}
