using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Exploration.Data
{
    [CreateAssetMenu(fileName = "ExplorationRegion", menuName = "Zombie Card Survive/Exploration/Region")]
    public class ExplorationRegionData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string regionId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite artwork;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private ExplorationTier regionTier = ExplorationTier.Basic;
        [SerializeField] private DangerLevel dangerLevel = DangerLevel.Low;
        [SerializeField] private int baseOfferWeight = 10;
        [SerializeField] private bool isRareRegion;
        [SerializeField] private List<RewardTag> expectedRewards = new List<RewardTag>();
        [SerializeField] private List<CardPackDropEntry> cardPackDrops = new List<CardPackDropEntry>();
        [SerializeField] private List<RegionRiskEntry> risks = new List<RegionRiskEntry>();

        public string RegionId => regionId;
        public string DisplayName => displayName;
        public Sprite Artwork => artwork;
        public string Description => description;
        public ExplorationTier RegionTier => regionTier;
        public DangerLevel DangerLevel => dangerLevel;
        public int BaseOfferWeight => Mathf.Max(0, baseOfferWeight);
        public bool IsRareRegion => isRareRegion;
        public IReadOnlyList<RewardTag> ExpectedRewards => expectedRewards;
        public IReadOnlyList<CardPackDropEntry> CardPackDrops => cardPackDrops;
        public IReadOnlyList<RegionRiskEntry> Risks => risks;
        public string Id => regionId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(regionId))
            {
                regionId = AutoIdUtility.CreateId("region");
            }
        }

        public void RegenerateId()
        {
            regionId = AutoIdUtility.CreateId("region");
        }

        public string BuildExpectedRewardsText()
        {
            return ExplorationTextUtility.FormatRewards(expectedRewards);
        }
    }
}
